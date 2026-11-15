# Regras por edição

A ficha e a apresentação devem encaminhar as ações à edição correta. `VCS.Rules` contém o contrato inicial de resolução de poderes; `VCS.Rules.V5` fornece a implementação atual. O registro identifica cada implementação pela edição e rejeita edições ausentes.

O fluxo de ativação de poderes da aplicação ainda usa Fome, Checagem de Sangue e dados da V5. Por isso ele rejeita explicitamente outras edições antes de pagar custos ou rolar dados. O identificador padrão V5 preserva o comportamento das fichas existentes; ainda não existe seletor nem edição persistida por ficha.

Para acrescentar outra edição, primeiro defina seus próprios modelos de custos, dados, catálogo e ficha, implemente seus serviços e registre-os no ponto de composição. Não reutilize mecanicamente pressupostos V5 como Fome ou Potência de Sangue. Migração e identificação de edição por ficha, exportação, fontes e compatibilidade de dados devem ser feitas antes de disponibilizar a seleção.

A extração da interface não automatiza poderes ou rituais pendentes. O inventário detalhado continua em `power-effect-coverage.md`.
