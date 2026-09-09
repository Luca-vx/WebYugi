Yu-Gi-Oh! Deck Builder

Plataforma web para criação, montagem, organização e validação de Decks de Yu-Gi-Oh!, permitindo que os usuários pesquisem cartas, construam suas próprias listas e verifiquem sua validade de acordo com as regras e Banlists configuradas no sistema.

Status do projeto: Em desenvolvimento 🚧

📖 Sobre o projeto

O projeto consiste no desenvolvimento de uma plataforma web voltada para jogadores de Yu-Gi-Oh! que desejam montar e organizar seus Decks de forma prática.

A aplicação disponibiliza um catálogo de cartas e um Deck Builder, permitindo adicionar, remover e organizar cartas nas diferentes seções de um Deck. O sistema também realiza validações automáticas para identificar problemas relacionados à quantidade de cartas, quantidade de cópias e restrições da Banlist.

O projeto foi planejado com foco em usabilidade, organização, modularidade e confiabilidade das regras de construção de Decks.

🎯 Objetivos

O principal objetivo é desenvolver uma plataforma que facilite a construção e o gerenciamento de Decks de Yu-Gi-Oh!.

Entre os objetivos específicos estão:

* Disponibilizar um catálogo pesquisável de cartas.
* Permitir a criação e gerenciamento de múltiplos Decks.
* Permitir a organização das cartas em Main Deck, Extra Deck e Side Deck.
* Validar automaticamente a composição dos Decks.
* Aplicar restrições definidas pelas Banlists.
* Permitir visualização e compartilhamento de Decks.
* Oferecer recursos de organização pessoal, como favoritos e coleção.
  
🃏 Funcionalidades
Página inicial

A página inicial apresenta a plataforma e fornece acesso às principais funcionalidades do sistema.

Dashboard

O Dashboard centraliza os recursos do usuário, permitindo acessar rapidamente:

* Meus Decks;
* Deck Builder;
* Catálogo de cartas;
* Cartas favoritas;
* Coleção;
* Perfil.
* Catálogo de cartas

O sistema disponibiliza um catálogo para consulta das cartas.

É possível:

* pesquisar por nome;
* pesquisar pelo texto do efeito;
* filtrar por características;
* visualizar detalhes;
* consultar restrições da Banlist.
* Deck Builder

O Deck Builder é o principal recurso da plataforma.

O usuário poderá:

* criar Decks;
* editar Decks;
* excluir Decks;
* adicionar cartas;
* remover cartas;
* alterar quantidades;
* organizar Main Deck;
* organizar Extra Deck;
* organizar Side Deck;
* duplicar Decks;
* salvar suas listas.
* Validação de Deck

O sistema verifica automaticamente se a composição do Deck atende às regras configuradas.

Entre as validações estão:

* quantidade de cartas no Main Deck;
* quantidade de cartas no Extra Deck;
* quantidade de cartas no Side Deck;
* quantidade máxima de cópias;
* cartas proibidas;
* cartas limitadas;
* cartas semi-limitadas.

O usuário recebe informações sobre os problemas encontrados para facilitar a correção do Deck.

Banlist

A plataforma possui suporte a Banlists, permitindo consultar as restrições aplicadas às cartas.

Os possíveis estados de uma carta são:

* PROIBIDA
* LIMITADA
* SEMI-LIMITADA
* LIVRE
* Compartilhamento

Isso permite que o usuário controle quem pode visualizar suas listas.

Recursos sociais

A plataforma poderá disponibilizar:

* cópia de Decks públicos.
* Favoritos

O usuário poderá marcar cartas como favoritas para encontrá-las facilmente posteriormente.

Coleção

O sistema poderá permitir que o usuário registre sua coleção pessoal de cartas e suas respectivas quantidades.

🧩 Modelo de domínio

O domínio principal da aplicação é baseado nas seguintes entidades:

-------------------------> FAZER DEPOIS
🔧 Tecnologias

A stack tecnológica pode ser definida conforme a implementação do projeto.

Backend
* C#
* ASP.NET Core
* Entity Framework Core
* API REST
  
Banco de dados
* PostgreSQL
  
Frontend
* HTML
* CSS
* JavaScript

ou um framework frontend, caso adotado durante o desenvolvimento.

Ferramentas
* Git
* GitHub
* Visual Studio / Visual Studio Code

🏗️ Arquitetura

------------------------> FAZER DEPOIS
📋 Histórias de usuário

O desenvolvimento é organizado através de Histórias de Usuário (HUs) priorizadas.

Exemplo:

HU — Criar Deck
Como jogador, quero criar um novo Deck para organizar uma lista de cartas.

As histórias são classificadas pela prioridade:

Prioridade	Significado
🔴 P1	Crítica
🟠 P2	Alta
🟡 P3	Média
🟢 P4	Baixa

As HUs são posteriormente refinadas com critérios de aceitação, permitindo que cada requisito seja desenvolvido e testado de forma objetiva.

✅ Escopo

O sistema contempla:

* Cadastro e autenticação;
* Perfil de usuário;
* Catálogo de cartas;
* Pesquisa e filtros;
* Criação de Decks;
* Main Deck;
* Extra Deck;
* Side Deck;
* Validação de Decks;
* Banlists;
* Favoritos;
* Coleção;
  
Mecânicas fora do escopo

O projeto não contempla partidas online.

Também não fazem parte do sistema:

❌ Duelos online;
❌ Matchmaking;
❌ Salas de duelo;
❌ Turnos;
❌ Campo de batalha;
❌ Chain durante partidas;
❌ Cálculo de dano;
❌ Histórico de partidas;
❌ Invocação-Pêndulo;
❌ Invocação-Link;
❌ Rush Duels.

O foco é exclusivamente a criação, organização e validação de Decks.

📌 Roadmap
Fase 1 — Estrutura inicial
 Configuração do projeto
 Banco de dados
 Autenticação
 Estrutura de usuários
 
Fase 2 — Catálogo
 Cadastro/importação de cartas
 Listagem de cartas
 Pesquisa
 Filtros
 Página de detalhes
 
Fase 3 — Deck Builder
 Criar Deck
 Adicionar cartas
 Remover cartas
 Main Deck
 Extra Deck
 Side Deck
 Alteração de quantidades
 Salvamento
 
Fase 4 — Validação
 Validação da quantidade de cartas
 Validação de cópias
 Banlist
 Mensagens de erro
 Status do Deck
 
Fase 5 — Recursos adicionais
 Favoritos
 Coleção
 Amizades
 Compartilhamento
 Decks públicos
 Duplicação de Decks
 
👥 Equipe

Projeto desenvolvido como parte de uma iniciativa acadêmica de desenvolvimento de software.

📄 Documentação

A documentação do projeto inclui:

* elicitação de requisitos;
* histórias de usuário;
* critérios de aceitação;
* modelo conceitual do domínio;
* modelo de dados;
* arquitetura do sistema;
* documentação da API;
* testes.
⚠️ Aviso

Yu-Gi-Oh! é uma propriedade intelectual de seus respectivos detentores. Este projeto possui finalidade acadêmica e/ou experimental e não possui vínculo oficial com a Konami.

🚀 Objetivo do projeto

Criar uma ferramenta simples, organizada e confiável para transformar a ideia de um Deck em uma lista estruturada, pesquisável e validada.
