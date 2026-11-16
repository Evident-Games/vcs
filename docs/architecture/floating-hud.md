# HUD lateral flutuante

O shell Windows/WPF usa um dock à direita e um drawer de 340 unidades, a 20 unidades das bordas da área de trabalho. Não foi introduzida uma aplicação web ou outra biblioteca de interface.

O HUD é uma sobreposição sem coluna reservada. Seu fundo externo é nulo: o espaço não desenhado permanece disponível para a ficha. Ao minimizar, a largura do host cai a zero, a opacidade diminui e o drawer desliza; a superfície recolhida deixa de receber mouse e teclado. Animações respeitam a preferência do Windows de reduzir animações. Os dois botões de navegação reabrem o drawer na aba escolhida; “Exibir” continua podendo ocultar todo o HUD.

Cabeçalho e rodapé ficam fixos, com conteúdo rolável entre eles. A lista de PCs aparece em Coterie como “Personagens locais”; não se presume que todas as fichas sejam aliadas. NPCs ficam separados. Abrir, criar PC/NPC, duplicar e excluir reaproveitam os comandos existentes; exclusão mantém sua confirmação. A busca continua por nome, clã e crônica.

O nome e o avatar do jogador são preferências locais, configuráveis em Perfil local. Sem imagem, aparecem iniciais; sem nome, “Jogador local”. Imagens são carregadas com tamanho reduzido, liberadas do arquivo e mantidas em cache por caminho. Os indicadores verdes significam **ficha aberta no dispositivo**, e não presença online. O app ainda não possui serviço de presença nem nome de jogador por ficha; abaixo de cada personagem permanece seu clã real.

Crônicas são agrupadas pelos nomes já presentes nas fichas e nos eventos salvos. Criar uma crônica grava um registro inicial no armazenamento existente de eventos, sem introduzir migração ou alterar fichas. Abrir um card mostra as ferramentas do Narrador filtradas pela crônica e recolhe o HUD. O filtro não substitui o rascunho atual nem muda o identificador do evento editado; “Todas as crônicas” remove o filtro. Novos eventos herdam o filtro como crônica inicial.

`HudCollapsed`, `HudPlayerName` e `HudAvatarPath` são campos opcionais das preferências existentes. Preferências anteriores continuam carregando normalmente. `CharacterSidebar.xaml.cs` concentra apenas apresentação, animação e seletor de arquivo; navegação e listas ficam no ViewModel.

## Temas

O HUD usa recursos próprios derivados da paleta do aplicativo; o tema individual de uma ficha permanece independente. Arquivos existentes não precisam receber novas chaves. Para personalizar o HUD, um tema pode fornecer estes recursos no bloco do aplicativo:

| Chave opcional | Tipo | Uso |
| --- | --- | --- |
| `VcsAppHudSurface` | SolidColorBrush | Fundo do drawer |
| `VcsAppHudDock` | SolidColorBrush | Pílula translúcida |
| `VcsAppHudCard` | SolidColorBrush | Cards, campos e avatares |
| `VcsAppHudText` / `VcsAppHudMuted` | SolidColorBrush | Texto principal / secundário |
| `VcsAppHudBorder` | SolidColorBrush | Bordas sutis |
| `VcsAppHudAccent` | SolidColorBrush | Destaque e ícones selecionados |
| `VcsAppHudSection` | Brush | Gradiente dos accordions |
| `VcsAppHudOpen` / `VcsAppHudClosed` | SolidColorBrush | Ficha aberta / salva |

O tema escuro vermelho define o carmesim `#9E0B22`; os outros herdam o próprio destaque. Cinzel e Inter/Montserrat são preferidos quando instalados, com Georgia/Segoe UI como alternativas locais. A translucidez usa brushes com alfa e sombras nativas; não se aplica `BlurEffect` ao conteúdo nem se afirma implementar o `backdrop-filter` do navegador. A mesa de dados continua funcional e foi posicionada à esquerda para não ficar sob o HUD.

## Verificação

Revisão estática e leitura de XML realizadas. **Build, testes e execução visual em Windows não foram realizados**, conforme a instrução vigente. Aparência em DPI distintos, navegação por teclado, seleção de imagens e persistência durante execução ainda precisam de validação no Windows.
