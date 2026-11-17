# Separação entre lógica e interface

## Objetivo

Manter a versão Windows em WPF enquanto as regras e os casos de uso ficam independentes da interface. Uma futura interface para macOS ou Android deverá consumir os mesmos serviços, sem duplicar regras.

A árvore-alvo das pastas está em [project-structure.md](project-structure.md). Ela remove o prefixo VCS dos nomes de diretório e separa Shared de Editions. Os projetos existentes estão em `src/Shared`, `src/Editions/V5/Rules` e `src/Desktop`, mantendo seus nomes e namespaces. A separação das dependências e dos fluxos específicos da V5 ainda está pendente.

## Limites entre projetos

- `VCS.Sheet`: modelos de ficha e valores persistidos; não deve depender de WPF, SQLite ou controles de interface.
- `VCS.Content`: catálogos, conteúdo de jogo e atribuição de fontes; não deve depender de UI ou persistência.
- `VCS.Rules.V5`: resolução determinística das regras V5; não deve depender de WPF, banco de dados, arquivos ou serviços de aplicação.
- `VCS.Application`: casos de uso comuns e coordenação. Os fluxos que dependem de V5 foram movidos para `VCS.Application.V5`; a aplicação compartilhada não referencia diretamente `VCS.Rules.V5`.
- `VCS.Infrastructure`: banco, arquivos, exportação e serviços externos; implementa interfaces definidas pela aplicação.
- `VCS.Desktop`: WPF, ViewModels, comandos, navegação e composição de dependências. Deve chamar casos de uso da aplicação e adaptar seus resultados para a tela.

Fluxo atual da ficha V5:

`View WPF → ViewModel → caso de uso em VCS.Application → fluxo/regras V5 → resultado → ViewModel → View WPF`

Fluxo-alvo:

`View → ViewModel → caso de uso compartilhado → módulo da edição da ficha → resultado → ViewModel → View`

Texto de apresentação, `ObservableCollection`, `ICommand`, `Dispatcher`, `CollectionView` e controles permanecem na camada Desktop. Cálculos e decisões V5 ficam em `VCS.Rules.V5` ou nos fluxos V5 ainda presentes em `VCS.Application`. No alvo, cada decisão de edição ficará no módulo correspondente.

## Primeiro recorte

`VCS.Application.Blood.BloodActions` coordena alimentação, gatilhos e resistência a Frenesi, Surto de Sangue, testes e Checagens de Sangue. `VCS.Application.Humanity.HumanityActions` coordena Remorso, dano por Máculas e consulta aos níveis de Humanidade. `CharacterSheetActions` coordena validação, capacidades, cálculo de XP, bases de criação, dados de apresentação da ficha e verificações de Potência/Geração; `PredatorActions` e `LoresheetActions` coordenam as seleções e alterações de estado. `DiceActions` monta paradas de Atributo + Habilidade, aplica o dado de especialização quando marcado e encaminha a rolagem. A interface mantém a formatação e a apresentação.

## Próximas etapas pendentes

1. Concluído parcialmente: validação, capacidade de trilhas, cálculo de XP, sincronização das bases, consultas de Humanidade, predador, ficha de conhecimento, custo de disciplina, dados auxiliares da ficha e montagem de parada foram encaminhados a `VCS.Application`. Falta mover o fluxo de compras de XP e completar operações comuns de vantagens/disciplina.
2. Extrair seleção/aplicação de Vantagens e Disciplinas para fluxos de aplicação; deixar regras puras em `VCS.Rules.V5`.
3. Remover dependências de WPF de ViewModels e helpers que ainda usam `ListCollectionView`, `Dispatcher`, controles ou tipos WPF. Hoje `NotesViewModel` e o filtro de disciplinas ainda usam `ListCollectionView`.
4. Mover formatação/localização de mensagens para apresentadores da camada Desktop, mantendo resultados de regra tipados.
5. Manter a interface WPF como cliente atual; só depois de estabilizar os casos de uso, criar outro cliente multiplataforma que reutilize `VCS.Application`.

A migração de pastas planejada está descrita em [project-structure.md](project-structure.md). Cada recorte deve preservar comportamento, fichas, fontes, exceções do Narrador e temas personalizados. Nenhuma tela ou regra deve migrar de uma vez sem revisão do respectivo fluxo.
