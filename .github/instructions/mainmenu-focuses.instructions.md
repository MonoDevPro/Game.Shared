
**Título:** Proposta de Arquitetura para a Camada de Menu Principal (MainMenu) de um MMORPG 2D

**Objetivo:** Atuar como um arquiteto de software sênior. O objetivo é projetar e refatorar a arquitetura da camada de `MainMenu` para um jogo MMORPG 2D, garantindo um design robusto, escalável e de fácil manutenção tanto no cliente quanto no servidor.

**1. Contexto do Projeto:**
A solução .NET está organizada em três projetos principais:
* `Client/`: O projeto do jogo, desenvolvido em Godot.
* `Server/`: O aplicativo de console que atua como servidor autoritativo.
* `Shared/`: Uma biblioteca de classes compartilhada entre o Cliente e o Servidor, contendo modelos de dados, pacotes de rede e lógica comum.

**2. Tecnologias:**
* **Engine:** Godot 4.4 com C#.
* **Rede:** LiteNetLib para a camada de transporte.
* **Arquitetura Lógica:** ArchECS, utilizado primariamente para a simulação em tempo real *dentro do jogo* (após a seleção de personagem).

**3. Escopo do Foco Atual:**
O foco exclusivo desta tarefa é a **camada de `MainMenu` no lado do Cliente** e sua **contraparte lógica no Servidor**. Isso inclui:
* **Cliente:** O `MenuManager` como orquestrador principal e seus agregados (as janelas de UI):
    * Janela de Login (`LoginWindow`).
    * Janela de Criação de Conta (`CreateAccountWindow`).
    * Janela de Lista de Personagens (`CharacterListWindow`).
    * Janela de Criação de Personagem (`CreateCharacterWindow`).
* **Servidor:** A lógica recíproca para processar as requisições enviadas pelo menu do cliente, interagir com a camada de persistência (banco de dados) e enviar respostas consistentes.

**4. Requisitos Chave:**
* **Separação de Responsabilidades:** O `MenuManager` deve focar na orquestração da UI. A lógica de comunicação com a rede deve ser isolada em uma classe de serviço dedicada (ex: `MenuNetwork`).
* **Organização de Pastas:** Propor e seguir uma estrutura de pastas clara e lógica para todos os arquivos relacionados ao `MainMenu` (scripts, cenas, pacotes de rede) dentro dos projetos `Client` e `Shared`.
* **Fluxo de Dados Assíncrono e Orientado a Eventos:** O fluxo de interação (ex: clicar em "Login" -> aguardar resposta) deve ser desacoplado, usando eventos C# e modelos de dados `Attempt`/`Result` para a comunicação interna no cliente.
* **Consistência de Dados:** Definir os pacotes de rede (DTOs) necessários para a comunicação Cliente-Servidor no menu, garantindo que ambos os lados compartilhem o mesmo "contrato".
* **Coesão e Baixo Acoplamento:** As janelas de UI devem ser "burras", sem conhecimento direto da camada de rede. Elas apenas emitem eventos com os dados inseridos pelo usuário.

**5. Entregáveis Esperados:**
1.  Uma proposta de **estrutura de pastas** para a feature `MainMenu` nos projetos `Client` e `Shared`.
2.  **Exemplos de código C#** para as classes principais no cliente: `MenuManager` (orquestrador), `MenuNetwork` (serviço de rede), e um exemplo de script de janela (ex: `LoginWindow`).
3.  Uma descrição clara do **fluxo de dados** para uma operação completa, como o login do usuário, desde o clique na UI até a resposta do servidor.
4.  Um design conceitual e exemplos de código para a **lógica correspondente no lado do servidor**, mostrando como ele recebe um pacote, o processa (usando o padrão de Entidade de Comando no ECS) e envia uma resposta.