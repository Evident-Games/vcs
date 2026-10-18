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

Abra `artifacts\portable-0.13.0\VCS.Desktop.exe` com duplo clique.
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
- Exibição por abas, agrupamentos livres e seções configuráveis em **Exibir**.
- Todos os campos de identidade; nove atributos básicos; listas de traços
  pessoais com nome e pontuação; seleção editável de clã.
- Trilhas de Fome e Humanidade; Potência de Sangue; capacidade e danos de
  Saúde e Força de Vontade.
- Salvamento manual e automático, indicação de alterações pendentes,
  validação por campo e resumo dos erros.
- Datas de criação e alteração em UTC; revisão para concorrência otimista.
- Dump manual, restauração protegida e importação/exportação JSON.
- Cinco temas; Branco e vermelho como padrão, com tema e layout persistidos entre execuções.
- Sem serviços remotos, telemetria própria ou conta de usuário.

Habilidades possuem 27 referências no SQLite, compartilhadas por personagens
e NPCs, e continuam aceitando entradas pessoais. Vantagens e
desvantagens possuem o catálogo SQLite descrito abaixo e aceitam entradas pessoais.
Disciplinas possuem o catálogo pessoal
descrito abaixo e continuam aceitando entradas personalizadas.
O seletor inclui os 14 clãs fornecidos pelo usuário e os nomes pessoais já
usados nas fichas.
Não há seed de personagens, nem conteúdo oficial completo.
Os valores iniciais de uma ficha são apenas um rascunho editável, não uma
distribuição válida de pontos de criação.

### Ficha completa e menus

O editor abre como uma ficha contínua, sem aba de atributos separada por padrão:

- Cabeçalho próprio VCS e campos de identidade alinhados em três colunas.
- Atributos físicos, sociais e mentais, com pontos editáveis.
- Saúde, Força de Vontade, Fome, Humanidade e Potência de Sangue.
- Descrição, informações do clã e suas três disciplinas.
- Habilidades, disciplinas/poderes/rituais, vantagens e desvantagens.
- Ambição, desejo, ressonância, princípios da crônica, pilares, convicções
  e perdição do clã.
- Idade real/aparente, nascimento, morte, aparência, marcas e história.
- Referências de Surto de Sangue, recuperação de dano, bônus de poderes,
  rerrolagem, penalidade de alimentação e severidade da perdição.
- Experiência total, gasta e disponível, além das anotações.

Idades, datas e referências de sangue são preenchidas livremente pelo usuário.
A experiência disponível é total menos gasta; valores negativos, gasto maior
que o total ou acima de 1.000.000 são rejeitados. Selecionar poderes ou alterar
disciplinas em fichas livres não gasta experiência automaticamente.
Com acompanhamento ativo, use as compras em **Criação e evolução / XP**.

### Criação e evolução / XP

PCs novos começam em criação; rascunhos incompletos podem ser salvos.
Expanda **Criação e evolução / XP** para escolher idade e perfil de habilidades,
conferir os pontos e concluir a criação. A conclusão registra uma base gratuita
e libera compras com histórico, XP gasto e saldo. Os campos de pontos ficam
bloqueados para edição direta; identidade, notas, danos e poderes continuam editáveis.

| Idade | XP inicial | Potência de Sangue | Vantagens | Desvantagens |
|---|---:|---:|---:|---:|
| Criança da noite | 0 | 0 | 7 | 2 |
| Neófito | 15 | 1 | 7 | 2 |
| Ancillae | 35 | 2 | 9 | 4 |

- Atributos: 1 com 4, 3 com 3, 4 com 2 e 1 com 1.
- Habilidades, conforme os perfis fornecidos pelo usuário; demais em zero:
  Especialista: 4,3,3,3,2,2,2,2,1; Equilibrado: 3,3,3,2,2,2,2,2,1;
  Pau para toda obra: 4,3,2,2,2,2,2,1,1. Não são a distribuição de atributos.
- Disciplinas iniciais: duas do clã, uma com 2 e outra com 1.
- Compras: atributo e disciplina do clã = próximo nível × 5;
  disciplina fora do clã = próximo nível × 7; habilidade = próximo nível × 3;
  Potência de Sangue = próximo nível × 10; vantagem = 3 XP por ponto.
  Desvantagens não geram XP.
- Especializações: 3 XP cada após consumir as gratuitas aplicáveis.
  Uma gratuita por ficha; outra de predador quando ele estiver informado.
  Cada habilidade com 4+ ganha uma local. Erudição/Acadêmicos, Ofícios/Artesanato,
  Performance e Ciência ganham outra local ao receber o primeiro ponto.
  Bônus são cumulativos, inclusive na criação; podem ser usados depois.
- Os tipos de predador e seus bônus de disciplina serão cadastrados depois:
  nenhum ponto de disciplina é concedido por suposição.
- Fichas antigas preservam valores e XP sem cobrança retroativa.
  **Registrar base atual** habilita somente compras futuras, mantendo gastos antigos.
  NPCs são livres por padrão; podem registrar a base ou optar por criação com regras.
  **Iniciar criação (zera XP)** é uma opção explícita para fichas livres.
- Informe o XP total acumulado (já incluindo o bônus de idade); o total gasto
  é calculado pelas compras. Histórico e base acompanham banco, JSON, dump e PDF.
  Trocar a idade durante a criação ajusta somente a diferença do bônus de XP
  e redefine a Potência de Sangue inicial, sem apagar os demais pontos.

#### Fontes de XP

O XP de campanha é uma parte identificada do XP total, não outro saldo somado a ele.
**Receber XP de campanha** soma o prêmio ao total e ao recebido de campanha.
O painel mostra saldo de campanha e saldo inicial/anterior separadamente.
Compras comuns consomem primeiro o saldo inicial/anterior e depois campanha;
o histórico registra quanto de cada compra veio da campanha.
Prêmios recebidos posteriormente não mudam a origem das compras antigas.

Fichas anteriores preservam total e gastos, com campanha inicialmente zero.
Quando houver saldo antigo comprovadamente de campanha, informe sua parcela
em **XP de campanha recebido (parte do total)**, sem somá-la novamente.
Gastos antigos não são recalculados. Em fichas com perfil de criação, o bônus de
Neófito/Ancillae não pode ser reclassificado como campanha.

### Loresheets / Fichas de Conhecimento

Em **Vantagens e desvantagens**, o catálogo de loresheets fica separado das vantagens
comuns, com filtro por coleção, busca e descrição do benefício selecionado.
Os **20 benefícios fornecidos pelo usuário** são persistidos no SQLite:
**Os Bahari, Theo Bell, Heresia Cainita e Carna**, cinco benefícios de 1 a 5 pontos
por coleção. As quatro coleções são **gerais**, conforme confirmado; a classificação
de linhagem/descendência está preparada para futuras entradas, sem inventar conteúdo.

- Cada benefício é comprado individualmente. Comprar o de 5 pontos não concede
  os anteriores nem exige comprá-los; custa 5 pontos ou 15 XP, não a soma dos níveis.
- Durante a criação: no máximo **um benefício geral e um de linhagem/descendência**,
  não um por coleção. Com apenas as quatro coleções atuais, cabe um benefício geral.
- Pagamento inicial: o custo sai dos 7 pontos de vantagens (9 para Ancillae),
  **ou** é pago a 3 XP por ponto usando o bônus inicial de idade. A caixa de seleção
  permite escolher a fonte; pontos pagos com XP não consomem o orçamento de vantagens.
- Na criação em andamento, remover um benefício devolve seus pontos/XP inicial.
  Rascunhos podem ser salvos; a conclusão confere o orçamento restante de vantagens.
- Depois da criação: benefícios novos usam **exclusivamente XP de campanha**,
  a 3 XP por ponto, sem limite de coleções. Saldo inicial de Neófito/Ancillae não serve
  para essas compras. Benefícios repetidos são rejeitados.
- Fichas livres, inclusive NPCs, precisam iniciar a criação ou registrar a base atual
  antes de adicionar loresheets. Não há cobrança retroativa.
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
- **Exibir**: usar abas, Organizar abas, mostrar/ocultar cada seção e escolher em **Temas**:
  Preto e branco, Branco e preto, Branco e vermelho, Preto e vermelho,
  Amarelo e vermelho.
- **Ferramentas**: Criar dump do banco e Restaurar dump, com confirmação.
- **Ajuda**: instruções de navegação e atalhos.

As ações de duplicar, excluir e exportar usam a ficha aberta; na lista,
usam a seleção. Trocar de ficha salva o rascunho ou pede confirmação para
descartá-lo caso esteja inválido. Duplicar, exportar e criar dump exigem
salvar a ficha aberta com sucesso.

Em **Exibir → Organizar abas**, cada seção tem visibilidade, título editável,
nome de aba, ordem da seção e ordem da aba (1–99).
Seções com o mesmo nome (ignorando espaços nas pontas e maiúsculas/minúsculas)
aparecem juntas, respeitando a ordem configurada para as seções. A ordem de
uma aba agrupada é o menor número definido por suas seções; empates mantêm
a ordem das seções. Por padrão, Identidade e Atributos
compartilham uma aba; as demais ficam separadas. Por exemplo, escreva
`Ficha principal` em Identidade, Atributos e Estado e sangue para reuni-las.
Um nome vazio usa o título da própria seção. Nomes admitem até 80 caracteres.
Desmarque **Exibir por abas** para voltar à ficha contínua, sem perder os grupos.
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
seleções é validado em pelo menos 4,5:1 nos cinco temas.
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
rolagem interna para os níveis. Os catálogos continuam com painéis de consulta
de altura limitada e rolagem própria, além da rolagem principal da ficha.

As trilhas de bolinhas não possuem mais botão “0”. Clique num ponto para
definir o nível; repetir no ponto correspondente ao nível atual reduz um ponto.
Assim, no nível 1, clicar novamente no ponto 1 zera habilidades, disciplinas
e demais trilhas que aceitam zero. Atributos individuais preservam o mínimo 1.
Essa interação também vale nas fichas de NPC. O valor numérico ao lado é apenas
um indicador, não um botão.

### Habilidades compartilhadas

As 27 habilidades fornecidas pelo usuário são inseridas na tabela `SkillCatalog`:
nove Físicas, nove Sociais e nove Mentais, com nomes, descrições e IDs estáveis.
Ambos os editores exibem três colunas com pontos de 0 a 5; as descrições estão
na dica ao passar o mouse sobre o nome da habilidade. Habilidades pessoais continuam na
edição livre. Ao abrir uma ficha, entradas ausentes são apresentadas com zero;
elas são persistidas no próximo salvamento, sem marcar a abertura como alteração.
Nomes antigos equivalentes, ignorando acentos e caixa, conservam chave e pontuação.
Ao lado dos pontos de cada habilidade há **especialização em texto livre**,
até 500 caracteres. Separe especializações por ponto e vírgula, vírgula ou nova linha;
barras permanecem parte do texto. Fichas livres não calculam custos. Com criação/evolução
ativa, as especializações respeitam os créditos gratuitos e as compras de 3 XP.
Não há bônus automático de dados. O texto acompanha autosave, duplicação, JSON, dump e PDF,
tanto em personagens quanto NPCs; habilidades pessoais também o aceitam.

### Fichas de NPC

Use **Arquivo → Novo NPC** ou **+ → NPC** na lateral. O tipo de ficha fica
salvo no próprio agregado, não em outro banco, e acompanha duplicação, JSON e dump.
Fichas existentes continuam como personagens; não há conversão automática.

- Nome, clã e geração.
- Físicos, Sociais e Mentais em pontos de 0 a 5, sem presumir fórmulas.
- Vitalidade com total editável de 1 a 20: caixas vazias, cheias e X.
  Clique para alternar o estado; marcas são agrupadas por estado, não por posição.
- Força de Vontade com total de 1 a 20 e apenas caixas vazias ou X.
- Fome de 0 a 5 e Humanidade de 0 a 10 em caixas; clicar na última marcada reduz o valor.
- As mesmas 27 habilidades do personagem, sem catálogo exclusivo para NPC.
- Seis espaços iniciais de disciplinas, seletor, pontos e poderes próprios de cada
  disciplina. Poderes conhecidos são marcados livremente; rituais têm área separada.
- Seis espaços combinados de vantagens/defeitos, com seleção e pontos.
- Botões para adicionar mais espaços e notas livres.
- Potência de Sangue e acompanhamento opcional de criação/XP.
- As mesmas preferências de seções, títulos, ordem, agrupamentos e abas dos PCs,
  mantendo os campos práticos do NPC; o PDF respeita as seções visíveis.

Todos os espaços preenchidos são preservados; espaços vazios adicionais são
apenas auxiliares de edição e voltam ao mínimo de seis ao reabrir.
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
- NPCs usam a ficha simplificada; todos os espaços preenchidos são incluídos,
  mesmo acima de seis. Há pelo menos seis espaços de disciplinas e vantagens/defeitos.
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

O catálogo usa lista à esquerda e painel de detalhes à direita, como o de poderes,
com filtro de Todos/Vantagens/Defeitos e indicação de busca sem resultados.
Na seção **Vantagens e desvantagens**, busque por nome, grupo, tipo ou descrição
(com ou sem acentos), selecione uma entrada, escolha os pontos e adicione à ficha.
Custos fixos são pré-preenchidos; faixas usam seus limites no seletor.
Conforme confirmado, **Inimigo** mantém `1+` como referência, mas permite
de 1 a 5 pontos no aplicativo.

As seleções usam os traços existentes e acompanham autosave, duplicação,
JSON e dump/restore. Entradas repetidas do mesmo catálogo não são adicionadas
duas vezes; edite os pontos na lista. Entradas pessoais continuam disponíveis.
O editor livre mantém a edição de pontos de 0 a 5 e avisa quando a pontuação
sai da referência do catálogo; descrições não alteram dados, atributos,
habilidades, fome ou dados de testes automaticamente.

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

A identidade exibe a descrição e as três disciplinas do clã. Nas trilhas e
nos detalhes de poderes, o aplicativo indica **Do clã / Fora do clã** e o
custo do **próximo nível da disciplina**, não o custo de comprar o poder
selecionado. Por exemplo, subir de 2 para 3 custa 15 XP ou 21 XP.
No nível 5 é informado o limite, sem calcular um sexto nível.
Rituais não recebem esses multiplicadores. Para clã vazio ou personalizado,
o custo fica explicitamente indefinido.

A mudança de clã recalcula as informações sem apagar disciplinas/poderes,
sem conceder pontos e sem gastar XP. Disciplinas fora do clã permanecem
selecionáveis. A distribuição inicial 2/1 é validada ao concluir a criação
com acompanhamento ativo; fichas livres não impõem essa distribuição.

### Disciplinas, poderes e rituais

A seção **Disciplinas** inclui as **84 entradas fornecidas pelo usuário**:
79 poderes de 11 disciplinas e 5 rituais em uma seção separada.
O arquivo fornecido está incorporado ao executável em
[Disciplines.txt](src/VCS.Infrastructure/Data/Disciplines.txt).
Nomes, níveis, custos, paradas de dados e resumos foram mantidos como enviados,
sem certificação de correção ou de completude das regras oficiais.
Entradas compostas, como dois nomes separados por `/`, continuam sendo uma
única entrada, conforme o material recebido.

- Pesquise sem diferenciar acentos ou maiúsculas por nome, disciplina, custo,
  parada de dados ou resumo.
- Filtre por disciplina e/ou marque **Somente conhecidos**.
- Clique em uma entrada para ver os detalhes; marque a caixa para vinculá-la
  à ficha. A seleção participa do autosave e do salvamento manual.
- Todas as 11 disciplinas aparecem em trilhas de **0 a 5 bolinhas**, em duas
  colunas; 0 significa não adquirida. Níveis existentes são preservados; as
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
- Os filtros nunca apagam seleções. Poderes permanecem ao reabrir, duplicar,
  importar/exportar ou restaurar um backup.

As seleções são traços da categoria `DisciplinePower`, com chave estável
derivada do nome e da disciplina. Isso reutiliza a tabela `CharacterTraits`
sem recriar o banco nem exigir uma nova migração. Uma seleção importada de
outro catálogo é preservada e aparece como **Fora do catálogo**, com aviso de
que os detalhes não estão disponíveis, em vez de ser descartada silenciosamente.
O catálogo não altera automaticamente custos, rolagens, capacidades ou dano.
Rituais não possuem trilha de disciplina nem entram na relação das 11
disciplinas: são seleções da aba **Rituais**, associadas a Feitiçaria de Sangue.
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
- Atributos de 1 a 5; demais traços de 0 a 5; até 200 traços por ficha.
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
| `__EFMigrationsHistory` | Histórico de migrações do Entity Framework Core |

`CharacterDetails` é um Value Object imutável mapeado na própria tabela do
personagem. A relação com traços é 1:N com exclusão em cascata. Há índice no
nome e índice único em personagem + categoria + chave do traço. Datas são
armazenadas como UTC; a revisão é controlada pela aplicação, não por
`rowversion`, que não é suportado nativamente pelo SQLite.

### Migrações

As migrações `InitialCreate`, `CompleteCharacterSheet`, `AdvantageCatalog`, `SkillsAndNpcSheets`,
`SkillSpecializations`, `CharacterAdvancement` e `LoresheetsAndCampaignXp` já estão em
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

Arquivos usam `System.Text.Json`, envelope com `FormatVersion: 7` e objeto
`Character`. A importação também aceita os formatos 1, 2, 3, 4, 5 e 6 anteriores,
usando os valores padrão quando faltam os campos novos.
O formato 2 introduziu `DisciplinePower`; o formato 3 inclui os campos
adicionais de identidade, história, sangue e experiência. O formato 4 acrescenta
tipo de ficha, atributos agrupados e capacidades do NPC. O formato 5 acrescenta
`Specialization` aos traços, com texto vazio ao importar formatos anteriores.
O formato 6 acrescenta `Advancement`, com idade, perfil, base gratuita e histórico
de compras; sua ausência mantém o editor livre. O formato 7 adiciona loresheets e
fontes de XP; campos de campanha ausentes começam em zero, sem inferir sua origem.
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
    Themes\                    Paletas próprias clara e escura
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
