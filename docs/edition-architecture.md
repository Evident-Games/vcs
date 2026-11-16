# Regras por edição

A ficha e a apresentação devem encaminhar as ações à edição correta. `VCS.Rules` contém o contrato inicial de resolução de poderes; `VCS.Rules.V5` fornece a implementação atual. O registro identifica cada implementação pela edição e rejeita edições ausentes.

O fluxo de ativação de poderes da aplicação ainda usa Fome, Checagem de Sangue e dados da V5. Por isso ele rejeita explicitamente outras edições antes de pagar custos ou rolar dados. O identificador padrão V5 preserva o comportamento das fichas existentes; ainda não existe seletor nem edição persistida por ficha.

Para acrescentar outra edição, primeiro defina seus próprios modelos de custos, dados, catálogo e ficha, implemente seus serviços e registre-os no ponto de composição. Não reutilize mecanicamente pressupostos V5 como Fome ou Potência de Sangue. Migração e identificação de edição por ficha, exportação, fontes e compatibilidade de dados devem ser feitas antes de disponibilizar a seleção.

A extração da interface não automatiza poderes ou rituais pendentes. O inventário detalhado continua em `power-effect-coverage.md`.

## Rituais do livro base V5

`VCS.Rules.Rituals.IRitualEffectRuleset` expõe um contexto de resultado independente do rolador WPF e uma proposta de efeito. `V5RitualEffectRuleset` interpreta esse contexto, enquanto `RitualOngoingRules` calcula ajustes condicionais e proteção. Nenhuma regra de V20/V3/V2/V1 foi acrescentada.

`RitualWorkflowActions` coordena etapas, pagamentos unitários, testes, repetições de Dagon, doses e disputas dos círculos. Esse coordenador ainda é **específico da V5**: usa Fome, Checagem de Sangue e suas paradas. O contrato dos efeitos não transforma esse fluxo em um sistema universal de custos. Cada futura edição precisará do próprio coordenador.

`RitualWorkflowState` e o histórico por intruso são campos opcionais de `PowerEffectEntry`, armazenado no JSON existente. Registros anteriores continuam válidos, com estado nulo; não se criam colunas ou migrações. A interface escolhe o preparo/efeito, recebe confirmações e apresenta dados/resultados na mesa existente. O catálogo fornece ingredientes, tempo e gatilhos; a ficha conserva os textos que o usuário já personalizou.

O encerramento e as noites de cegueira de Olhos do Falcão Noturno usam `RitualOngoingRules` e `RitualWorkflowActions`, fora do ViewModel. A interface só envia a decisão confirmada e a data da crônica; a interpretação de três noites é V5. Exceções antecipadas são registradas explicitamente, sem reaproveitar automaticamente uma autorização do preparo.


## Mapa comparativo: V5, V20 Dark Ages e V3

### Escopo e fontes

- **V5:** livro-base V5 fornecido pelo usuário e implementação ativa no repositório.
- **V3:** *Vampiro: A Máscara*, 3ª edição, livro-base em português fornecido pelo usuário.
- **“V20” disponível para esta comparação:** o anexo é *V20 Dark Ages Core Rulebook* (2015), ambientado no mundo medieval. Ele **não é** o livro-base moderno *Vampire: The Masquerade 20th Anniversary Edition*. As notas abaixo identificam explicitamente isso; não devem ser tratadas como especificação do V20 moderno.
- O mapa serve à arquitetura; não é uma transcrição exaustiva de regras. Quando a comparação precisa de regra detalhada — especialmente custos, limites de geração, combate ou disciplina — ela fica pendente do livro/trecho correto.

### Diferenças mecânicas e de conteúdo confirmadas

| Subsistema | V5 | V3 (livro-base) | V20 Dark Ages (anexo) | Consequência para módulos |
| --- | --- | --- | --- | --- |
| Resolução de ações | Pool de dados de Atributo + Habilidade, d10; sucessos e regra de Fome/críticos próprios da V5. | Pool de Atributo + Habilidade, d10; regras clássicas de dificuldade, falha crítica e especialidades. | Também apresenta o Storyteller System com pools/dificuldades e regras clássicas, em cenário medieval. | Um lançador de dados genérico é compartilhável. Montagem de pool, dificuldade, sucessos, falhas, críticos e gasto de Força de Vontade são políticas do ruleset. |
| Criação | Distribuição e validações próprias da V5; ficha usa Potência do Sangue, Geração e Humanidade como trilhas V5. | Criação em cinco passos. Atributos 7/5/3; Habilidades 13/9/5 (Talentos/Perícias/Conhecimentos); 3 Disciplinas, 5 Antecedentes, 7 Virtudes; 15 pontos de bônus; Humanidade = Consciência + Autocontrole; Força de Vontade = Coragem (V3, pp. 102–104). | O sumário do livro localiza criação nas pp. 151–156, com Atributos, Habilidades, Vantagens, Virtudes e Road; os valores e exceções de criação ainda precisam ser lidos diretamente nessa seção. | Os conceitos podem compartilhar formulários/configuração. Distribuições, características obrigatórias, pontos iniciais, bônus e cálculos derivados são perfil de edição. |
| Traços morais | Humanidade, Convicções/Tenets e Máculas/Remorso na implementação V5. | Humanidade ou Trilhas; Consciência/Autocontrole e Coragem são Virtudes clássicas que afetam moralidade e resistência à Besta. | Roads são parte central do cenário e do sistema medieval, com ética e Virtudes próprias da Trilha/Road. | A UI pode reutilizar campos e apresentação de pontos; o modelo moral e seus testes precisam ser plugins separados. Não converter valores automaticamente. |
| Sangue e geração | Fome, Checagens de Sangue, Potência do Sangue e tabela Geração/Potência V5. | Pontos de Sangue, geração e limite de gasto/armazenamento clássicos; não há Potência do Sangue/Fome V5. | Pontos de sangue e geração clássicos, ajustados ao contexto Dark Ages; sem a estrutura de Fome/Potência da V5. | Recursos podem ter uma interface genérica de trilha/contagem, mas economia, limites e ações sobre sangue pertencem à edição. |
| Vantagens e desvantagens | Catálogo moderno de vantagens/desvantagens com predadores e exceções narrativas registradas. | Antecedentes clássicos (ex.: Aliados, Contatos, Fama, Geração, Rebanho, Recursos, Status), comprados na criação e por pontos de bônus/XP; não são semanticamente iguais a todos os merits/flaws posteriores. | O apêndice do anexo organiza Merits/Flaws opcionais em categorias (inclui sociais e sobrenaturais), além dos Backgrounds clássicos. | Um catálogo genérico pode hospedar itens, fontes e limites. Categorias, custo, disponibilidade, pré-requisitos e integração à criação devem ser dados e regras do módulo. |
| Disciplinas e magia | Disciplinas e poderes V5 com níveis, custos e regras V5; Taumaturgia é Feitiçaria de Sangue com poderes/rituais V5. | Disciplinas clássicas e árvores/poderes próprios; Taumaturgia e Necromancia usam linhas e rituais, com progressão/custos clássicos específicos. | Mais conteúdo de Dark Ages: disciplinas/caminhos/rituais medievais e opções de clã/bloodline próprias do anexo. | O registro comum de poder pode conter nome/fonte/nível. Aprendizado, árvore, custo, teste e efeito devem vir de cada módulo e catálogo de edição. |
| Clãs e cenário | Clãs, fraquezas, compulsões, seitas e conteúdo V5. | Livro-base de 3ª edição possui seleção de clãs/seitas/linhagens própria e mecânicas de fraqueza/clã. | Clãs altos/baixos, Roads, bloodlines e opções medievais adicionais; o conteúdo do anexo diverge do livro-base moderno V20. | Catálogos comuns são transportáveis; conteúdo e regra de clã são pacotes identificados por edição/setting. |

### Estado arquitetural encontrado no HEAD 35fcf757

- `VCS.Sheet` define os dados de ficha e traços persistidos, sem referência a `VCS.Rules.V5`. Isso dá um núcleo neutro de armazenamento, mas o modelo atual ainda contém campos específicos da V5 e uma estratégia de persistência única; não é um modelo de ficha multi-edição concluído.
- `VCS.Content` fornece catálogos como JSON embutido. Os dados têm fontes por item, mas os catálogos presentes são majoritariamente V5; ainda não há resolução de pacotes por edição/módulo nem controle completo de dependências e conflitos.
- `VCS.Rules` contém contratos iniciais para resolução de poderes e proposta de efeitos de rituais. `PowerResolutionContext` já inclui `BloodPotency` e `RouseChecks`, e `RitualWorkflowActions` continua sendo coordenador V5. Portanto, o contrato não é plenamente neutro ainda.
- `VCS.Rules.V5` implementa regras V5, validação, custos e aplicação. O projeto referencia `VCS.Content` e `VCS.Sheet`; ele não pode ser descrito como uma engine independente de dados da edição.
- `VCS.Application` contém casos de uso, mas `PredatorActions` chama diretamente `VCS.Rules.V5.PredatorRules`, e a composição injeta implementação V5 em fluxos de ficha. Há separação de camadas parcial, não seleção genérica de rulesets para todo o domínio.
- `VCS.Desktop` apresenta e edita os dados. A tela de ficha chama ações e ViewModels que coordenam distribuição; XAML exibe controles. Apresentação visual é separável, mas a interface atual é construída em torno da ficha e dos conceitos V5, não de cinco edições selecionáveis.
- Os pontos de investimento em vantagem/desvantagem do predador continuam persistidos separadamente para manter a concessão, o orçamento e o custo de XP sem cobrança dupla; a linha unificada é uma projeção da interface.

### O que pode ser compartilhado e o que precisa de regra de edição

**Compartilhável como infraestrutura:** operação de fichas e crônicas, persistência/versionamento, motor de rolagem de dados configurável, componentes visuais, catálogo pesquisável, atribuição de fonte, registro de exceções do Narrador, trilha de auditoria/histórico e contratos de resultado.

**Específico por edição:** criação e orçamento; modelo de atributos/habilidades/virtudes; pool e interpretação dos dados; sangue, fome e geração; moralidade; custo e progressão; clãs/linhagens; catálogo e pré-requisitos de poderes/rituais; validação de efeitos e limites.

**Módulo opcional futuro:** deve declarar identificador e versão, edição-base compatível, dependências, conflitos, conteúdo que fornece e pontos de extensão. Um módulo V5 só poderá ser habilitado numa ficha V20 se declarar compatibilidade e seus pressupostos forem satisfeitos. Compartilhar interface/tipo de conteúdo não prova compatibilidade mecânica.

### Próxima etapa de arquitetura

1. Não criar ficha V20/V3 baseada em cópia superficial do modelo V5. Primeiro definir `EditionId`, formato versionado de ficha e política de migração/exportação, preservando dados existentes.
2. Generalizar os contratos atuais retirando campos V5 dos tipos comuns; manter fome, Potência do Sangue e Checagens de Sangue apenas nos contratos/implementações V5.
3. Introduzir um manifesto de módulo com compatibilidade/dependências/conflitos e registro de implementations, e compor regras + catálogo a partir da edição da ficha.
4. Fazer um protótipo de V3 com o livro anexo e validar criação, ficha e regras em fluxo isolado antes de expor seletor de edição.
5. Obter o livro-base **V20 moderno**. O anexo Dark Ages não sustenta especificação do V20 moderno.

### Fontes

- *Vampire: The Masquerade V5 Corebook*, livro-base anexado; sistema V5 e ficha atual.
- *Vampiro: A Máscara*, 3ª edição, livro-base em português anexado, especialmente criação de personagem pp. 102–104.
- *V20 Dark Ages Core Rulebook* (2015), anexo do usuário, sumário pp. 4–6 e exemplos/categorias de conteúdo do cenário. Este arquivo é identificado como Dark Ages e não substitui o livro-base V20 moderno.
