# Organização dos temas

Os temas ficam em `src/Desktop/Themes/Default`, `Clan` e `Special` e são registrados em `ThemeCatalog.cs`. O ID de cada tema é persistido nas preferências e não depende do nome do arquivo; preserve IDs e caminhos existentes ao editar cores.

Cada arquivo tem uma paleta base `VcsSheet*`, usada dentro da ficha e como base para a aplicação. `ThemeService.Load` copia essa base para os recursos `Vcs*`. Um recurso `VcsApp*` substitui somente a cor correspondente da aplicação; por exemplo, `VcsAppCard` muda o cartão da aplicação sem mudar `VcsSheetCard`. Sem override, a aplicação acompanha a cor da ficha. `VcsAppHeaderText` e `VcsAppFooterText` usam `VcsOuterText` quando não são definidos.

Para mudar uma cor em ambas as áreas, edite a chave `VcsSheet*` e verifique se existe override `VcsApp*` correspondente. Para mudar somente a aplicação, adicione ou edite a chave `VcsApp*`. Para mudar somente a ficha sem alterar a aplicação, primeiro declare no bloco `VcsApp*` a cor atual da aplicação e depois altere a chave `VcsSheet*`. O tema individual de cada ficha continua independente do tema global; seus recursos são limitados à própria ficha por `SheetTheme`.

| Papel | Recursos base |
| --- | --- |
| Superfícies e textos | `Background`, `OuterText`, `Card`, `CardText`, `Border` |
| Campos | `Input`, `InputText`, `InputTitle`, `InputDescription` |
| Destaques | `Accent`, `AccentText`, `AccentBorder`, `AccentHotbar` |
| Interação | `Hover`, `Selection`, `SelectionText`, `Scroll` |
| Dados | `DieNumber` para a cor dos números |

O HUD usa valores derivados da paleta da aplicação, com overrides opcionais `VcsAppHud*`; veja [HUD flutuante](floating-hud.md). As cores e a opacidade escolhidas pelo usuário são aplicadas pelo mecanismo existente; reorganizar uma paleta não deve excluir preferências ou dados da ficha. A seleção do Windows/WPF recebe recursos próprios derivados de `Selection` e `SelectionText`.

A comparação estática dos 22 arquivos do catálogo durante esta reorganização não encontrou diferenças nas cores efetivas antes e depois. Ela não substitui conferência visual no Windows para contraste, transparência e componentes de terceiros.
