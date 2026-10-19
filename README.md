# VCS - Vampire Character Sheet

Aplicativo desktop Windows para criar e organizar fichas pessoais de RPG,
inspirado no fluxo de ferramentas de criação de personagens e adaptado a uma
ficha básica de Vampiro: A Máscara 5ª Edição. Interface própria, sem código,
imagens, textos descritivos ou catálogos proprietários do GCS ou dos livros.

## Requisitos

- Windows x64 compatível com .NET 10; desenvolvimento e validação em Windows 11.
- SDK .NET 10 LTS **10.0.200 ou posterior da linha 10.0**.
  A seleção está em [global.json](global.json).
- PowerShell e acesso ao NuGet na primeira restauração.
- Visual Studio com suporte a .NET 10 e desenvolvimento desktop, ou VS Code
  com C# Dev Kit, são opcionais: os comandos abaixo bastam.
- Não é necessário instalar servidor SQLite ou workload MAUI.
- Janela mínima: 1000 x 680 unidades independentes de DPI.

## Como executar

### Só abrir, sem configurar banco

Abra `artifacts\portable-0.14.0\VCS.Desktop.exe` com duplo clique.
É um executável único e autocontido: pode copiar somente esse arquivo para
outra pasta ou computador Windows x64. Não precisa de instalador, privilégios
de administrador, .NET instalado, criação manual de banco ou comandos.

Esta versão usa um banco provisório **persistente**, criado automaticamente
em `%LOCALAPPDATA%\VCS\Provisorio\vcs.db`. Suas fichas continuam disponíveis
ao fechar e reabrir. O banco anterior `%LOCALAPPDATA%\VCS\vcs.db` não é
alterado nem importado automaticamente; isso isola o esquema de uma versão
experimental anterior. Não é necessário apagá-lo para abrir o aplicativo.

### Executar a partir do código-fonte

Na raiz do repositório:

```powershell
dotnet restore VCS.slnx
dotnet build VCS.slnx
dotnet run --project src\VCS.Desktop
dotnet publish .\src\VCS.Desktop\VCS.Desktop.csproj -c Release -p:PublishProfile=WindowsX64 -o .\artifacts\portable
```

A primeira execução aplica as migrações e abre uma lista vazia. Use
**Arquivo → Novo personagem**, edite a ficha e volte por **Arquivo → Lista de personagens**.
Para abrir uma ficha na lista, selecione-a e use **Arquivo → Abrir ficha**, Enter ou
duplo clique. A busca
filtra nome, clã e crônica sem diferenciar maiúsculas e minúsculas.

## Funcionalidades desta etapa

- Criar, abrir, editar, duplicar e excluir personagens com confirmação.
- Ficha contínua de identidade, atributos, habilidades, disciplinas,
  vantagens / desvantagens, estado, história / convicções e anotações.
- Três modos de exibição em **Exibir → Modo de exibicao**: lista contínua, abas e
  bento compacto; agrupamentos livres e seções configuráveis.
- Todos os campos de identidade; nove atributos básicos; listas de traços
  pessoais com nome e pontuação; seleção editável de clã.
- Tipo de predador e clã têm ícones `?` junto à seleção. O `?` do predador
  mostra benefícios e defeitos ao passar o mouse ou clicar; a aplicação dos bônus
  fica em Vantagens. O campo "Predador registrado" não é mais exibido.
- Faixas de geração de referência: Criança da Noite 12ª–16ª, Neófito 12ª–13ª
  e Ancillae 10ª–11ª. Gerações mais baixas são permitidas e indicadas como
  possível diablerie; não bloqueiam a ficha.
- Trilhas de Fome e Humanidade; Potência de Sangue; capacidade e danos de
  Saúde e Força de Vontade.
- Salvamento manual e automático, indicação de alterações pendentes,
  validação por campo e resumo dos erros.
- Datas de criação e alteração em UTC; revisão para concorrência otimista.
- Dump manual, restauração protegida e importação/exportação JSON.
- Cinco temas padrão e temas de clã; Branco e vermelho como padrão, com tema e layout persistidos entre execuções.
- Sem serviços remotos, telemetria própria ou conta de usuário.

Habilidades possuem 27 referências no SQLite, compartilhadas por personagens
e NPCs, e continuam aceitando entradas pessoais. A tela de habilidades mostra
Físicas, Sociais e Mentais em três colunas, sem paginação: a lista rola junto com a
ficha. Um cabeçalho compacto mostra o perfil inicial, as notas (`4 · 3 · 3 …`), o que
falta, as especializações grátis e o XP gasto, cada um com um `?` explicativo. Passar o
mouse no nome da habilidade mostra sua descrição. Especializações são editadas abaixo
das bolinhas (placeholder "+ especialização"), uma por linha. O botão `+` ao lado do
selo "N esp." (ou Enter) cria outra linha logo abaixo; cada habilidade mostra até 2
linhas e, a partir da 3ª, as setas `‹ ›` paginam de 2 em 2. As três colunas são montadas
por linha, então uma habilidade com mais especializações não desalinha as vizinhas.
Linhas vazias somem ao sair do campo. A especialização do predador aparece como linha
travada, destacada na cor do tema. Vantagens e
desvantagens possuem o catálogo SQLite descrito abaixo e aceitam entradas pessoais.
Disciplinas possuem o catálogo pessoal
descrito abaixo e continuam aceitando entradas personalizadas.
O seletor inclui os 14 clãs fornecidos pelo usuário e os nomes pessoais já
usados nas fichas.
Não há seed de personagens. Os catálogos atuais se baseiam no livro básico de
Vampire: The Masquerade V5.
Os valores iniciais de uma ficha são apenas um rascunho editável, não uma
distribuição válida de pontos de criação. Atributos e habilidades exibem os
níveis que faltam na distribuição inicial escolhida. Cada grupo de habilidades
mantém uma linha pessoal vazia; ao começar a preenchê-la, outra linha vazia aparece
abaixo. Se nome e especializações forem apagados, a habilidade é removida
automaticamente. As entradas pessoais guardam seu grupo e continuam editáveis ao
reabrir. Os campos de texto usam linha inferior sem moldura, como pauta de ficha.

### Ficha completa e menus

O editor abre como uma ficha contínua, sem aba de atributos separada por padrão:

- Identidade em três colunas verticais: nome, nome mortal e crônica; conceito,
  ambição e desejo; sire, idade/geração, clã e predador. Descrição fica em
  História e convicções.
- Atributos físicos, sociais e mentais, com pontos editáveis.
- Saúde, Força de Vontade, Fome, Humanidade e Potência de Sangue.
- Informações do clã e suas três disciplinas.
- Habilidades, disciplinas/poderes/rituais, vantagens e desvantagens.
- Ambição, desejo, ressonância, princípios da crônica, pilares, convicções
  e perdição do clã.
- Idade real/aparente, nascimento, morte, aparência, marcas e história.
- Referências de Surto de Sangue, recuperação de dano, bônus de poderes,
  rerrolagem, penalidade de alimentação e severidade da perdição.
- Experiência total, gasta e disponível, além das anotações.

Idades, datas e referências de sangue são preenchidas livremente pelo usuário.
A experiência disponível é total menos gasta. O custo dos pontos excedentes é
calculado automaticamente; saldo negativo é permitido e mostra quanto XP falta.
Selecionar poderes conhecidos não gera outra cobrança além dos níveis de disciplina.
Valores técnicos inválidos (como XP recebido negativo ou acima do limite) continuam rejeitados.

### Pontos iniciais e XP automático

Não existe painel de Criação e evolução, seletor de compras, etapa de conclusão
ou botão de registro de base. Edite os pontos diretamente na ficha.
- **Identidade:** nome, conceito, sire, idade/geração, clã e predador em três
  colunas verticais; o `?` do predador resume benefícios e defeitos.
- **Atributos:** distribuição gratuita, custo dos excedentes e avisos.
- **Habilidades:** perfil inicial, distribuição, especializações gratuitas e custos.
- **Disciplinas:** distribuição 2/1, bônus de predador e custos por nível.
- **Vantagens:** tabelas de vantagens e defeitos, com orçamento por idade,
  pontos selecionados/restantes, custos de XP e escolhas do predador.
- **Estado:** Potência de Sangue e custo dos níveis acima do inicial.
- **História e convicções:** descrição, XP da idade, XP de campanha concedido pelo mestre,
  total, gasto calculado, saldo e detalhamento. Se faltar XP, o aviso informa o
  valor exato, sem impedir autosave, JSON, duplicação ou PDF.

NPCs usam os mesmos cálculos, mas seus três grupos de atributos continuam livres
de 0 a 5, sem cobrar uma distribuição individual que não faz parte dessa ficha.
Seleções vazias mostram um texto indicativo, como **Clã** ou **Disciplina**,
sem salvar esse texto como valor.

| Idade | XP inicial | Potência de Sangue | Vantagens | Desvantagens |
|---|---:|---:|---:|---:|
| Criança da noite | 0 | 0 | 7 | 2 |
| Neófito | 15 | 1 | 7 | 2 |
| Ancillae | 35 | 2 | 9 | 4 |

- Atributos: 1 com 4, 3 com 3, 4 com 2 e 1 com 1. Quando os nove pontos
  formam exatamente essa distribuição (inclusive ao abrir a ficha), a base de
  cada atributo é registrada. A partir daí, cada atributo é cobrado da sua
  própria base (Vigor 1→5 custa 10+15+20+25 = 70 XP), e cada linha mostra a
  base, o XP gasto e o custo do próximo ponto. Baixar um atributo abaixo da
  base libera a redistribuição automaticamente. Sem base registrada, a
  distribuição é tratada como personalizada e os atributos não geram XP.
- Habilidades, conforme os perfis fornecidos pelo usuário; demais em zero:
  Especialista: 4,3,3,3,2,2,2,2,1; Equilibrado: 3,3,3,2,2,2,2,2,1;
  Pau para toda obra: 4,3,2,2,2,2,2,1,1. Não são a distribuição de atributos.
- Disciplinas iniciais: duas do clã, uma com 2 e outra com 1.
- Excedentes: atributo e disciplina do clã = novo nível × 5;
  disciplina fora do clã = próximo nível × 7; habilidade = próximo nível × 3;
  Potência de Sangue = próximo nível × 10; vantagem = 3 XP por ponto.
  Desvantagens não geram XP.
- Especializações: 3 XP cada após consumir as gratuitas aplicáveis.
  Uma gratuita por ficha; a especialização escolhida no predador é adicional
  e aplicada automaticamente, sem consumir essa gratuidade.
  Cada habilidade com 4+ ganha uma local. Erudição/Acadêmicos, Ofícios/Artesanato,
  Performance e Ciência ganham outra local ao receber o primeiro ponto.
  Bônus são cumulativos, inclusive na criação; podem ser usados depois.
- Os bônus de predador são adicionais à distribuição básica e não custam XP;
  precisam de aplicação explícita pelo painel descrito abaixo.
- Os pontos são ordenados do maior para o menor para aplicar a distribuição
  gratuita automaticamente; somente níveis acima das vagas gratuitas são cobrados.
  Reduzir pontos ou trocar perfil/idade recalcula os custos, sem compras duplicadas.
- Ao abrir uma ficha antiga, os pontos existentes passam pelo cálculo automático.
  O recebido anterior é preservado como campanha, descontando o bônus de idade
  quando já existia perfil. Gastos antigos deixam de ser o total autoritativo;
  os dados históricos antigos permanecem armazenados por compatibilidade.
- Informe somente o XP recebido do mestre. O total soma campanha e idade.
  Trocar idade preserva os níveis adicionais de Potência e os bônus de predador.

#### Predadores

Em **Identidade**, use `?` para consultar benefícios e defeitos. Escolher o tipo
já aplica o predador; os efeitos aparecem nas próprias seções da ficha.
os 13 tipos fornecidos: Vira-lata, Chantagista, Assassino na Estrada, Trinchador,
Consensualista, Osíris, Scene Queen, Sereia, Sandman, Ladrão de Sepultura,
Sacoleiro, Sanguessuga e Fazendeiro. As definições estão no SQLite.

Em **Disciplinas** e **Habilidades**, as opções elegíveis mostram o símbolo
discreto **✧** (como o ◆ do clã), e o cabeçalho da seção traz a legenda
"✦ Predador: …" com as opções. Clicar no símbolo marca a escolha (vira ✦ em destaque):
na disciplina já soma o +1 gratuito; na habilidade já adiciona a especialização.
Clicar de novo desmarca. Quando a opção pede tradição, cena, animal ou outra
especialização pessoal, uma linha de texto aparece abaixo da habilidade.
A habilidade precisa ter pelo menos um ponto. Enquanto faltar a disciplina ou a
especialização, a criação lista o que falta.

Vantagens e defeitos do predador aparecem no topo das linhas de **Vantagens e
desvantagens**, com barra e nome destacados. Itens fixos não editam. Enquanto
houver pontos livres, **todas** as opções do predador aparecem com
"predador · N pontos livres · até M"; clique nas bolinhas para alocar os pontos
(em qualquer opção, inclusive defeitos de 2 pontos como Isca de Estaca). Com tudo
alocado, as opções restantes somem; retirar pontos faz elas reaparecerem.
Uma linha discreta acima das vantagens resume o predador com **remover**.

- A disciplina recebe +1 sobre a distribuição básica; a especialização escolhida
  é gratuita, sem gerar outra gratuidade genérica.
- Vantagens e defeitos são entradas identificadas como **Predador**, adicionais
  aos orçamentos básicos de 7/2 ou 9/4; não são compras de XP.
- Humanidade e Potência de Sangue são ajustadas apenas na aplicação.
  Trocar a idade preserva o +1 de Potência de Sanguessuga.
- Trocar/remover desfaz somente os efeitos registrados.
  As entradas adicionais são protegidas contra edição direta. Se um efeito
  registrado tiver sido alterado, a operação é recusada com erro explícito.
- Sacoleiro e Fazendeiro rejeitam Ventrue; Fazendeiro rejeita Potência 3+
  na escolha. Feitiçaria de Sangue nestes predadores só está disponível para Tremere.
- Nomes de predador em fichas antigas permanecem texto, **sem aplicação retroativa**.
  O fluxo de aplicação também funciona em NPCs, sem ativar uma etapa de criação.

#### Humanidade, Máculas e Remorso

Em **Estado**, PC e NPC exibem a referência do nível atual de Humanidade e
uma lista expansível de todos os níveis 0–10, usando os resumos fornecidos.
Penalidades sociais, Rubor da Vida, alimentação e sol são referências da mesa;
o aplicativo não inventa rolagens, dificuldades ou cálculos de dano solar.

Registre as Máculas no campo numérico. **Aplicar 1 Agravado na Vontade** é uma
ação separada, exige Máculas e espaço na trilha e não consome as Máculas.
Trilha cheia é recusada, sem sobrescrever marcas nem converter danos por suposição.
**Falha no Remorso** exige Máculas, limpa todas e reduz a Humanidade em 1,
atomicamente; repetir sem Máculas ou reduzir abaixo de zero é recusado.
O contador admite 0–1.000.000 por validação técnica, não como limite de regra.

Humanidade zero mostra **Wassail**, mas mantém a ficha e permite editá-la.
Nenhum botão apaga o personagem ou bloqueia a aplicação. Seleções, bônus e
Máculas acompanham autosave, duplicação, JSON, backup e PDF preenchível.

#### Fontes de XP

O total é **XP inicial da idade + XP de campanha recebido do mestre**.
Neófito tem 15 XP iniciais: um custo de 20 XP com campanha zero mostra
**Faltam 5 XP**, mas permite salvar. Informar 5 XP de campanha zera a falta.
Custos comuns consomem primeiro o bônus inicial. Loresheets antigos marcados
como exclusivos de campanha continuam separados dessa verba.

### Loresheets / Fichas de Conhecimento

Em **Vantagens e desvantagens**, loresheets são vantagens da categoria
**Ficha de conhecimento**: adicione uma linha, escolha a categoria e digite ou
selecione o benefício. A sugestão e a linha escolhida mostram a coleção abaixo do nome.
Os **20 benefícios fornecidos pelo usuário** são persistidos no SQLite:
**Os Bahari, Theo Bell, Heresia Cainita e Carna**, cinco benefícios de 1 a 5 pontos
por coleção. As quatro coleções são **gerais**, conforme confirmado; a classificação
de linhagem/descendência está preparada para futuras entradas, sem inventar conteúdo.

- Cada benefício é comprado individualmente. Comprar o de 5 pontos não concede
  os anteriores nem exige comprá-los; custa 5 pontos ou 15 XP, não a soma dos níveis.
- Todo personagem tem no mínimo **7 pontos de vantagens e 2 de defeitos**
  (Ancillae: 9 e 4). Cada ponto de vantagem acima da base custa **3 XP**, e o
  resumo da seção mostra base, usados, faltam/excedente e o XP gasto.
- Na criação, uma ficha **geral** e uma de **linhagem** podem sair dos pontos de
  vantagem; qualquer outra é paga com XP (3 por ponto) e indicada como "pago com XP".
- Remover um benefício recalcula e devolve o custo. Não há etapa de conclusão
  nem bloqueio por saldo insuficiente; benefícios repetidos continuam rejeitados.
- Seleções guardam coleção, classificação, custo, descrição e forma de pagamento.
  Acompanham autosave, duplicação, JSON, dump e PDF preenchível, para PC e NPC.
  Efeitos, seguidores, bônus, penalidades e poderes descritos não são executados automaticamente.

A organização usa a ficha-base fornecida como referência, sem incorporar
o PDF, logotipo ou artes. O visual acompanha o tema escolhido no VCS.
A ficha usa rolagem vertical para permanecer acessível no tamanho mínimo.
O menu superior reúne:

- **Arquivo**: Novo personagem (Ctrl+N), Abrir ficha (Ctrl+O), Salvar (Ctrl+S),
  lista de personagens, Novo NPC, Importar JSON, Exportar JSON e Exportar PDF preenchível.
- **Editar**: Duplicar personagem e Excluir personagem com confirmação.
- **Exibir**: Modo de exibicao (lista contínua, abas ou bento compacto), Organizar abas, mostrar/ocultar cada seção e escolher em **Temas**:
  **Padrao** (Preto e branco, Branco e preto, Branco e vermelho, Preto e vermelho,
  Amarelo e vermelho) ou **Clas** (Banu Haqim, Brujah, Gangrel, Malkavianos,
  Nosferatu, Tremere, Ventrue, Toreador, Caitiff, O Ministério, Ravnos, Salubri,
  Tzimisce e Sangue-Ralo).
- **Ferramentas**: Criar dump do banco e Restaurar dump, com confirmação.
- **Ajuda**: instruções de navegação e atalhos.

As ações de duplicar, excluir e exportar usam a ficha aberta; na lista,
usam a seleção. Trocar de ficha salva o rascunho ou pede confirmação para
descartá-lo caso esteja inválido. Duplicar, exportar e criar dump exigem
salvar a ficha aberta com sucesso.

Em **Exibir → Organizar abas**, cada seção tem visibilidade, título editável e
nome de aba. A ordem é definida arrastando a alça **⋮⋮** de cada linha: as outras
seções abrem espaço enquanto você arrasta. Pelo teclado, foque a alça e use
**Alt + seta para cima/baixo**. Seções com o mesmo nome de aba (ignorando espaços
nas pontas e maiúsculas/minúsculas) aparecem juntas, e a aba agrupada fica na
posição da primeira seção do grupo. Por padrão, Identidade e Atributos
compartilham uma aba; as demais ficam separadas. Por exemplo, escreva
`Ficha principal` em Identidade, Atributos e Estado e sangue para reuni-las.
Um nome vazio usa o título da própria seção. Nomes admitem até 80 caracteres.
Escolha **Lista continua** para voltar à ficha contínua, sem perder os grupos.
O modo **Bento compacto** mostra todas as seções visíveis de uma vez, em blocos
que preenchem a janela sem rolagem geral: Identidade, Atributos, Habilidades e
Anotações ocupam meia largura, as demais um quarto. O conteúdo fica 15% menor e
cada bloco rola internamente quando necessário. Abaixo de 900 px a grade usa
duas colunas. O modo escolhido é salvo com as preferências de exibição.

**Fichas abertas em abas:** cada ficha aberta (PC ou NPC) ganha uma aba no topo;
clique para alternar e no **×** para fechar. Abrir, criar ou importar outra ficha
não fecha as anteriores — um rascunho inválido continua em sua própria aba.
Com duas ou mais fichas abertas, o modo **Bento compacto** as mostra **lado a lado**
em uma grade automática (2 → 2 colunas, até 4 → 2×2, mais → 3 colunas), podendo
misturar PCs e NPCs. Clicar em um painel o torna ativo; **Voltar** mantém as abas.
É possível deixar apenas Atributos visível, por exemplo.
Esconder uma seção não apaga dados nem marca a ficha como alterada.
As duas formas de exibição compartilham validação, cálculos e autosave.
As preferências ficam em `display-settings.json`, ao lado do banco; elas não
fazem parte do dump nem do JSON do personagem.

A **lista lateral de personagens** vem visível e pode ser ocultada em **Exibir**.
Ela inclui busca arredondada de 30 px, com entrada de 24 px e **+** circular de 24 px
integrado à direita. Clicar no **+** abre **PC / Personagem** e **NPC / Ficha prática**.
Um clique em uma ficha abre seu editor;
a troca usa o mesmo fluxo de salvamento/validação da lista principal, sem
descartar alterações inválidas sem confirmação.

O tema inicial é **Branco e vermelho**. A primeira cor define o fundo e a
segunda orienta os destaques. Textos longos e campos usam tons neutros para
reduzir fadiga; o vermelho fica nos títulos, trilhas e indicadores.
O amarelo usa tons suaves de papel. Campos, seleções, hover e foco acompanham
a paleta; listas selecionadas usam fundo suave e contorno, sem inverter o texto.
O contraste de textos e destaques nas superfícies principais, campos e
seleções é validado em pelo menos 4,5:1 nos cinco temas padrão; as paletas de
clã mantêm as cores escolhidas e não passam por essa validação.

As paletas ficam em `src\VCS.Desktop\Themes\Default` e `src\VCS.Desktop\Themes\Clan`
e são registradas em `Services\ThemeCatalog.cs` (id, nome, pasta e arquivo).
Cada paleta separa o texto em duas cores:

- `VcsText`: texto de dentro — cartões, seções, campos, listas e menus abertos;
- `VcsOuterText`: texto de fora — direto sobre `VcsBackground` (título da janela,
  menu principal, nome da ficha, quadro de Humanidade e barra de status).

Uma paleta sem `VcsOuterText` usa `VcsText` nos dois lugares. Se o fundo
(`VcsBackground`) for escuro, os controles WPF UI entram no modo escuro.
As paletas também atualizam a cor de destaque dos controles WPF UI.
Preferências antigas de escuro/claro são preservadas como Preto e vermelho
ou Branco e vermelho; os demais ajustes de exibição não são alterados.
Os templates de campos, botões, caixas de seleção, listas, menus, expansores,
abas, dicas e seletores usam explicitamente as superfícies da paleta,
inclusive o campo editável de clã, o popup e as opções selecionadas.
As barras de rolagem vertical e horizontal têm trilho e puxador próprios,
visíveis em todos os temas; o contraste mínimo do puxador é 3:1.
Quando uma lista interna chega ao início/fim, a roda do mouse continua a
rolagem da ficha externa. Dentro de uma lista ainda rolável, ela não move a ficha.

### Organização compacta e pontos

As abas, agrupamentos e ordens existentes são preservados. A identidade usa
campos de 30 unidades de altura mínima, cantos de sete unidades e espaçamento
reduzido; as seções usam cartões arredondados. O seletor de clã mantém uma
área de clique maior. As descrições das habilidades ficam apenas na dica ao
passar o mouse; não existem expansores de descrição por habilidade.
As disciplinas usam linhas compactas e custos de evolução na dica, sem uma
rolagem interna para os níveis. Poderes e rituais ficam em linhas abaixo delas. Loresheets continuam com painéis de consulta
de altura limitada e rolagem própria, além da rolagem principal da ficha.

As trilhas de bolinhas não possuem mais botão “0”. Clique num ponto para
definir o nível; repetir no ponto correspondente ao nível atual reduz um ponto.
Assim, no nível 1, clicar novamente no ponto 1 zera habilidades, disciplinas,
atributos e demais trilhas que aceitam zero. Um atributo em 0 deixa a
distribuição personalizada (sem cobrança de XP), como qualquer valor abaixo da base.
Abaixo das bolinhas de cada atributo ficam duas linhas fixas, da largura da trilha:
base/XP gasto e custo do próximo ponto; assim a coluna não muda de largura.
Essa interação também vale nas fichas de NPC. O valor numérico ao lado é apenas
um indicador, não um botão.

### Habilidades compartilhadas

As 27 habilidades fornecidas pelo usuário são inseridas na tabela `SkillCatalog`:
nove Físicas, nove Sociais e nove Mentais, com nomes, descrições e IDs estáveis.
Ambos os editores exibem três colunas com pontos de 0 a 5; as descrições estão
na dica ao passar o mouse sobre o nome da habilidade. Habilidades pessoais podem
ser criadas diretamente na linha vazia abaixo de cada grupo, sem botão ou painel
separado de edição livre. Ao abrir uma ficha, entradas ausentes são apresentadas com zero;
elas são persistidas no próximo salvamento, sem marcar a abertura como alteração.
Nomes antigos equivalentes, ignorando acentos e caixa, conservam chave e pontuação.
Ao lado dos pontos de cada habilidade há **especialização em texto livre**,
até 500 caracteres. Separe especializações por ponto e vírgula, vírgula ou nova linha;
barras permanecem parte do texto. As especializações respeitam os créditos gratuitos;
as excedentes custam 3 XP cada, calculados ao editar, sem botão de compra.
Não há bônus automático de dados. O texto acompanha autosave, duplicação, JSON, dump e PDF,
tanto em personagens quanto NPCs; habilidades pessoais também o aceitam.

### Fichas de NPC

Use **Arquivo → Novo NPC** ou **+ → NPC** na lateral. O tipo de ficha fica
salvo no próprio agregado, não em outro banco, e acompanha duplicação, JSON e dump.
Fichas existentes continuam como personagens; não há conversão automática.

A ficha de NPC é uma **ficha rápida** em cartões, no mesmo visual da ficha de jogador:

- **Base**: Nome, Crônica, Clã e Geração.
- Vitalidade e Força de Vontade em caixas; o total é um campo digitável entre as
  bolinhas **− / +**, sem limite prático (até 100); clique alterna vazia → cheia → X
  (Força de Vontade só vazia/X). Não é possível remover caixas já marcadas.
- Potência de Sangue; **Surto de Sangue**, **Quantidade recuperada** e **Bônus de poder**
  são preenchidos pela tabela V5 ao mudar a potência, mas continuam editáveis —
  um texto personalizado não é sobrescrito.
- Fome em 5 caixas.
- **Ataques**: começa com um bloco (Ataque, Parada de dados, Bônus de dano,
  Tipo de dano, Disciplinas ativas e Notas); **+** adiciona e **×** remove (até 50).
- **Atributos** agrupados (Físicos, Sociais, Mentais, 0–5).
- **Habilidades**: as 27 habilidades em três colunas, com especializações.
- **Disciplinas**: 3 espaços (disciplina sugerida ou personalizada + bolinhas);
  **+** adiciona e **×** remove. Poderes e rituais usam as mesmas tabelas da ficha de jogador.
- **Vantagens e defeitos**: mesmo layout da ficha de jogador (categoria, nome com catálogo, bolinhas, **×** remove, **+** adiciona), começando com 3 linhas de vantagem e 3 de defeito, sem orçamento/XP.
- Todos os botões da ficha de NPC são bolinhas, no mesmo tema da ficha de jogador.
  Trocar o tipo move o item entre vantagens e defeitos sem perder nome/valor.
- **Histórico**: idade verdadeira e aparente, Notas, História e Aparência.

O PDF de NPC espelha essa ficha inteira, independente da seleção de seções,
e não inclui XP, Humanidade nem Máculas.
Reduzir uma capacidade abaixo das marcas existentes exibe validação, sem apagar
danos automaticamente nem sobrescrever a ficha salva.
Os nove atributos individuais internos são preservados por compatibilidade,
mas não definem as capacidades do NPC nem aparecem em seu PDF.

Os detalhes de poderes incluem disciplina, descrição base, ingredientes, custo,
parada de dados e funcionamento. Descrição base e ingredientes não fornecidos
são identificados como não informados. O funcionamento usa o resumo recebido,
sem inventar texto oficial ou regras detalhadas.

### PDF preenchível e adaptativo

**Arquivo → Exportar PDF preenchível** exporta a ficha aberta ou selecionada.
O aplicativo exige salvar um rascunho válido primeiro.
O PDF possui layout próprio A4, sem copiar logotipo, arte ou páginas do anexo.
Usa PDFsharp (MIT) e formulários AcroForm nativos, sem JavaScript.

- Personagens respeitam as seções visíveis, seus títulos e a ordem de seções
  configurados em **Exibir → Organizar abas**. Abas são convertidas em fluxo de impressão.
- NPCs começam pela ficha rápida (identidade, caixas, sangue, fome e ataques);
  seções extras só entram se forem pedidas explicitamente.
- O conteúdo ganha páginas conforme necessário; notas e textos extensos são
  divididos em campos de continuação para não serem cortados.
- Textos são editáveis e os pontos/caixas são clicáveis. Pontos são caixas
  independentes: marque/desmarque a quantidade desejada, sem cálculo automático.
- Caixas de dano usam seletores preenchíveis vazia/cheia/X; na Força de Vontade
  do NPC, somente vazia/X. A aparência inicial mantém o desenho da trilha.
- O arquivo pode ser salvo em um leitor compatível com formulários PDF,
  como Adobe Acrobat Reader. A compatibilidade varia em visualizadores de navegador.
- Alterações no PDF **não retornam ao banco** e não são importadas pelo JSON.
  O documento é uma cópia independente, não uma sincronização da ficha.

### Vantagens e defeitos no banco

As **43 entradas fornecidas pelo usuário** (18 vantagens e 25 defeitos)
são cadastradas na tabela SQLite `AdvantageCatalog`, com identificador estável,
grupo, nome, tipo, descrição e limites de pontos. Os dez grupos são Visual,
Refúgio, Recursos, Alimentação, Membros, Acompanhando o Tempo, Mítico, Máscara,
Linguística e Mortais. Nomes e descrições seguem o material recebido, sem
certificação de completude ou correção das regras oficiais.

Na seção **Vantagens e desvantagens**, vantagens e defeitos são tabelas de linhas,
no mesmo padrão dos poderes: **Categoria · Nome · Pontos**. Use **+** para
adicionar uma linha, **▸** para ver a referência e escrever anotações e **×** para remover.

- **Categoria** navega pelas famílias (Beleza, Mortais, Refúgio…) e filtra as
  sugestões do nome. O grupo Visual aparece como **Beleza**.
- **Nome** aceita escrita livre ou escolha na lista (digitando ou pelo ▾). Sem
  categoria, a lista mostra todas as opções com família e pontos; escolher uma
  preenche a categoria. Opções já escolhidas em outra linha não aparecem e,
  se digitadas, exibem aviso.
- Custos fixos e faixas respeitam os limites do catálogo. Um nome fora do
  catálogo vira característica própria (0 a 5 pontos) na categoria escolhida.
Conforme confirmado, **Inimigo** mantém `1+` como referência, mas permite
de 1 a 5 pontos no aplicativo.

- Beleza admite uma escolha: Belo, Deslumbrante, Feio ou Repulsivo.
- Recursos/Destituído e Status/Suspeito/Segregado também são exclusivos entre si.
  As demais opções podem acumular, inclusive as outras características de Membros.
- Uma nova escolha exclusiva substitui a anterior. Linhas pessoais incompatíveis
  ficam com zero pontos, preservando seus textos. Conflitos antigos são exibidos
  com aviso, sem remoção automática ao abrir a ficha.
- Entradas antigas sem família aparecem em **Outras características**.
  Características de predador ficam no topo, destacadas e protegidas; só a
  distribuição variável pode ser ajustada pelos pontos.

As seleções, famílias e descrições pessoais acompanham autosave, duplicação,
JSON (formato 10, ainda aceita 1–9), backup e PDF preenchível, tanto em PC quanto NPC.
Os pontos excedentes continuam entrando no cálculo automático de XP; defeitos
não devolvem XP. Descrições não executam efeitos, bônus ou penalidades automaticamente.

O catálogo é inserido por migração uma única vez, sem criar personagens nem
adicionar traços automaticamente às fichas. Abrir o aplicativo atualiza
bancos anteriores preservando personagens e preferências.

### Clãs e custo das disciplinas

O catálogo contém Banu Haqim, Brujah, Gangrel, Malkavianos, O Ministério,
Nosferatu, Ravnos, Salubri, Toreador, Tremere, Tzimisce, Ventrue, Hecata e
Lasombra, com as descrições fornecidas pelo usuário. Assamitas, Setitas,
Giovanni, Malkavian e variações sem acentos são reconhecidos sem reescrever
o texto salvo na ficha.

As três disciplinas de cada clã foram conferidas pela associação de símbolos
na página 3 do PDF fornecido, **Monte Um Vampiro**. O PDF e suas imagens não
são redistribuídos pelo aplicativo. Vicissitude não foi criada como uma
disciplina separada: no catálogo V5 usado aqui, Tzimisce tem Animalismo,
Dominação e Proteanismo.

O PDF não especifica custos de evolução de disciplinas; o usuário confirmou:

- Disciplina do clã: **novo nível × 5 XP**.
- Disciplina fora do clã: **novo nível × 7 XP**.

A interrogação ao lado do clã exibe suas disciplinas e perdição. Nas trilhas e
nos detalhes de poderes, o aplicativo indica **Do clã / Fora do clã** e o
custo do **próximo nível da disciplina**, não o custo de comprar o poder
selecionado. Por exemplo, subir de 2 para 3 custa 15 XP ou 21 XP.
No nível 5 é informado o limite, sem calcular um sexto nível.
Rituais não recebem esses multiplicadores. Para clã vazio ou personalizado,
o custo fica explicitamente indefinido.

O resumo de perdições no `?` segue o texto fornecido para os 14 clãs e pode
ser consultado por hover ou clique. A geração tem controle numérico com espaço
para os botões de incremento e decremento; Sire fica abaixo da Crônica na
primeira coluna da Identidade.

A mudança de clã recalcula as informações sem apagar disciplinas/poderes,
sem conceder pontos e sem gastar XP. Disciplinas fora do clã permanecem
selecionáveis. A distribuição inicial 2/1 é descontada automaticamente dos custos;
os pontos excedentes geram XP gasto, sem exigir conclusão de criação.

### Disciplinas, poderes e rituais

A seção **Disciplinas** inclui as **84 entradas fornecidas pelo usuário**:
79 poderes de 11 disciplinas e 5 rituais em uma seção separada.
O arquivo fornecido está incorporado ao executável em
[Disciplines.txt](src/VCS.Infrastructure/Data/Disciplines.txt).
Nomes, níveis, custos, paradas de dados e resumos foram mantidos como enviados,
sem certificação de correção ou de completude das regras oficiais.
Entradas compostas, como dois nomes separados por `/`, continuam sendo uma
única entrada, conforme o material recebido.

- O cabeçalho segue o das habilidades: distribuição inicial, o que falta em
  bolinhas (ex.: `Faltam: 1 × ●● · 1 × ●`), disciplinas do clã e XP gasto.
  Atributos e habilidades também mostram o que falta assim
  (`1 × ●●●● · 3 × ●●● · 4 × ●● · 1 × ●`).
- **Poderes** ficam em linhas de papel, como as especializações:
  Disciplina | Nível | Poder | Custo | Parada de dados | Duração. O `+` adiciona
  uma linha; o nome abre uma lista filtrada pela disciplina da linha (busca sem
  acentos), mas aceita texto livre. Escolher um poder do catálogo preenche os
  campos, que continuam editáveis. O `▸` abre os extras (pré-requisito e
  descrição) e **Restaurar do catálogo** desfaz os ajustes.
- **Rituais e cerimônias** usam um bloco completo: disciplina, nível, nome,
  custo de ativação, parada de dados, ingredientes, processo, sistema,
  pré-requisito e descrição.
- Todas as 11 disciplinas aparecem em trilhas de **0 a 5 bolinhas**, em três
  colunas (as do clã primeiro, marcadas com ◆); 0 significa não adquirida. Níveis existentes são preservados; as
  demais começam em 0, sem conceder níveis ou marcar o rascunho como alterado.
  A próxima gravação inclui as trilhas no banco. O painel de edição pessoal
  continua disponível para nomes e disciplinas personalizadas.
- **Potência de Sangue** também usa bolinhas, mantendo sua faixa de **0 a 10**.
- Quimerismo, Fata Morgana, Obeah e Aliviando a Alma Bestial incluem os
  requisitos de amálgama fornecidos. Eles aparecem nos detalhes e geram
  avisos quando faltam os níveis necessários, sem bloquear a seleção.
- Entradas pessoais existentes são reutilizadas quando o nome coincide,
  ignorando acentos, maiúsculas e espaços nas extremidades.
- Poderes acima do nível da disciplina geram **somente aviso**, sem impedir
  seleção ou salvamento. Para rituais, o aviso usa o nível de Feitiçaria de
  Sangue. Nenhum limite de quantidade, pré-requisito não fornecido ou regra de
  aquisição foi presumido.
- Reduzir ou remover uma disciplina não remove poderes selecionados; o
  aviso é atualizado. Desmarcar um poder não apaga a disciplina.
- Poderes permanecem ao reabrir, duplicar, importar/exportar ou restaurar um backup.

As seleções são traços da categoria `DisciplinePower`. Poderes do catálogo usam
chave estável derivada do nome e da disciplina; poderes próprios recebem uma
chave `power-…`. Os detalhes editados (`PowerInfo`) ficam na coluna JSON
`Power` de `CharacterTraits` (migração `DisciplinePowerDetails`); poderes do
catálogo sem ajustes gravam apenas a referência. O PDF usa os detalhes editados
quando existem. Uma seleção importada de
outro catálogo é preservada e aparece como **Fora do catálogo**, com aviso de
que os detalhes não estão disponíveis, em vez de ser descartada silenciosamente.
O catálogo não altera automaticamente custos, rolagens, capacidades ou dano.
Rituais não possuem trilha de disciplina nem entram na relação das 11
disciplinas: ficam no bloco **Rituais e cerimônias**, associados por padrão a Feitiçaria de Sangue.
O catálogo de poderes/rituais é incorporado ao EXE; somente as seleções e
pontuações da ficha são gravadas em `CharacterTraits`.

## Salvamento e validação

O autosave aguarda **900 ms** após a última alteração. Alterações em listas e
pontuações também o acionam. As gravações são serializadas, e uma edição
realizada durante uma gravação permanece pendente para a próxima.

Um rascunho inválido **não substitui** o último estado válido no banco.
**Salvar agora**, voltar à lista, exportar, criar backup ou fechar a janela
tentam concluir a gravação. Ao voltar ou fechar, caso ela não seja possível,
o usuário pode permanecer na ficha ou confirmar o descarte das alterações
pendentes. Falhas de armazenamento ficam visíveis e permitem nova tentativa.

Regras básicas implementadas:

- Nome obrigatório, até 120 caracteres; limites individuais de texto.
- Geração de 1 a 16; Fome de 0 a 5; Humanidade e Potência de Sangue de 0 a 10.
- Atributos e demais traços de 0 a 5 (poderes de disciplina de 1 a 5); até 200 traços por ficha.
- Só o campo com erro recebe o contorno de validação; o restante da ficha não é destacado.
- Saúde = Vigor + 3.
- Força de Vontade = Autocontrole + Determinação.
- Dano superficial e agravado não negativos; a soma não excede a capacidade.
- Os nove atributos devem existir, com identificadores únicos e estáveis.

Estas regras são validações do MVP, **não um motor completo das regras V5**.
Criação e custos de evolução seguem as distribuições confirmadas pelo usuário.
Não há restrições adicionais por geração,
resolução automática de dano, execução de efeitos de poderes,
ressonância ou rolagens.
O usuário registra manualmente o dano já resolvido pela mesa.

## Banco local e logs

Local padrão:

```text
%LOCALAPPDATA%\VCS\Provisorio\vcs.db
%LOCALAPPDATA%\VCS\Provisorio\logs\vcs-AAAAMMDD.log
```

Logs Serilog têm retenção de 14 arquivos diários. O aplicativo permite uma
instância por banco na sessão Windows. SQLite usa chaves estrangeiras e
transações; não se deve editar o banco em uso com ferramentas externas.

Para desenvolvimento isolado, configure um caminho absoluto:

```powershell
$env:VCS_DB_PATH = Join-Path $env:TEMP 'VCS-dev\vcs.db'
dotnet run --project src\VCS.Desktop
Remove-Item Env:\VCS_DB_PATH
```

A pasta é criada automaticamente, e os logs acompanham esse caminho.
Banco, JSON e backups **não são criptografados**. Proteja-os pelas permissões
da conta Windows e não os versione.

### Modelo relacional

| Tabela | Conteúdo |
| --- | --- |
| `Characters` | `Id` GUID, identidade e estado de `CharacterDetails`, anotações, `CreatedUtc`, `UpdatedUtc` e `Revision` |
| `CharacterTraits` | `Id` GUID, `CharacterId` obrigatório, categoria, chave, nome e pontuação |
| `CharacterTraits.Specialization` | Texto livre das especializações de habilidades, vazio nas fichas antigas |
| `Characters.Details_Advancement` | JSON opcional com etapa, idade, perfil, base inicial e compras de XP; vazio em fichas antigas |
| `AdvantageCatalog` | 43 vantagens/defeitos comuns + 20 benefícios de loresheets, com coleção e classificação |
| `CharacterTraits.Loresheet` | Metadados opcionais do benefício: coleção, classificação, descrição e pagamento |
| `Characters.Details_CampaignExperienceTotal/Spent` | XP recebido/gasto de campanha como parte dos totais |
| `SkillCatalog` | 27 habilidades compartilhadas, grupos e descrições |
| `PredatorCatalog` | 13 tipos, descrições, disciplinas, especializações e opções de bônus/custos |
| `Characters.Details_PredatorSelection` | JSON opcional com escolhas e registro dos efeitos aplicados |
| `Characters.Details_Stains` | Máculas registradas, zero nas fichas antigas |
| `__EFMigrationsHistory` | Histórico de migrações do Entity Framework Core |

`CharacterDetails` é um Value Object imutável mapeado na própria tabela do
personagem. A relação com traços é 1:N com exclusão em cascata. Há índice no
nome e índice único em personagem + categoria + chave do traço. Datas são
armazenadas como UTC; a revisão é controlada pela aplicação, não por
`rowversion`, que não é suportado nativamente pelo SQLite.

### Migrações

As migrações `InitialCreate`, `CompleteCharacterSheet`, `AdvantageCatalog`, `SkillsAndNpcSheets`,
`SkillSpecializations`, `CharacterAdvancement`, `LoresheetsAndCampaignXp`, `PredatorsAndHumanity`,
`GroupedMeritDetails`, `PersonalSkillGroups`, `DisciplinePowerDetails` e `NpcQuickSheet` já estão em
[Persistence/Migrations](src/VCS.Infrastructure/Persistence/Migrations).
O aplicativo aplica migrações pendentes no início e rejeita histórico de
versões desconhecidas mais recentes. A segunda migração adiciona os campos
da ficha completa, preservando os personagens existentes com texto vazio
e experiência zero nos novos campos. Dumps anteriores também são atualizados
na cópia temporária antes da restauração.
A terceira migração cria e preenche o catálogo com as 43 entradas, sem
modificar os traços dos personagens existentes.
A quarta adiciona o catálogo de habilidades e os campos de NPC; fichas antigas
mantêm `IsNpc=false`, atributos agrupados zero e totais auxiliares de cinco caixas.
A quinta adiciona especializações de habilidades com texto vazio, sem alterar
nomes, chaves ou pontuações dos traços existentes.
A sexta adiciona a base e o histórico de evolução, como JSON em uma coluna
opcional de `Characters`; fichas antigas ficam livres, sem recalcular gastos.
A sétima adiciona os 20 benefícios, suas coleções, metadados de seleção e contadores
de campanha com zero por padrão; preserva personagens, valores e histórico de XP.
A oitava insere os 13 predadores e adiciona o registro opcional de aplicação
e Máculas zero, sem alterar os efeitos ou o texto das fichas anteriores.
A nona adiciona os metadados opcionais de família e descrição pessoal aos traços
de vantagens/defeitos, sem alterar pontos, identificadores ou nomes anteriores.
A décima registra o grupo de habilidades pessoais, sem alterar os traços existentes.
A décima primeira guarda detalhes de poderes; a décima segunda adiciona os ataques
do NPC (`Details_NpcAttacks`, JSON com padrão `[]`), preservando fichas antigas.

Para aplicar manualmente, com o aplicativo fechado:

```powershell
dotnet tool restore
dotnet ef database update --project src\VCS.Infrastructure
```

O factory de design usa o mesmo caminho padrão ou `VCS_DB_PATH`.
Para evoluir o modelo:

```powershell
dotnet ef migrations add NomeDaAlteracao --project src\VCS.Infrastructure --output-dir Persistence\Migrations
dotnet ef database update --project src\VCS.Infrastructure
```

Não use `EnsureCreated` em substituição às migrações. Gere um backup antes de
evoluir um banco com dados importantes.

### Backup e restauração

**Ferramentas → Criar dump do banco** usa a API de backup do SQLite para produzir uma cópia consistente,
inclusive quando o banco usa WAL. O arquivo é criado temporariamente ao lado
do destino e publicado somente após sucesso. É um banco SQLite, não um script SQL.

**Ferramentas → Restaurar dump** exige confirmação e trabalha primeiro em uma cópia temporária:
verifica a integridade, o histórico de migrações, os relacionamentos e a
validade das fichas. Só então cria um arquivo
`vcs.db.before-restore-<identificador>.bak` ao lado do banco atual e substitui
o conteúdo pela API SQLite. Essa cópia anterior pode ser restaurada pelo
mesmo menu. Não há limpeza automática dessas cópias de segurança.

Um backup de versão futura ou sem esquema VCS conhecido é rejeitado. Erros
de validação não sobrescrevem o banco atual. A restauração substitui todas as
fichas; não é uma operação de mesclagem.

### JSON

Arquivos usam `System.Text.Json`, envelope com `FormatVersion: 11` e objeto
`Character`. A importação também aceita os formatos 1 a 10 anteriores,
usando os valores padrão quando faltam os campos novos.
O formato 2 introduziu `DisciplinePower`; o formato 3 inclui os campos
adicionais de identidade, história, sangue e experiência. O formato 4 acrescenta
tipo de ficha, atributos agrupados e capacidades do NPC. O formato 5 acrescenta
`Specialization` aos traços, com texto vazio ao importar formatos anteriores.
O formato 6 acrescenta `Advancement`, com idade, perfil, base gratuita e histórico
de compras, preservados para leitura de dados antigos. O formato 7 adiciona loresheets e
fontes de XP; campos de campanha ausentes começam em zero, sem inferir sua origem.
O formato 8 adiciona Máculas e a seleção de predador com origem dos efeitos;
sem esses campos, não aplica nenhum bônus de predador retroativamente.
O formato 9 identifica o cálculo automático de XP: totais e gastos são conferidos
contra os pontos, mas XP faltante é aviso e não bloqueia a importação.
O formato 10 adiciona a família e a descrição das vantagens/defeitos pessoais;
fichas antigas continuam aceitas e não perdem suas características.
O formato 11 registra o grupo das habilidades pessoais, preservando e exibindo
as novas linhas no grupo em que foram criadas.
Aplicativos anteriores
podem não conseguir ler esses arquivos. Categorias são nomes de enum, não números. Importações validam
o formato e as mesmas regras do domínio; arquivos acima de 5 MB, propriedades
desconhecidas e versões não suportadas são rejeitados.

**Importar sempre cria um novo personagem**, com novos IDs, datas e revisão,
sem sobrescrever uma ficha pelo ID do arquivo. A exportação usa arquivo
temporário e mantém o destino anterior se a serialização falhar.

## Testes

```powershell
dotnet test VCS.slnx
```

Os testes usam diretórios temporários exclusivos, migrações reais e SQLite
em arquivo, sem tocar no banco pessoal. Cobrem domínio, cálculos, validação,
CRUD, cascata, concorrência, JSON, backup, restauração e os ViewModels,
incluindo atualização de banco/dump antigo, JSON legado e preferências.
Também verificam as 84 entradas do catálogo, IDs estáveis, separação de
rituais, filtros, avisos não bloqueantes e persistência das seleções.
Também cobrem as 27 habilidades, preservação de habilidades antigas, fichas NPC,
trilhas editáveis, exportação PDF com campos realmente preenchíveis, aparências,
paginação, textos longos e limites físicos dos campos.
O contraste também é testado nos elementos visuais de fato renderizados, não só
nas cores da paleta, incluindo menus e popups. Há testes de arraste vertical/
horizontal, passagem da roda entre áreas, clique para zerar e especializações.

O smoke test WPF roda em uma thread STA: abre a janela, verifica bindings,
edita pela View, aguarda o autosave real, verifica as oito seções contínuas,
ativa abas, visita os agrupamentos, reúne seções pela tela de configuração,
oculta Atributos sem perda de dados, troca temas,
seleciona poderes e rituais pela View, verifica que foram salvos,
reduz a janela ao tamanho mínimo e exige o evento de fechamento. Por isso,
a suíte completa requer Windows com suporte à criação de janelas WPF.

## Publicação portátil — executável único

Publicação autocontida Windows x64, sem exigir runtime instalado:

```powershell
dotnet publish src\VCS.Desktop\VCS.Desktop.csproj -p:PublishProfile=WindowsX64 -o artifacts\portable
.\artifacts\portable\VCS.Desktop.exe
```

O perfil gera um `.exe` único com runtime .NET, bibliotecas gerenciadas e
DLLs nativas WPF/SQLite incluídos. Na execução, o runtime extrai automaticamente
as bibliotecas nativas para o cache da conta do usuário; isso não instala o
aplicativo no sistema. O primeiro início pode demorar um pouco mais.
Não há trimming, pois WPF, XAML e EF usam recursos e reflexão.

As fichas permanecem em `%LOCALAPPDATA%\VCS\Provisorio`, separadas do executável.
Mover ou substituir o `.exe` não apaga as fichas. Para levá-las a outro
computador, use backup/restauração ou exportação/importação pela interface.
Para remover o aplicativo, basta apagar o executável; os dados não são apagados.
Não há criação de atalhos, associação de arquivos ou instalação em Program Files.
A versão inicial não possui assinatura digital e pode gerar aviso do Windows.

## Estrutura e decisões arquiteturais

```text
VCS.slnx
Directory.Build.props
global.json
dotnet-tools.json
src\
  VCS.Domain\
    Characters\                Agregado, Value Objects, categorias e regras
  VCS.Application\
    Abstractions\              Repositório, manutenção e arquivos
    Characters\                DTOs e casos de uso
    Disciplines\               Contrato e definições do catálogo pessoal
  VCS.Infrastructure\
    Persistence\
      Migrations\              Migrações incrementais e snapshot EF
    Repositories\              Repositório SQLite
    Files\                     Backup, restauração, JSON e PDF preenchível
    Data\                      Material fornecido, incorporado ao EXE
  VCS.Desktop\
    Views\                     XAML e ciclo visual da janela
    ViewModels\                Comandos, navegação e estado de edição
    Controls\                  Pontos, números, dano, clãs e listas editáveis
    Services\                  Diálogos, temas, preferências e autosave
    Themes\                    Default\ e Clan\ com as paletas; estilos compartilhados
    Properties\PublishProfiles\
tests\
  VCS.Tests\
artifacts\                     Saídas locais ignoradas pelo Git
```

- [VCS.Domain](src/VCS.Domain): sem dependências de WPF, EF ou SQLite.
  `Character` é a raiz do agregado; apenas estados válidos são aceitos.
- [VCS.Application](src/VCS.Application): depende apenas do domínio; expõe
  casos de uso e contratos, sem conhecer janelas ou o provedor de banco.
- [VCS.Infrastructure](src/VCS.Infrastructure): implementa os contratos,
  usa contextos curtos, consultas sem tracking e transações explícitas.
  Um gate compartilhado coordena acesso, backups e restaurações.
- [VCS.Desktop](src/VCS.Desktop): composition root com DI e Serilog;
  MVVM com CommunityToolkit; `FluentWindow`, botões e temas WPF UI.
  Code-behind fica restrito à inicialização, interação dos controles e
  ciclo de fechamento da janela; regras e persistência ficam fora das Views.
- [VCS.Tests](tests/VCS.Tests): xUnit com testes unitários, de integração e
  smoke test visual. Referencia Desktop para exercitar os fluxos MVVM reais.

Navegação: **lista → criar/abrir/duplicar → editor → salvar/voltar → lista**.
Importação e exportação funcionam na lista ou no editor.
Organização visual fica em Exibir; dumps em Ferramentas. Listas começam vazias
e possuem estado visual próprio; painéis usam grids proporcionais, wrap e
rolagem para respeitar os limites de layout do WPF.

## Próximos passos

1. Homologar o executável portátil em VM limpa; assinatura digital.
2. Refinamentos de acessibilidade, localização e navegação por teclado.
3. Catálogos pessoais persistentes e reutilizáveis entre fichas.
4. Expansão das regras de criação, experiência e validações entre campos,
   sempre com testes e sem incorporar textos protegidos.
5. Impressão direta, anexos e histórico recuperável de alterações.
6. Política de retenção de backups e migrações de formatos JSON futuros.
