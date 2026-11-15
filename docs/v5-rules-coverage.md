# Cobertura de regras V5 no aplicativo

Escopo desta auditoria: o código presente no repositório nesta branch, focado no livro-base. A existência de uma regra no projeto `VCS.Rules.V5` não significa que exista uma tela ou integração completa para usá-la.

## Já há lógica implementada

| Área | Cobertura encontrada |
| --- | --- |
| Rolagens | Sucessos, dados de Fome, críticos, Crítico Bestial, Falha Bestial e rerrolagem de até três falhas comuns com dano superficial de Vontade aplicado. O rolador monta a parada por Atributo + Habilidade, permite marcar especialização aplicável (+1 dado), registrar ajuste e sua origem; também mantém a opção de parada manual. |
| Sangue | Potência/Geração, efeitos da Potência, Checagens de Sangue, Surto de Sangue, alimentação, rolagem guiada de Ressonância, bônus opcional de +1 em teste compatível e expiração ao zerar Fome ou alimentar-se novamente. Mending executa a Checagem de Sangue e cura dano superficial de Saúde até o valor de Mending da Potência do Sangue, inclusive quando a checagem aumenta a Fome. Discrasia permanece registrada pela mesa e expira ao chegar a Fome 5 ou alimentar-se novamente. |
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
11. **Perdição do clã e dano/cura:** Mending está integrado para recuperar dano superficial de Saúde conforme Potência do Sangue. Os dados e a gravidade da Perdição podem ser exibidos, e as trilhas de dano podem ser editadas; faltam fluxos gerais para aplicar efeitos da Perdição e para aplicar dano superficial/agravado e cura fora de Mending.
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


## Integrações adicionais

- Narrador → Eventos da crônica: tabela persistente com personagem opcional, decisões, resultados e fonte.
- Caça: abordagem e parada sugerida, dificuldade por território e ajustes aprovados; resultado da cena registrado no evento. A quantidade de sangue depende da fonte e da cena.
- Diablerie: consequências calculadas após consumo da alma e disputa informada; Humanidade, geração, XP restritos e duração da marca na aura ficam no evento. Não transfere XP restritos ao saldo geral.
- Disciplinas: ativação de poderes conhecidos com custos explícitos, rerrolagem de Sangue por Potência, autorização de exceção, alvo, efeito e duração registrados no caderno; efeitos individuais ainda precisam de resolvedores específicos.
- Dano: excesso superficial converte marcas em agravadas; excesso além da trilha é indicado para adjudicação.

Ainda não constitui implementação integral de V5: faltam resolvedores individuais dos poderes, fluxos estruturados de Sangues-Ralos, aplicação de Perdição, múltiplos vínculos estruturados, consumo específico de Discrasias, conflitos completos e modificadores tipados por fonte. Os registros do Narrador permitem documentar essas decisões, mas não substituem esses resolvedores.


## Registros estruturados e modificadores

Eventos agora persistem o estado de vínculos (outra parte, nível e último gole), efeitos (descrição, disciplina, condição de término/consumo e ativo) e conflitos (turno, paradas, Fome e modificadores de dano). É possível manter vários registros por personagem. A área do Narrador registra os turnos físicos com dano bruto; a aplicação e redução continuam explícitas na ficha.

O rolador da ficha aceita vários modificadores com motivo, fonte, valor e ativação individual. Estes são ajustes da cena atual, não bônus permanentes salvos na ficha.

Build e testes não executados por instrução do usuário nesta rodada.
