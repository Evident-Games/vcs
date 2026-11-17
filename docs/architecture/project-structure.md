# Estrutura de pastas do VCS

Este documento descreve a organização-alvo do código para manter o núcleo compartilhado separado das regras de cada edição. A migração é gradual e ainda não está concluída.

## Estrutura atual

Os projetos existentes foram movidos fisicamente para as pastas Shared, Editions/V5 e Desktop. Seus nomes de projeto e namespaces `VCS.*` foram mantidos. Essa mudança de caminhos não significa que todo o conteúdo seja neutro: há modelos, catálogos e fluxos V5 em Shared que ainda precisam ser separados.

- `src/Shared/Sheet`, `Content`, `Rules`, `Application` e `Infrastructure`: projetos existentes, ainda com dependências e conteúdo V5 a separar.
- `src/Editions/V5/Rules`: projeto de regras V5, incluindo o contrato de poderes atual que contém campos V5.
- `src/Editions/V5/Application`: casos de uso V5, inclusive ficha, sangue, poderes, rituais, dados e crônica; namespaces anteriores preservados.
- `src/Editions/V5/Content/Data`: catálogos JSON V5, incorporados ao assembly `VCS.Content.V5` com os nomes de recurso anteriores. `VCS.Content` ainda referencia explicitamente esse assembly; a composição dinâmica por edição permanece pendente.
- `src/Desktop`: interface WPF, navegação e temas.
- `tests/VCS.Tests`: projeto de testes existente; testes ligados à V5 estão em `Editions/V5`, enquanto os demais ainda incluem testes compartilhados e de interface. A divisão física não cria projetos de teste separados.
- V3 e V20 ainda não têm projetos de regras.

O registro de compatibilidade está em VCS.Rules.Modules.RuleModuleRegistry. Ele ainda não está conectado à composição do aplicativo. As fichas também ainda não armazenam a edição selecionada.

## Estrutura-alvo

Os diretórios deixam de repetir VCS. Os nomes de namespace C# podem continuar começando por VCS para evitar conflitos e preservar uma identidade clara para os tipos.

- src/
  - Shared/
    - Sheet/ — estrutura comum da ficha, identidade, histórico e dados portáveis
    - Content/ — contratos e formatos comuns de catálogos e fontes
    - Rules/
      - Modules/ — manifesto, seleção, compatibilidade, dependências e conflitos
      - Disciplines/ — contratos neutros de poderes
      - Rituals/ — contratos neutros de rituais
    - Application/ — operações que não pertencem a uma edição
    - Infrastructure/ — banco, arquivos, exportação, importação e migrações
  - Editions/
    - V5/
      - Content/ — catálogos e fontes V5
      - Rules/ — criação, sangue, dados, disciplinas, predadores, rituais e validação V5
      - Application/ — fluxos de uso específicos da V5
    - V3/
      - Content/ — catálogos e fontes V3
      - Rules/ — criação, sangue, disciplinas, moralidade e validação V3
      - Application/ — fluxos de uso específicos da V3
    - V20/
      - Content/ — catálogos e fontes V20
      - Rules/ — criação, sangue, disciplinas, moralidade e validação V20
      - Application/ — fluxos de uso específicos do V20
  - Desktop/ — interface, temas e apresentação dos fluxos disponibilizados
- tests/
  - Shared/ — comportamento comum
  - Editions/
    - V5/
    - V3/
    - V20/

## O que fica compartilhado

- Persistência da ficha, identificadores, histórico, registro de fontes e exceções aprovadas pelo Narrador.
- Componentes visuais, navegação, busca de catálogos e apresentação de resultados.
- Infraestrutura de rolagem reutilizável, desde que não interprete regras de uma edição.
- Contratos para solicitar uma ação e devolver um resultado sem impor custos ou características de uma edição.

## O que fica dentro de cada edição

Criação de personagem, atributos e habilidades permitidos, custos e orçamento, sangue, dados e interpretação de sucessos, moralidade, clãs, vantagens, predadores, disciplinas, poderes, rituais, requisitos, efeitos e validações. Um módulo de outra edição só pode ser usado quando declarar compatibilidade e quando a mesa aprovar seus pressupostos.

## Ordem de migração

1. Criar a nova organização sem alterar os dados das fichas; preservar as fichas atuais como V5 por padrão.
2. Mover o código existente gradualmente para Shared e Editions/V5, atualizando a solução e as referências entre projetos em cada recorte.
3. Retirar campos V5 dos contratos compartilhados e colocá-los nos contratos V5.
4. Separar catálogos e fontes por edição.
5. Implementar e registrar V3 e V20 com base nos livros-base fornecidos.
6. Conectar a seleção de módulos ao aplicativo e só então permitir escolher uma edição na ficha.

A árvore não torna uma regra compatível por semelhança. Mudanças de pasta não devem alterar regras, aparência dos temas, dados salvos ou resultados já registrados. Build e testes permanecem pendentes enquanto estiverem proibidos pelo usuário.
