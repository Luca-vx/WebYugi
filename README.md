# WebYugi API

Backend REST do WebYugi: contas e sessões, catálogo de cartas, decks pessoais e favoritos. A API não implementa duelos em tempo real nem um motor de partidas.

## Funcionalidades

- Cadastro, login por e-mail/nome, logout, leitura e atualização do próprio perfil e exclusão da própria conta.
- Recuperação de senha por e-mail configurável.
- Consulta pública do catálogo com filtros; operações de escrita para cartas exigem sessão autenticada.
- CRUD de decks limitados ao proprietário, com cartas organizadas por seção (`Main`, `Extra`, `Side`).
- Favoritos por usuário.
- Upload de imagens de cartas para armazenamento local.

> **Permissão do catálogo:** atualmente qualquer usuário autenticado pode criar, alterar ou excluir cartas. Não há papel administrativo; considere isso antes de expor a API publicamente.

## Tecnologias e organização

- C# com ASP.NET Core no .NET 10 para API e middleware HTTP.
- Entity Framework Core 10 e Npgsql para persistência e migrations.
- PostgreSQL como banco de dados.
- ASP.NET Core `PasswordHasher` para hashes de senha (PBKDF2, salt individual, 210.000 iterações).
- Cookies de autenticação HttpOnly; o projeto não usa JWT.
- Swashbuckle para Swagger em Development; SkiaSharp para validar imagens enviadas.

```text
WebYugi/
  Controllers/      rotas REST e validação/orquestração de requests
  DTOs/             contratos HTTP
  Infrastructure/   AppDbContext e mapeamentos EF Core
  Migrations/       histórico versionado do schema
  Model/            entidades e enums
  Services/         SMTP, armazenamento de imagens e migração de senhas antigas
  uploads/cartas/   arquivos de cartas armazenados localmente
  Program.cs        registro de serviços, autenticação, middleware e rotas
  appsettings.example.json
```

Não há camada Repository dedicada; os controllers/serviços usam `AppDbContext` diretamente.

Para detalhes do fluxo entre componentes e do modelo relacional, consulte [Arquitetura](docs/architecture/README.md) e [Banco de dados](docs/database/README.md). A [referência da API](docs/api/README.md) resume os grupos de rotas e seus limites; os contratos completos estão descritos abaixo.

## Instalação e execução pela primeira vez

### Pré-requisitos

- .NET SDK 10.0.
- PostgreSQL (o exemplo usa `localhost:5432`).
- EF Core CLI 10.0.12, correspondente às dependências do projeto:

```bash
dotnet tool install --global dotnet-ef --version 10.0.12
```

### Clonar e configurar PostgreSQL

```bash
git clone https://github.com/Luca-vx/WebYugi.git
cd WebYugi
```

Crie um usuário e o banco PostgreSQL. Exemplo executado no `psql` como usuário administrador (troque a senha pelo valor local escolhido):

```sql
CREATE USER webyugi WITH PASSWORD 'defina-uma-senha-local';
CREATE DATABASE cartas OWNER webyugi;
```

Copie `WebYugi/appsettings.example.json` para `WebYugi/appsettings.json`. No novo arquivo, substitua `ConnectionStrings:DefaultConnection` com as credenciais locais. Alternativamente, defina `ConnectionStrings__DefaultConnection` no ambiente do processo backend. Não versione credenciais reais.

O arquivo de exemplo usa o banco `cartas`, porta `5432` e placeholders de usuário/senha. Não há `.env.example`.

### Aplicar migrations

Na pasta raiz deste repositório:

```bash
dotnet restore WebYugi/WebYugi.csproj
dotnet ef database update --project WebYugi/WebYugi.csproj --startup-project WebYugi/WebYugi.csproj
```

As migrations criam tabelas para usuários, cartas, decks, itens de deck, favoritos e tokens de redefinição. A aplicação **não aplica migrations automaticamente** e não tem seed de cartas. Um banco vazio pode ser inicializado com as migrations; cadastre cartas após iniciar a API.

Antes de atualizar um banco já utilizado, faça backup. A migration de autenticação normaliza identificadores de contas antigas e falha se detectar nome ou e-mail duplicado após normalização; resolva os conflitos antes de repetir a operação. Ela também adiciona suporte a hashes e tokens de recuperação. Evite reverter somente o código após usuários terem migrado suas senhas: versões antigas não validam hashes.

### Restaurar e iniciar

```bash
dotnet run --project WebYugi/WebYugi.csproj --launch-profile https
```

O perfil `https` escuta em `https://localhost:7146` e `http://localhost:5292`. O perfil `http` usa `http://localhost:5292`. Swagger/Swagger UI estão em `/swagger` apenas em Development. Para confiar o certificado HTTPS local, use `dotnet dev-certs https --trust`; alternativamente inicie com `--launch-profile http` para desenvolvimento local.

Verificação rápida, usando o endereço HTTP local:

```bash
curl http://localhost:5292/cards
```

Uma resposta `200` com `[]` indica que a API está acessível e o banco consultável, ainda que não haja cartas cadastradas. Endpoints protegidos retornarão `401` sem cookie de sessão.

## Configuração

### Banco

| Chave | Necessária | Descrição |
|---|---:|---|
| `ConnectionStrings__DefaultConnection` | Sim, se não estiver em `appsettings*.json` | Connection string do PostgreSQL |

Exemplo de formato (substitua as credenciais): `Host=localhost;Port=5432;Database=cartas;Username=webyugi;Password=...`.

### E-mail de recuperação (opcional)

Defina por ambiente ou gerenciador de segredos:

| Variável | Descrição |
|---|---|
| `Email__SmtpHost` | Host SMTP |
| `Email__SmtpPort` | Porta SMTP TLS (padrão 587) |
| `Email__Username` | Usuário SMTP, se necessário |
| `Email__Password` | Senha SMTP; não commitar |
| `Email__From` | Remetente verificado |
| `Email__PasswordResetUrl` | URL HTTPS da página que receberá `?token=...` |

Sem uma configuração SMTP válida e URL HTTPS, `POST /users/forgot-password` continua retornando uma mensagem genérica, mas nenhum e-mail é enviado. O frontend ainda não oferece a página de redefinição.

## Autenticação e segurança

- Login cria o cookie `WebYugiAuth`, com `HttpOnly` e `SameSite=Lax`, duração de 8 horas e renovação deslizante. HTTPS é obrigatório fora de Development.
- Chamadas autenticadas devem preservar o cookie; não há token Bearer/JWT para enviar.
- Login e rotas de recuperação de senha compartilham um limite de 10 solicitações por endereço IP a cada 10 minutos (`429` ao exceder).
- Senhas novas e redefinidas: 8–128 caracteres, maiúscula, minúscula, número e símbolo; senhas comuns e dados de identificação são recusados.
- Senhas antigas em texto puro são convertidas em lotes pelo serviço de background após a migration; login válido também migra a conta individualmente.
- Tokens de recuperação são aleatórios, guardados em hash, expiram em 20 minutos e são de uso único.
- Headers de segurança são adicionados pela API; HSTS e HTTPS redirect são habilitados fora de Development.

## Rotas da API

Prefixos abaixo são relativos à origem do backend (por exemplo `https://localhost:7146`). A API lê/escreve JSON, exceto upload de carta (`multipart/form-data`). `401` indica sessão ausente/inválida, `403` acesso proibido, `404` recurso não encontrado, `409` conflito/regra de domínio, `429` limite de tentativas. DTO inválido costuma resultar em `400` via `[ApiController]`.

### Usuários — `/users`

| Método e rota | Acesso | Entrada | Resposta |
|---|---|---|---|
| `POST /users` | Público | `{ "name": "Duelista", "email": "duelista@example.com", "senha": "SenhaForte1!" }` | `201` usuário; `400` validação; `409` conflito de nome/e-mail |
| `POST /users/login` | Público, limitado | `{ "emailOrName": "duelista@example.com", "password": "SenhaForte1!" }` | `200` usuário e cookie; `401` credenciais inválidas; `429` limite |
| `POST /users/logout` | Autenticado | Sem body | `204`; encerra sessão |
| `GET /users/me` | Autenticado | — | `200` próprio perfil |
| `PUT /users/me` | Autenticado | `{ "name": "Novo nome", "email": "novo@example.com", "senha": "NovaSenha1!", "senhaAtual": "SenhaForte1!" }` | `200` perfil atualizado; senha e senhaAtual opcionais, porém ambas necessárias para trocar senha; `400`, `401` ou `409` em caso de erro |
| `GET /users` | Autenticado | — | `200` lista contendo apenas a conta atual |
| `GET /users/{id}` | Autenticado | ID inteiro | `200` própria conta; `403` para outra; `404` se não existir |
| `PUT /users/{id}` | Autenticado | Mesmo body de `PUT /users/me` | Atualiza somente a própria conta; `204`, `403` ou `404` |
| `DELETE /users/{id}` | Autenticado | ID inteiro | Exclui a própria conta; `204`, `403` ou `404` |
| `POST /users/forgot-password` | Público, limitado | `{ "email": "duelista@example.com" }` | `202` com resposta genérica, independentemente de a conta existir |
| `POST /users/reset-password` | Público, limitado | `{ "token": "token-recebido-no-email", "newPassword": "NovaSenha1!" }` | `204`; `400` token inválido/expirado ou senha rejeitada |

### Cartas — `/cards`

`type`: `Monster`, `Spell`, `Trap`; `attribute`: `Dark`, `Light`, `Earth`, `Water`, `Fire`, `Wind`, `Divine`; `monsterRace` aceita os valores do enum, como `Dragon` e `Spellcaster`.

| Método e rota | Acesso | Parâmetros/entrada | Resposta |
|---|---|---|---|
| `GET /cards` | Público | Query opcionais: `search`, `type`, `attribute`, `monsterRace`, `minAttack`, `maxAttack`, `minDefense`, `maxDefense`, `minLevel`, `maxLevel`, `archetype`, `rarity`, `description` | `200` lista ordenada por nome |
| `GET /cards/{id}` | Público | ID inteiro | `200` carta; `404` ausente |
| `POST /cards` | Autenticado* | Multipart: `name`, `type`, `description`; opcionais `attribute`, `monsterRace`, `level`, `attack`, `defense`, `archetype`, `rarity`, `image` | `201`; arquivo de imagem até 5 MB; `400` validação/upload inválido |
| `PUT /cards/{id}` | Autenticado* | Mesmo formulário multipart | `200` carta atualizada; `404` ausente |
| `DELETE /cards/{id}` | Autenticado* | ID inteiro | `204`; `409` carta referenciada em deck |

Validações: nome obrigatório até 200 caracteres; nível entre 0 e 13; arquétipo até 100 e raridade até 50. Imagem aceita JPG/JPEG, PNG, WEBP e GIF, limitada a 5 MB e verificada por decoder. `*` Qualquer conta autenticada pode alterar catálogo: não existe role administrativa.

Exemplo:

```http
GET /cards?search=Drag%C3%A3o&type=Monster&minAttack=2000
```

### Decks — `/decks`

Todas as rotas exigem autenticação e verificam a propriedade do deck.

| Método e rota | Entrada/query | Resposta |
|---|---|---|
| `GET /decks` | — | `200` decks do usuário |
| `GET /decks/{id}` | ID inteiro | `200` deck; `404` ausente ou de outro usuário |
| `POST /decks` | `{ "name": "Dragões" }` | `201` deck criado |
| `PUT /decks/{id}` | `{ "name": "Novo nome" }` | `200` deck atualizado |
| `DELETE /decks/{id}` | ID inteiro | `204` |
| `PATCH /decks/{deckId}/cards/{cardId}` | `{ "quantity": 1, "section": "Main" }` | `200` deck atualizado; `409` se violar limites |
| `PUT /decks/{deckId}/cards/{cardId}?section=Main` | `{ "quantity": 2 }` | `200` deck atualizado; `409` se violar limites |
| `DELETE /decks/{deckId}/cards/{cardId}?section=Main` | Seção opcional, padrão `Main` | `200` deck atualizado |

Nome obrigatório com até 150 caracteres; quantidade de cópias de 1–3; seções `Main`, `Extra`, `Side`. Deck pode ser incompleto. `IsValid` informa se soma 40–60 cartas; o máximo de 60 e três cópias por carta é aplicado nas mutações.

### Favoritos — `/favorites`

Todas as rotas exigem autenticação.

| Método e rota | Resposta |
|---|---|
| `GET /favorites` | `200` cartas favoritas da conta |
| `POST /favorites/{cardId}` | `204`; `404` se carta não existir; repetição é idempotente |
| `DELETE /favorites/{cardId}` | `204`, inclusive se não era favorito |

## Persistência e uploads

`AppDbContext` define as tabelas `users`, `cards`, `decks`, `deck_cards`, `user_favorite_cards` e `password_reset_tokens`. Decks/favoritos/tokens pertencem ao usuário; exclusões relacionadas usam cascade onde configurado. Cartas usadas em decks têm exclusão restrita. Imagens ficam em `WebYugi/uploads/cartas` e são expostas em `/uploads/cartas/...`.

Em produção, use disco persistente para uploads (e compartilhado se houver múltiplas instâncias) ou substitua o serviço por storage apropriado. Configure proxy da aplicação para `/uploads` e API conforme sua hospedagem.

O diagrama e a descrição das chaves estão em [Banco de dados](docs/database/README.md).

## Limitações e diagnóstico

- **Não conecta ao banco:** confirme serviço PostgreSQL, host, porta, nome do banco e connection string.
- **Migration não aplica:** faça backup; confira conflitos de nomes/e-mails normalizados e compatibilidade da ferramenta `dotnet-ef` 10.x.
- **API retorna 401:** autentique-se e preserve o cookie `WebYugiAuth`; Swagger/testes HTTP precisam manter cookies entre requisições.
- **Imagem desaparece após redeploy:** o diretório de uploads não está persistido no disco de hospedagem.
- **Sem conteúdo no catálogo:** não há seed ou importador de cartas neste repositório.
- **Sem fluxo de duelos:** partidas online e motor de regras não estão implementados.
- **Testes:** não há projeto de testes automatizados configurado.

## Verificação

```bash
dotnet build WebYugi/WebYugi.csproj
```

Build valida a compilação. Para validar persistência e rotas, configure PostgreSQL, aplique migrations e inicie a API; migrations não foram executadas automaticamente na inicialização.
