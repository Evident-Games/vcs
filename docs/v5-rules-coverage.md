# Cobertura de regras V5 no aplicativo

Escopo desta auditoria: o código presente no repositório nesta branch, focado no livro-base. A existência de uma regra no projeto `VCS.Rules.V5` não significa que exista uma tela ou integração completa para usá-la.

## Já há lógica implementada

| Área | Cobertura encontrada |
| --- | --- |
| Rolagens | Sucessos, dados de Fome, críticos, Crítico Bestial, Falha Bestial e rerrolagem de até três falhas comuns com dano superficial de Vontade aplicado. O rolador monta a parada por Atributo + Habilidade, permite marcar especialização aplicável (+1 dado), registrar ajuste e sua origem; também mantém a opção de parada manual. |
| Sangue | Potência/Geração, efeitos da Potência, Checagens de Sangue, Surto de Sangue, alimentação, rolagem guiada de Ressonância, bônus opcional de +1 em teste compatível e expiração ao zerar Fome ou alimentar-se novamente. Discrasia permanece registrada pela mesa e expira ao chegar a Fome 5 ou alimentar-se novamente. |
| Humanidade | Níveis e descrições, Máculas existentes, teste de Remorso, perda de Humanidade e dano de Força de Vontade por Máculas. |
| Frenesi | Alguns gatilhos de exemplo, dificuldades, resistência, encerramento em alguns casos e Compulsão de Falha Bestial resolvida na mesa de dados. Caitiff/Sangue-Ralo rerrolam o resultado 10; Compulsão de Clã continua uma decisão do Narrador. |
| Outros sistemas | Vínculo de Sangue, Torpor e uma troca oposta de conflito têm controles na ficha e registram histórico no caderno. As regras são chamadas por `ChronicleActions`. Também existem custo de avanço, criação, Predadores e fichas de conhecimento. |
| Conteúdo | Catálogos de clãs, disciplinas, poderes/rituais, vantagens, habilidades, predadores e fichas de conhecimento com dados de fonte. |

## Lacunas ou integrações incompletas

1. **Poderes de Disciplina como regras utilizáveis:** os 84 registros em `powers.json` guardam nível, custo, parada, resumo e fonte; isso é catálogo, não um resolvedor completo por poder. Faltam fluxos comuns para custo/checagem, alvos, duração, resistência, efeitos, dano/cura e consumo/atualização do estado da ficha onde a regra pedir. O catálogo inclui poderes do livro-base; poderes de suplementos continuam fora do escopo definido.
2. **Proteanismo e outras Disciplinas:** há poderes de Proteanismo no catálogo do livro-base, mas falta a integração sistemática entre seleção, pré-requisitos/amalgamas, custo, rolagem e aplicação dos efeitos. O mesmo diagnóstico vale para outras Disciplinas.
3. **Caça:** fluxo completo de cena de caça fica fora do escopo por decisão do usuário.
4. **Máculas e moralidade:** a ficha registra Máculas e resolve Remorso, mas não calcula de forma guiada a quantidade de Máculas a partir de Tenets/Convicções nem cobre o julgamento moral da cena. A decisão narrativa continua com o Narrador.
5. **Compulsões:** Falha Bestial determina a tabela no rolador; ainda falta registro persistente e aplicação narrativa. A escolha da Compulsão de Clã permanece com o Narrador.
6. **Conflitos:** a ficha resolve uma troca oposta com sucessos informados, aplica modificadores e redução de dano superficial a vampiros, e pode aplicar o dano ao personagem. Falta condução por turnos, rolagens integradas, manobras, armas/armaduras, estados da trilha e encerramento.
7. **Diablerie e Sangues-Ralos:** a ficha pode alertar sobre geração/Potência, mas não encontrei um fluxo de Diablerie com as rolagens, consequências e mudanças persistentes relacionadas. A criação e aplicação dos efeitos/limitações de Sangue-Ralo também precisam de uma revisão específica.
8. **Vínculos e Torpor:** a ficha calcula nível do vínculo e despertar natural, registra participantes/datas e mantém histórico em notas. Falta um registro estruturado de múltiplos vínculos, Vitae que desperta Torpor e efeitos de saída do Torpor.
9. **Montagem de paradas:** combina Atributo + Habilidade, especialização aplicável, ajuste situacional, origem em texto livre e bônus opcional de Ressonância/Discrasia. Ainda faltam múltiplos modificadores tipados por fonte, seletores/guias por contexto de ação e equipamento do inventário.
10. **Ressonância e Discrasia na ficha:** a rolagem, o bônus manual e as condições básicas de expiração estão integrados. Falta seleção da disciplina beneficiada, controlar uma Discrasia estruturada e validar seu consumo/efeitos específicos.
11. **Perdição do clã e dano/cura:** os dados e a gravidade da Perdição podem ser exibidos, e as trilhas de dano podem ser editadas; faltam fluxos gerais para aplicar efeitos da Perdição, dano superficial/agravado, cura e Mending conforme Potência do Sangue.
12. **Coterie, domínio e sistemas de crônica:** o escopo atual é principalmente ficha individual. Se o aplicativo também for cobrir a mesa/campanha, faltam os registros e fluxos desses sistemas do livro-base.

## Ordem sugerida

1. Completar os fluxos da ficha: criação, validação, vantagens/fichas, custos de avanço e fonte visível em todas as escolhas.
2. Ligar os casos de uso de conflito, Vínculo de Sangue e Torpor a controles e estado persistente na ficha.
3. Completar Discrasia, Máculas/Remorso, dano/cura e Perdição sem automatizar decisões narrativas do Narrador.
4. Criar um catálogo tipado de efeitos dos poderes do livro-base e integrar seus custos e rolagens, disciplina por disciplina.
5. Completar Diablerie/Sangues-Ralos e múltiplos modificadores tipados de parada.

## Próximo recorte recomendado

1. Completar conflitos por turnos com paradas, manobras, armas/armaduras e encerramento.
2. Criar resolvedores e fluxos específicos para os poderes restantes de Disciplinas, começando por Proteanismo e sem incluir conteúdo de suplementos.
3. Estruturar Diablerie/Sangues-Ralos, múltiplos modificadores de parada, dano/cura e aplicação de Perdição.

O montador atual resolve a soma básica e o bônus de especialização; o usuário informa a origem do ajuste em texto livre. Ele ainda não automatiza contexto ou equipamento.

Esta lista é uma auditoria inicial do código, não uma certificação de equivalência completa ao livro. Regras e conteúdo novos devem trazer a fonte (livro-base ou errata identificada) e ser conferidos contra a edição adotada pelo projeto antes da implementação.
