# Separação de regras por edição

A ficha e a apresentação devem encaminhar as ações à edição correta. `VCS.Rules` contém o contrato inicial de resolução de poderes; `VCS.Rules.V5` fornece a implementação atual. O registro identifica cada implementação pela edição e rejeita edições ausentes.

O fluxo de ativação de poderes da aplicação ainda usa Fome, Checagem de Sangue e dados da V5. Por isso ele rejeita explicitamente outras edições antes de pagar custos ou rolar dados. O identificador padrão V5 preserva o comportamento das fichas existentes; ainda não existe seletor nem edição persistida por ficha.

Para acrescentar outra edição, primeiro defina seus próprios modelos de custos, dados, catálogo e ficha, implemente seus serviços e registre-os no ponto de composição. Não reutilize mecanicamente pressupostos V5 como Fome ou Potência de Sangue. Migração e identificação de edição por ficha, exportação, fontes e compatibilidade de dados devem ser feitas antes de disponibilizar a seleção.

A extração da interface não automatiza poderes ou rituais pendentes. O inventário detalhado continua em `power-effect-coverage.md`.

## Limite atual do contrato de poderes

O contrato inicial em `IPowerRuleset` ainda não é totalmente neutro: `PowerResolutionContext` inclui `RouseChecks` e `BloodPotency`, e o resultado inclui `HungerIncrease`, todos conceitos da ativação V5. `Wits` é um atributo usado por alguns poderes, mas não torna custos e contexto da V5 universais. Além disso, `PowerUseActions` rejeita edições diferentes de V5 antes de pagar custos ou rolar dados.

Isso é uma fronteira incompleta, não suporte às outras edições. Antes de habilitar outra versão, será necessário separar entradas comuns de rolagem dos contextos de ativação próprios de cada edição e manter os custos dentro do módulo correto. Nenhuma regra V3, V20, V2 ou V1 deve ser adaptada aos campos de Fome/Potência do Sangue da V5.


## Organização das pastas

A estrutura-alvo sem o prefixo repetido VCS está em [architecture/project-structure.md](architecture/project-structure.md). Ela separa código compartilhado em Shared e conteúdo/regras/fluxos em Editions/V5, Editions/V3 e Editions/V20. Essa árvore é planejada: o código atual ainda usa pastas de projeto como VCS.Sheet e VCS.Rules.V5.

## Rituais do livro base V5

`VCS.Rules.Rituals.IRitualEffectRuleset` expõe um contexto de resultado independente do rolador WPF e uma proposta de efeito. `V5RitualEffectRuleset` interpreta esse contexto, enquanto `RitualOngoingRules` calcula ajustes condicionais e proteção. Nenhuma regra de V20/V3/V2/V1 foi acrescentada.

`RitualWorkflowActions` coordena etapas, pagamentos unitários, testes, repetições de Dagon, doses e disputas dos círculos. Esse coordenador ainda é **específico da V5**: usa Fome, Checagem de Sangue e suas paradas. O contrato dos efeitos não transforma esse fluxo em um sistema universal de custos. Cada futura edição precisará do próprio coordenador.

`RitualWorkflowState` e o histórico por intruso são campos opcionais de `PowerEffectEntry`, armazenado no JSON existente. Registros anteriores continuam válidos, com estado nulo; não se criam colunas ou migrações. A interface escolhe o preparo/efeito, recebe confirmações e apresenta dados/resultados na mesa existente. O catálogo fornece ingredientes, tempo e gatilhos; a ficha conserva os textos que o usuário já personalizou.

O encerramento e as noites de cegueira de Olhos do Falcão Noturno usam `RitualOngoingRules` e `RitualWorkflowActions`, fora do ViewModel. A interface só envia a decisão confirmada e a data da crônica; a interpretação de três noites é V5. Exceções antecipadas são registradas explicitamente, sem reaproveitar automaticamente uma autorização do preparo.


## Mapa comparativo: V5, V3 e V20

### Escopo e fontes

- **V5:** livro-base V5 fornecido pelo usuário e implementação ativa no repositório.
- **V3:** *Vampiro: A Máscara*, 3ª edição, livro-base em português fornecido pelo usuário.
- **V20:** *Vampire: The Masquerade 20th Anniversary Edition*, livro-base anexado pelo usuário e consultado nesta rodada.
- O mapa serve à arquitetura; não é uma transcrição exaustiva de regras. Quando a comparação precisa de regra detalhada — especialmente custos, limites de geração, combate ou disciplina — ela fica pendente do livro/trecho correto.



### Diferenças mecânicas e de conteúdo confirmadas

| Subsistema | V5 | V3 (livro-base) | V20 (livro-base) | Consequência para módulos |
| --- | --- | --- | --- | --- |
| Resolução de ações | Pool de Atributo + Habilidade, d10; sucessos e regra de Fome/críticos próprios da V5. | Pool de Atributo + Habilidade, d10; regras clássicas de dificuldade, falha crítica e especialidades. | Sistema Storyteller clássico: pools de d10, dificuldade e sucessos; sem dados de Fome da V5. | O lançador pode ser compartilhado; composição, dificuldade, sucessos, falhas e Força de Vontade são políticas da edição/setting. |
| Criação | Distribuição e validações próprias da V5; inclui Potência do Sangue, Geração e Humanidade no modelo da edição. | Criação clássica: Atributos 7/5/3; Habilidades 13/9/5; 3 Disciplinas, 5 Antecedentes, 7 Virtudes e 15 pontos de bônus (pp. 102–104). | Criação clássica confirmada: Atributos 7/5/3; Habilidades 13/9/5; 3 Disciplinas, 5 Antecedentes, 7 Virtudes e 15 pontos livres (criação, p. 80). | A interface pode compartilhar controles, mas distribuição, características, limites, bônus e cálculos derivados vêm do módulo da edição/setting. |
| Traços morais | Humanidade, Convicções/Tenets e Máculas/Remorso na implementação V5. | Humanidade ou Trilhas; Virtudes clássicas incluem Consciência, Autocontrole e Coragem. | Humanidade ou Trilhas e Virtudes clássicas, com degeneração do sistema clássico. | A UI pode reutilizar apresentação; modelo moral, testes e progressão não devem ser convertidos automaticamente. |
| Sangue e geração | Fome, Checagens de Sangue, Potência do Sangue e tabela Geração/Potência V5. | Pontos de Sangue e Geração clássicos; sem Fome ou Potência do Sangue V5. | Pontos de Sangue, reserva e limites definidos por Geração; sem mecânica de Fome/Potência V5. | Um controle genérico de recurso é reutilizável; economia, limites e efeitos são específicos da edição/setting. |
| Vantagens, defeitos e caça | Predator Types são escolhas de criação com concessões e regras próprias V5. | A criação usa Antecedentes como Rebanho; não foi encontrada etapa de seleção equivalente a Predator Type no livro-base consultado. | A criação usa Backgrounds como Herd; no capítulo de criação consultado não há pacote de Predator Type equivalente ao da V5. | Catálogos e regras de seleção ficam no módulo. Nome/estilo de caça não concede efeitos V5 em outra edição sem regra opcional explícita. |
| Disciplinas e magia | Disciplinas, poderes e custos V5; Taumaturgia como Feitiçaria de Sangue. | Disciplinas clássicas; Taumaturgia e Necromancia com linhas/rituais e progressões próprias. | Disciplinas clássicas e opções do livro-base moderno. | Nome/fonte/nível podem compartilhar formato; árvore, aprendizado, custo, teste e efeito são do módulo e catálogo de cada edição. |
| Clãs e cenário | Clãs, fraquezas, compulsões, seitas e conteúdo V5. | Clãs, seitas, linhagens e fraquezas do livro-base de 3ª edição. | Clãs, seitas, linhagens e fraquezas do livro-base moderno V20. | Conteúdo e regra de clã são pacotes identificados por edição/setting; não inferir compatibilidade pelo nome. |

### O que “Predator Type” significa fora da V5

Os livros-base V3 e V20 consultados não estruturam a criação como a escolha de um Predator Type que concede um pacote fixo de vantagens, defeitos, pontos e regras de caça, como na V5. Ambos têm Antecedentes/Backgrounds — incluindo Rebanho/Herd — e descrevem caça/alimentação no fluxo do jogo. Portanto:

- V5 mantém Predator Type como regra exclusiva do seu módulo.
- Em V3/V20, uma preferência ou estilo de caça pode ser anotado como dado narrativo, mas não cria automaticamente as concessões ou restrições da V5.
- Um grupo pode adotar um módulo opcional de conversão, desde que declare edição compatível, fonte e efeitos aprovados pelo Narrador. Isso será regra da mesa, não regra-base V3/V20.
- Backgrounds/Antecedentes e vantagens homônimas não devem ser mesclados só pelo nome: custos, escala e integração à criação precisam vir dos livros de cada edição.

### Estado arquitetural encontrado no HEAD 0cddd927 (antes desta atualização documental)

- `VCS.Sheet` define os dados de ficha e traços persistidos, sem referência a `VCS.Rules.V5`. Isso dá um núcleo neutro de armazenamento, mas o modelo atual ainda contém campos específicos da V5 e uma estratégia de persistência única; não é um modelo de ficha multi-edição concluído.
- `VCS.Content` fornece catálogos como JSON embutido. Os dados têm fontes por item, mas os catálogos presentes são majoritariamente V5; ainda não há resolução de pacotes por edição/módulo nem controle completo de dependências e conflitos.
- `VCS.Rules` contém contratos iniciais para resolução de poderes e proposta de efeitos de rituais. `PowerResolutionContext` ainda expõe `BloodPotency` e `RouseChecks`; o formato `PowerResolution` inclui `HungerIncrease`. O contrato não é neutro e `RitualWorkflowActions` continua sendo coordenador V5.
- `VCS.Rules.V5` implementa regras V5, validação, custos e aplicação. O projeto referencia `VCS.Content` e `VCS.Sheet`; ele não pode ser descrito como uma engine independente de dados da edição.
- `VCS.Application` contém casos de uso, mas `PredatorActions` chama diretamente `VCS.Rules.V5.PredatorRules`, e a composição injeta implementação V5 em fluxos de ficha. Há separação de camadas parcial, não seleção genérica de rulesets para todo o domínio.
- `VCS.Desktop` apresenta e edita os dados. A tela de ficha chama ações e ViewModels que coordenam distribuição; XAML exibe controles. Apresentação visual é separável, mas a interface atual é construída em torno da ficha e dos conceitos V5, sem seletor genérico de edição.
- Os pontos de investimento em vantagem/desvantagem do predador continuam persistidos separadamente para manter a concessão, o orçamento e o custo de XP sem cobrança dupla; a linha unificada é uma projeção da interface.

### O que pode ser compartilhado e o que precisa de regra de edição

**Compartilhável como infraestrutura:** operação de fichas e crônicas, persistência/versionamento, motor de rolagem de dados configurável, componentes visuais, catálogo pesquisável, atribuição de fonte, registro de exceções do Narrador, trilha de auditoria/histórico e contratos de resultado.

**Específico por edição:** criação e orçamento; modelo de atributos/habilidades/virtudes; pool e interpretação dos dados; sangue, fome e geração; moralidade; custo e progressão; clãs/linhagens; catálogo e pré-requisitos de poderes/rituais; validação de efeitos e limites.

**Módulo opcional futuro:** deve declarar identificador e versão, edição-base compatível, dependências, conflitos, conteúdo que fornece e pontos de extensão. Um módulo V5 só poderá ser habilitado numa ficha V20 se declarar compatibilidade e seus pressupostos forem satisfeitos. Compartilhar interface/tipo de conteúdo não prova compatibilidade mecânica.

### Separação em andamento e limites atuais

`VCS.Rules.Modules.RuleModuleRegistry` agora valida a composição declarada de módulos: requer uma edição instalada, exige compatibilidade explícita de extensões, dependências satisfeitas e ausência de conflitos. Ele é uma fundação de arquitetura, ainda não ligado ao ponto de composição do Desktop nem aos catálogos/fichas existentes. No estado atual, há implementação de regras V5; não há módulos V3 ou V20 registrados nem seletor por ficha.

Nenhum conteúdo V3/V20 deve herdar V5 implicitamente. “Compatível” precisa ser declarado e revisado por módulo; o fato de dois livros usarem d10 ou terem Antecedentes parecidos não constitui compatibilidade.

### Próxima etapa de arquitetura

1. Não criar ficha V20/V3 baseada em cópia superficial do modelo V5. Primeiro definir `EditionId`, formato versionado de ficha e política de migração/exportação, preservando dados existentes.
2. Generalizar os contratos atuais retirando campos V5 dos tipos comuns; manter fome, Potência do Sangue e Checagens de Sangue apenas nos contratos/implementações V5.
3. Introduzir um manifesto de módulo com compatibilidade/dependências/conflitos e registro de implementations, e compor regras + catálogo a partir da edição da ficha.
4. Fazer um protótipo de V3 com o livro anexo e validar criação, ficha e regras em fluxo isolado antes de expor seletor de edição.
5. Converter regras específicas já misturadas nos contratos comuns para tipos próprios de cada edição; integrar o registry antes de disponibilizar seleção ou módulos opcionais.
6. Disponibilizar a seleção de módulos quando a edição e seus fluxos estiverem implementados e documentados.

### Fontes

- *Vampire: The Masquerade V5 Corebook*, livro-base anexado; sistema V5 e ficha atual.
- *Vampiro: A Máscara*, 3ª edição, livro-base em português anexado, especialmente criação de personagem pp. 102–104.
- *Vampire: The Masquerade 20th Anniversary Edition*, livro-base moderno fornecido pelo usuário; criação de personagens (p. 80), antecedentes e caça.
