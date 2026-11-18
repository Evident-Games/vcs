# Organização dos temas

Os temas ficam em `src/Desktop/Themes/Default`, `Clan` e `Special` e são registrados em `ThemeCatalog.cs`. O ID de cada tema é persistido nas preferências e não depende do nome do arquivo; preserve IDs e caminhos existentes ao editar cores.

Cada arquivo tem uma paleta base `ThemeSheet*`, usada dentro da ficha e como base para a aplicação. `ThemeService.Load` copia essa base para os recursos `Theme*`. Um recurso `ThemeApp*` substitui somente a cor correspondente da aplicação; por exemplo, `ThemeAppCard` muda o cartão da aplicação sem mudar `ThemeSheetCard`. Sem override, a aplicação acompanha a cor da ficha. `ThemeAppHeaderText` e `ThemeAppFooterText` usam `ThemeOuterText` quando não são definidos.

Para mudar uma cor em ambas as áreas, edite a chave `ThemeSheet*` e verifique se existe override `ThemeApp*` correspondente. Para mudar somente a aplicação, adicione ou edite a chave `ThemeApp*`. Para mudar somente a ficha sem alterar a aplicação, primeiro declare no bloco `ThemeApp*` a cor atual da aplicação e depois altere a chave `ThemeSheet*`. O tema individual de cada ficha continua independente do tema global; seus recursos são limitados à própria ficha por `SheetTheme`.

| Papel | Recursos base |
| --- | --- |
| Superfícies e textos | `Background`, `OuterText`, `Card`, `CardText`, `Border` |
| Campos | `Input`, `InputText`, `InputTitle`, `InputDescription` |
| Destaques | `Accent`, `AccentText`, `AccentBorder`, `AccentHotbar` |
| Interação | `Hover`, `Selection`, `SelectionText`, `Scroll` |
| Dados | `DieNumber` para a cor dos números |

O HUD usa valores derivados da paleta da aplicação, com overrides opcionais `ThemeAppHud*`; veja [HUD flutuante](floating-hud.md). As cores e a opacidade escolhidas pelo usuário são aplicadas pelo mecanismo existente; reorganizar uma paleta não deve excluir preferências ou dados da ficha. A seleção do Windows/WPF recebe recursos próprios derivados de `Selection` e `SelectionText`.

A comparação estática dos 22 arquivos do catálogo durante esta reorganização não encontrou diferenças nas cores efetivas antes e depois. Ela não substitui conferência visual no Windows para contraste, transparência e componentes de terceiros.

Os nomes de recursos atuais usam o prefixo `Theme` (por exemplo, `ThemeSheetCard`, `ThemeAppCard` e `ThemeCard`). O carregador também aceita os antigos prefixos `VcsSheet` e `VcsApp` em paletas existentes; quando ambos estiverem presentes, a versão `Theme` prevalece. IDs de temas, arquivos de preferências e dados das fichas não mudam.
