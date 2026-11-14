# Separação entre lógica e interface

## Objetivo

Manter a versão Windows em WPF enquanto as regras e os casos de uso ficam independentes da interface. Uma futura interface para macOS ou Android deverá consumir os mesmos serviços, sem duplicar regras.

## Limites entre projetos

- `VCS.Sheet`: modelos de ficha e valores persistidos; não deve depender de WPF, SQLite ou controles de interface.
- `VCS.Content`: catálogos, conteúdo de jogo e atribuição de fontes; não deve depender de UI ou persistência.
- `VCS.Rules.V5`: resolução determinística das regras; não deve depender de WPF, banco de dados, arquivos ou serviços de aplicação.
- `VCS.Application`: casos de uso e coordenação das regras; pode depender dos projetos de domínio/conteúdo, mas não de WPF nem de `VCS.Infrastructure`.
- `VCS.Infrastructure`: banco, arquivos, exportação e serviços externos; implementa interfaces definidas pela aplicação.
- `VCS.Desktop`: WPF, ViewModels, comandos, navegação e composição de dependências. Deve chamar casos de uso da aplicação e adaptar seus resultados para a tela.

Fluxo esperado:

`View WPF → ViewModel → caso de uso em VCS.Application → regras V5 → resultado → ViewModel → View WPF`

Texto de apresentação, `ObservableCollection`, `ICommand`, `Dispatcher`, `CollectionView` e controles permanecem na camada Desktop. Cálculos, validações, transições de estado e decisões de regra ficam em `VCS.Rules.V5` ou `VCS.Application`.

## Primeiro recorte

`VCS.Application.Blood.BloodActions` passou a coordenar alimentação, gatilhos e resistência a Frenesi, Surto de Sangue, testes e Checagens de Sangue. `VCS.Application.Humanity.HumanityActions` coordena Remorso e dano por Máculas. A interface continua formatando as mensagens e exibindo os dados; não executa diretamente esses fluxos de regra.

## Próximas etapas

1. Extrair criação/validação da ficha e cálculo de avanço dos ViewModels para casos de uso da aplicação.
2. Extrair seleção/aplicação de Predador, Vantagens, Disciplinas e fichas de conhecimento para fluxos de aplicação; deixar regras puras em `VCS.Rules.V5`.
3. Remover dependências de WPF de ViewModels e helpers que ainda usam `ListCollectionView`, `Dispatcher`, controles ou tipos WPF.
4. Mover formatação/localização de mensagens para apresentadores da camada Desktop, mantendo resultados de regra tipados.
5. Manter a interface WPF como cliente atual; só depois de estabilizar os casos de uso, criar outro cliente multiplataforma que reutilize `VCS.Application`.

Cada recorte deve preservar o comportamento e os dados existentes. Nenhuma tela ou regra deve migrar de uma vez sem um motivo concreto.
