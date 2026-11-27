# Referência da API

A API é implementada por controllers ASP.NET Core. Use `/swagger` em ambiente `Development` para explorar os contratos publicados. A referência completa de métodos, parâmetros e respostas está no [README do backend](../../README.md#rotas-da-api); este documento resume os grupos e as regras de acesso.

| Prefixo | Acesso | Escopo |
|---|---|---|
| `/users` | Misto | Cadastro, login/logout, perfil e recuperação de senha |
| `/cards` | Leitura pública; escrita administrativa | Catálogo, filtros e upload multipart de imagem |
| `/api/admin/users` | Administrador | Consulta e atualização de dados cadastrais de usuários |
| `/decks` | Autenticado | Decks próprios e suas cartas nas seções Main/Extra/Side |
| `/favorites` | Autenticado | Favoritos próprios |

## Convenções importantes

- A sessão usa cookie `WebYugiAuth`; não envie Bearer/JWT. O cliente deve preservar cookies.
- `401` representa sessão ausente/inválida e `403` acesso proibido. Recursos de deck que não pertencem à conta normalmente são apresentados como não encontrados.
- `POST /users/forgot-password` responde de forma genérica. O envio real depende de SMTP e URL HTTPS configurados; o frontend atual não oferece a tela correspondente.
- A escrita do catálogo e as rotas `/api/admin/users` exigem o papel administrativo verificado no banco a cada requisição.
- Upload de carta usa `multipart/form-data`. Os demais contratos usam JSON.
- A API não implementa duelo, matchmaking ou compartilhamento de deck.

Consulte os DTOs em `WebYugi/DTOs` e os controllers em `WebYugi/Controllers` para os tipos atuais. A configuração e execução estão no [README principal](../../README.md).
