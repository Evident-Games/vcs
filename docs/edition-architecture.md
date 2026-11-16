# Regras por edição

A ficha e a apresentação devem encaminhar as ações à edição correta. `VCS.Rules` contém o contrato inicial de resolução de poderes; `VCS.Rules.V5` fornece a implementação atual. O registro identifica cada implementação pela edição e rejeita edições ausentes.

O fluxo de ativação de poderes da aplicação ainda usa Fome, Checagem de Sangue e dados da V5. Por isso ele rejeita explicitamente outras edições antes de pagar custos ou rolar dados. O identificador padrão V5 preserva o comportamento das fichas existentes; ainda não existe seletor nem edição persistida por ficha.

Para acrescentar outra edição, primeiro defina seus próprios modelos de custos, dados, catálogo e ficha, implemente seus serviços e registre-os no ponto de composição. Não reutilize mecanicamente pressupostos V5 como Fome ou Potência de Sangue. Migração e identificação de edição por ficha, exportação, fontes e compatibilidade de dados devem ser feitas antes de disponibilizar a seleção.

A extração da interface não automatiza poderes ou rituais pendentes. O inventário detalhado continua em `power-effect-coverage.md`.

## Rituais do livro base V5

`VCS.Rules.Rituals.IRitualEffectRuleset` expõe um contexto de resultado independente do rolador WPF e uma proposta de efeito. `V5RitualEffectRuleset` interpreta esse contexto, enquanto `RitualOngoingRules` calcula ajustes condicionais e proteção. Nenhuma regra de V20/V3/V2/V1 foi acrescentada.

`RitualWorkflowActions` coordena etapas, pagamentos unitários, testes, repetições de Dagon, doses e disputas dos círculos. Esse coordenador ainda é **específico da V5**: usa Fome, Checagem de Sangue e suas paradas. O contrato dos efeitos não transforma esse fluxo em um sistema universal de custos. Cada futura edição precisará do próprio coordenador.

`RitualWorkflowState` e o histórico por intruso são campos opcionais de `PowerEffectEntry`, armazenado no JSON existente. Registros anteriores continuam válidos, com estado nulo; não se criam colunas ou migrações. A interface escolhe o preparo/efeito, recebe confirmações e apresenta dados/resultados na mesa existente. O catálogo fornece ingredientes, tempo e gatilhos; a ficha conserva os textos que o usuário já personalizou.
