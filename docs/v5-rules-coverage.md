# Cobertura de regras V5 no aplicativo

Escopo desta auditoria: o código presente no repositório nesta branch, focado no livro-base. A existência de uma regra no projeto `VCS.Rules.V5` não significa que exista uma tela ou integração completa para usá-la.

## Já há lógica implementada

| Área | Cobertura encontrada |
| --- | --- |
| Rolagens | Sucessos, dados de Fome, críticos, Crítico Bestial, Falha Bestial, limite de rerrolagem de dados comuns e extensão de testes. O rolador agora monta a parada por Atributo + Habilidade, permite marcar especialização aplicável (+1 dado) e somar ajuste situacional; também mantém a opção de parada manual. |
| Sangue | Potência/Geração, efeitos da Potência, Checagens de Sangue, Surto de Sangue, alimentação, Ressonância e Discrasia. |
| Humanidade | Níveis e descrições, Máculas existentes, teste de Remorso, perda de Humanidade e dano de Força de Vontade por Máculas. |
| Frenesi | Alguns gatilhos de exemplo, dificuldades, resistência, encerramento em alguns casos e classificação da Compulsão. |
| Outros sistemas | Vínculo de Sangue, duração/retorno do Torpor, rolagem oposta de conflito e partes de dano, custo de avanço, criação, Predadores e fichas de conhecimento. |
| Conteúdo | Catálogos de clãs, disciplinas, poderes/rituais, vantagens, habilidades, predadores e fichas de conhecimento com dados de fonte. |

## Lacunas ou integrações incompletas

1. **Poderes de Disciplina como regras utilizáveis:** os 84 registros em `powers.json` guardam nível, custo, parada, resumo e fonte; isso é catálogo, não um resolvedor completo por poder. Faltam fluxos comuns para custo/checagem, alvos, duração, resistência, efeitos, dano/cura e consumo/atualização do estado da ficha onde a regra pedir. O catálogo inclui poderes do livro-base; poderes de suplementos continuam fora do escopo definido.
2. **Proteanismo e outras Disciplinas:** há poderes de Proteanismo no catálogo do livro-base, mas falta a integração sistemática entre seleção, pré-requisitos/amalgamas, custo, rolagem e aplicação dos efeitos. O mesmo diagnóstico vale para outras Disciplinas.
3. **Caça:** as regras de saciedade por fonte existem, mas não há fluxo completo de cena de caça com abordagem, parada/dificuldade, margem, sucesso/falha, complicações e atualização de Fome/Ressonância.
4. **Máculas e moralidade:** a ficha registra Máculas e resolve Remorso, mas não calcula de forma guiada a quantidade de Máculas a partir de Tenets/Convicções nem cobre o julgamento moral da cena. A decisão narrativa continua com o Narrador.
5. **Compulsões:** resultados de rolagem sinalizam que uma Compulsão ocorreu; há classificação/tabela parcial, mas falta um fluxo integrado para determinar/aplicar a Compulsão adequada, incluindo a escolha da Compulsão de Clã.
6. **Conflitos:** existe resolução matemática de uma troca oposta e redução de dano superficial; falta uma ferramenta integrada para conduzir conflito físico/social/mental por turnos, incluindo paradas, manobras, armas/armaduras, estados da trilha e encerramento.
7. **Diablerie e Sangues-Ralos:** a ficha pode alertar sobre geração/Potência, mas não encontrei um fluxo de Diablerie com as rolagens, consequências e mudanças persistentes relacionadas. A criação e aplicação dos efeitos/limitações de Sangue-Ralo também precisam de uma revisão específica.
8. **Vínculos e Torpor:** há cálculos de regras, mas falta uma experiência de ficha para registrar histórico, participantes/datas, condições de despertar e alterações decorrentes.
9. **Montagem de paradas:** já combina Atributo + Habilidade, uma especialização marcada pelo jogador e ajuste situacional informado pela mesa. Ainda faltam seletores/guias por contexto de ação, equipamento do inventário, situações comuns e controle de modificadores por fonte. A escolha do Narrador continua manual.
10. **Ressonância e Discrasia na ficha:** há funções para intensidade, dados extras e expiração, mas os efeitos não entram automaticamente nas rolagens nem têm um fluxo completo para registrar consumo/expiração.
11. **Perdição do clã e dano/cura:** os dados e a gravidade da Perdição podem ser exibidos, e as trilhas de dano podem ser editadas; faltam fluxos gerais para aplicar efeitos da Perdição, dano superficial/agravado, cura e Mending conforme Potência do Sangue.
12. **Coterie, domínio e sistemas de crônica:** o escopo atual é principalmente ficha individual. Se o aplicativo também for cobrir a mesa/campanha, faltam os registros e fluxos desses sistemas do livro-base.

## Ordem sugerida

1. Completar os fluxos da ficha: criação, validação, vantagens/fichas, custos de avanço e fonte visível em todas as escolhas.
2. Fazer o rolador montar paradas a partir da ficha e ligar os resultados a uma mesa de rolagem comum.
3. Integrar Sangue, caça, Ressonância, Compulsões e Remorso sem automatizar decisões narrativas do Narrador.
4. Criar um catálogo tipado de efeitos dos poderes do livro-base e integrar seus custos e rolagens, disciplina por disciplina.
5. Avançar em conflitos e sistemas de campanha caso o escopo do VCS inclua ferramentas para o Narrador.

## Próximo recorte recomendado

1. Registrar de onde veio cada modificador da parada (especialização, equipamento, ajuda ou circunstância) para a mesa revisar o total.
2. Expor rerrolagens de Vontade sobre até três dados comuns já falhados, sem permitir selecionar dados de Fome ou sucessos.
3. Completar o registro de caça/ressonância após consolidar a regra por abordagem e fonte com a edição exata do livro-base usada no projeto.

O montador atual resolve a soma básica e o bônus de especialização. Ele não automatiza contexto, equipamento ou julgamento do Narrador.

Esta lista é uma auditoria inicial do código, não uma certificação de equivalência completa ao livro. Regras e conteúdo novos devem trazer a fonte (livro-base ou errata identificada) e ser conferidos contra a edição adotada pelo projeto antes da implementação.
