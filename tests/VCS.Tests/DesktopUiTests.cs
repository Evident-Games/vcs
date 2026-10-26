using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using VCS.Application.Characters;
using VCS.Desktop.Catalogs;
using VCS.Desktop.Controls;
using VCS.Desktop.Layout;
using VCS.Desktop.Sheet;
using VCS.Desktop.Themes;
using VCS.Sheet;

namespace VCS.Tests;

[Collection(WpfCollection.Name)]
public sealed class DesktopUiTests(WpfApp wpf)
{
    private readonly WpfApp _wpf = wpf;

    /// <summary>Main window chrome, menus and the full PC sheet editing flow.</summary>
    [Fact]
    public Task Window_menus_and_pc_sheet_editing_work() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            var searchField = Find<TextBox>(window, "Busca lateral de personagens");
            var addSheet = Find<Button>(window, "Adicionar ficha");
            Assert.Equal(18, searchField.ActualHeight);
            Assert.Equal(18, addSheet.ActualHeight);
            var searchBar = Assert.IsType<Border>(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(searchField)));
            Assert.Equal(22, searchBar.ActualHeight);
            Assert.Equal(new CornerRadius(10), searchBar.CornerRadius);
            Assert.Equal(Color.FromRgb(255, 249, 247), ((SolidColorBrush)window.Background).Color);
            var menu = Descendants<Menu>(window).Single();
            Assert.Equal(new[] { "_Arquivo", "_Editar", "E_xibir", "_Ferramentas", "A_juda" },
                menu.Items.Cast<MenuItem>().Select(x => x.Header));
            var file = (MenuItem)menu.Items[0];
            Assert.Equal(new[] { "_Novo personagem", "_Abrir ficha", "_Salvar", "Lista de personagens", "_Importar JSON...", "_Exportar JSON..." },
                file.Items.OfType<MenuItem>().Take(6).Select(x => x.Header));
            var newItem = file.Items.OfType<MenuItem>().First();
            Assert.Same(model.CreateCommand, newItem.Command);
            await model.CreateCommand.ExecuteAsync(null);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.False(model.Display.UseTabs);
            Assert.Equal(8, model.Editor!.VisibleSections.Count);
            Assert.Equal(8, Descendants<SheetSectionView>(window).Count());
            var sidebarList = Find<ListBox>(window, "Lista lateral de personagens");
            var sidebarRow = Assert.IsType<ListBoxItem>(sidebarList.ItemContainerGenerator.ContainerFromIndex(0));
            var rowButton = Descendants<Button>(sidebarRow).Single(x => x.ContextMenu is not null);
            Assert.Same(model, rowButton.Tag);
            var characterMenu = rowButton.ContextMenu!;
            characterMenu.PlacementTarget = rowButton;
            characterMenu.IsOpen = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(new[] { "Abrir ficha", null, "Duplicar personagem", "Excluir personagem..." },
                characterMenu.Items.Cast<object>().Select(x => x is MenuItem item ? item.Header?.ToString() : null));
            Assert.Same(model, characterMenu.DataContext);
            var contextCharacter = Assert.IsType<CharacterSummary>(((MenuItem)characterMenu.Items[0]).CommandParameter);
            Assert.Equal(model.Editor.Id, contextCharacter.Id);
            characterMenu.IsOpen = false;
            var ageChoice = Find<ComboBox>(window, "Idade vampirica");
            var identity = Assert.Single(Descendants<IdentitySheetView>(window));
            var identityChoices = Descendants<ComboBox>(identity).ToArray();
            Assert.Same(ageChoice, identityChoices[0]);
            Assert.Equal("Cla", AutomationProperties.GetName(identityChoices[1]));
            Assert.Equal("Tipo de predador", AutomationProperties.GetName(identityChoices[2]));
            Assert.Equal("Nome do personagem", AutomationProperties.GetName(Find<TextBox>(identity, "Nome do personagem")));
            var identityName = Find<TextBox>(identity, "Nome do personagem");
            Assert.NotNull(Find<TextBox>(identity, "Nome mortal"));
            var concept = Find<TextBox>(identity, "Conceito");
            var conceptFontSize = concept.FontSize;
            var originalConcept = concept.Text;
            Assert.Equal(TextAlignment.Center, concept.TextAlignment);
            concept.Text = "Ex-detetive obcecado por vingança que caça os assassinos da própria família";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(concept.FontSize < conceptFontSize);
            Assert.True(concept.FontSize >= FitText.MinimumFontSize);
            concept.Text = string.Concat(Enumerable.Repeat("Uma ambição muito longa ", 12));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(FitText.MinimumFontSize, concept.FontSize);
            Assert.Equal(TextWrapping.Wrap, concept.TextWrapping);
            concept.Text = "Curto";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(conceptFontSize, concept.FontSize);
            Assert.Equal(TextWrapping.NoWrap, concept.TextWrapping);
            concept.Text = originalConcept;
            var ambition = Find<TextBox>(identity, "Ambicao");
            var desire = Find<TextBox>(identity, "Desejo");
            Assert.Equal(identityName.TranslatePoint(new Point(), identity).Y,
                concept.TranslatePoint(new Point(), identity).Y, 1);
            Assert.Equal(identityName.TranslatePoint(new Point(), identity).Y,
                ageChoice.TranslatePoint(new Point(), identity).Y, 1);
            var conceptHelp = Find<Button>(identity, "Ajuda sobre conceito");
            Assert.Contains("ideia central", Assert.IsType<string>(conceptHelp.ToolTip));
            conceptHelp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.True(identity.ActiveHelp!.IsOpen);
            Assert.False(identity.ActiveHelp.StaysOpen);
            Assert.Contains("ideia central", ActiveHelpText(identity));
            Assert.IsType<string>(conceptHelp.ToolTip);
            Assert.Contains("longo prazo", Assert.IsType<string>(Find<Button>(identity, "Ajuda sobre ambição").ToolTip));
            Assert.Contains("curto prazo", Assert.IsType<string>(Find<Button>(identity, "Ajuda sobre desejo").ToolTip));
            var ageHelp = Find<Button>(identity, "Ajuda sobre idade");
            Assert.Contains("Potência de Sangue", Assert.IsType<string>(ageHelp.ToolTip));
            ageHelp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Contains("Potência de Sangue", ActiveHelpText(identity));
            identity.CloseHelp();
            var generation = Find<NumericField>(identity, "Geracao");
            Assert.True(generation.ActualWidth >= 60);
            var increaseGeneration = Find<Button>(generation, "Aumentar valor");
            Assert.True(increaseGeneration.IsEnabled);
            Assert.Equal(16, increaseGeneration.Width);
            Assert.Equal(new CornerRadius(8),
                Assert.IsType<Border>(increaseGeneration.Template.FindName("StepCircle", increaseGeneration)).CornerRadius);
            var generationValue = generation.Value;
            Find<Button>(generation, "Aumentar valor").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(generationValue + 1, generation.Value);
            Find<Button>(generation, "Diminuir valor").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(generationValue, generation.Value);
            var sire = Find<TextBox>(identity, "Sire");
            Assert.Equal(0, Grid.GetColumn(sire));
            Assert.True(Grid.GetRow(sire) > Grid.GetRow(Find<TextBox>(identity, "Cronica")));
            double Top(FrameworkElement element) => element.TranslatePoint(new Point(), identity).Y;
            Assert.Equal(Top(Find<TextBox>(identity, "Nome mortal")), Top(ambition), 1);
            Assert.Equal(Top(ambition), Top(Descendants<ClanSelector>(identity).Single()), 1);
            Assert.Equal(Top(Find<TextBox>(identity, "Cronica")), Top(desire), 1);
            Assert.Equal(Top(desire), Top(Find<ComboBox>(identity, "Tipo de predador")), 1);
            Assert.Equal(Top(Find<Button>(identity, "Ajuda sobre conceito")),
                Top(Find<Button>(identity, "Ajuda sobre idade")), 1);
            var labelTops = Descendants<TextBlock>(identity)
                .Where(x => x.Text is "Nome" or "Conceito" or "Idade" or "Geração").Select(Top).ToArray();
            Assert.Equal(4, labelTops.Length);
            Assert.All(labelTops, top => Assert.Equal(labelTops[0], top, 1));
            Assert.Equal(Top(Find<Button>(identity, "Ajuda sobre desejo")),
                Top(Find<Button>(identity, "Informacoes do predador")), 1);
            var clanHelp = Find<Button>(identity, "Informacoes do cla");
            model.Editor.Clan = "Brujah";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            clanHelp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Contains("Ganha: Celeridade, Potência, Presença", ActiveHelpText(identity));
            Assert.Contains("Perdição: Sangue Fervoroso", ActiveHelpText(identity));
            Assert.True(Assert.IsType<System.Windows.Controls.ToolTip>(clanHelp.ToolTip).StaysOpen);
            identity.CloseHelp();
            Assert.Equal(new CornerRadius(9),
                Assert.IsType<Border>(clanHelp.Template.FindName("HelpCircle", clanHelp)).CornerRadius);
            Assert.Equal(18, clanHelp.Width);
            Assert.Equal(PlacementMode.Top, ToolTipService.GetPlacement(clanHelp));
            Assert.DoesNotContain(Descendants<TextBox>(identity),
                x => AutomationProperties.GetName(x) == "Descricao do personagem");
            var historyDescription = Find<TextBox>(window, "Descricao do personagem");
            Assert.Same(historyDescription, Assert.Single(Descendants<TextBox>(
                Assert.Single(Descendants<SheetSectionView>(window),
                    x => Assert.IsType<EditorSection>(x.DataContext).Key == "Background")),
                x => AutomationProperties.GetName(x) == "Descricao do personagem"));
            ageChoice.SelectedIndex = 1;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(1, model.Editor.AgeIndex);
            Assert.Equal(15, model.Editor.ExperienceTotal);
            Assert.True(await model.Editor.FlushAsync());
            Assert.Equal(VampireAge.Neonate,
                (await service.GetAsync(model.Editor.Id)).Details.Advancement!.Age);
            ageChoice.SelectedIndex = 0;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var advancementView = Assert.Single(Descendants<AdvancementView>(window));
            var backgroundSection = Assert.Single(Descendants<SheetSectionView>(window),
                x => Descendants<AdvancementView>(x).Contains(advancementView));
            Assert.Equal("Background", Assert.IsType<EditorSection>(backgroundSection.DataContext).Key);
            Assert.DoesNotContain(Descendants<ComboBox>(advancementView),
                x => AutomationProperties.GetName(x) == "Idade vampirica");
            Assert.DoesNotContain(Descendants<ComboBox>(advancementView),
                x => AutomationProperties.GetName(x) == "Perfil de habilidades");
            Assert.DoesNotContain(Descendants<ComboBox>(advancementView),
                x => AutomationProperties.GetName(x) == "Tipo de predador");
            var initialSkillsView = Assert.Single(Descendants<SkillsView>(window));
            var skillProfile = Find<ComboBox>(initialSkillsView, "Perfil de habilidades");
            var skillDistribution = Find<TextBlock>(initialSkillsView, "Distribuicao inicial de habilidades");
            Assert.DoesNotContain(Descendants<Button>(initialSkillsView),
                x => (AutomationProperties.GetName(x) ?? "").Contains("página", StringComparison.OrdinalIgnoreCase));
            var skillColumns = Descendants<TextBlock>(initialSkillsView)
                .Where(x => x.Text is "Físicas" or "Sociais" or "Mentais" && x.FontFamily.Source == "Georgia")
                .Select(x => x.TranslatePoint(new Point(), initialSkillsView))
                .ToList();
            Assert.Equal(3, skillColumns.Count);
            Assert.All(skillColumns, x => Assert.Equal(skillColumns[0].Y, x.Y, 1));
            Assert.True(skillColumns[0].X < skillColumns[1].X && skillColumns[1].X < skillColumns[2].X);
            Assert.Equal("1 × ●●●● · 3 × ●●● · 3 × ●● · 3 × ● · demais em 0", skillDistribution.Text);
            skillProfile.SelectedIndex = 1;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal("3 × ●●● · 5 × ●● · 6 × ● · demais em 0", skillDistribution.Text);
            Assert.True(await model.Editor.FlushAsync());
            Assert.Equal(SkillProfile.Balanced,
                (await service.GetAsync(model.Editor.Id)).Details.Advancement!.Profile);
            skillProfile.SelectedIndex = 0;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var attributeDistribution = Find<TextBlock>(window, "Distribuicao inicial de atributos");
            Assert.StartsWith("Monte a base: 1 × ●●●●", attributeDistribution.Text);
            Assert.Contains("Faltam:", Find<TextBlock>(window, "Atributos que faltam").Text);
            var attributeSection = Assert.Single(Descendants<SheetSectionView>(window),
                x => Descendants<TextBlock>(x).Contains(attributeDistribution));
            Assert.Equal("Attributes", Assert.IsType<EditorSection>(attributeSection.DataContext).Key);
            foreach (var column in Descendants<ItemsControl>(attributeSection).Where(x => x.Parent is StackPanel { Parent: UniformGrid }))
            {
                var header = (FrameworkElement)((StackPanel)column.Parent).Children[0];
                var columnCenter = column.TranslatePoint(new Point(column.ActualWidth / 2, 0), attributeSection).X;
                var headerCenter = header.TranslatePoint(new Point(header.ActualWidth / 2, 0), attributeSection).X;
                Assert.Equal(headerCenter, columnCenter, tolerance: 1.5);
            }
            var originalAttributes = model.Editor.Attributes.Items.ToDictionary(x => x.Key, x => x.Rating);
            var attributeBase = new Dictionary<string, int>
            {
                ["Charisma"] = 4, ["Manipulation"] = 3, ["Composure"] = 3, ["Wits"] = 3,
                ["Strength"] = 2, ["Dexterity"] = 2, ["Resolve"] = 2, ["Intelligence"] = 2, ["Stamina"] = 1
            };
            foreach (var (key, rating) in attributeBase) model.Editor.Attributes.Items.Single(x => x.Key == key).Rating = rating;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var columnWidths = Descendants<ItemsControl>(attributeSection).Where(x => x.Parent is StackPanel { Parent: UniformGrid })
                .Select(x => x.ActualWidth).ToArray();
            foreach (var key in new[] { "Manipulation", "Wits" })
                Descendants<Button>(Find<DotTrack>(attributeSection, key)).Single(x => x.Tag is 4).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Contains("próximo +25 XP", model.Editor.Attributes.Items.Single(x => x.Key == "Wits").AdvancementInfo);
            Assert.Equal(columnWidths, Descendants<ItemsControl>(attributeSection).Where(x => x.Parent is StackPanel { Parent: UniformGrid })
                .Select(x => x.ActualWidth).ToArray());
            foreach (var column in Descendants<ItemsControl>(attributeSection).Where(x => x.Parent is StackPanel { Parent: UniformGrid }))
                Assert.True(column.ActualWidth <= ((FrameworkElement)column.Parent).ActualWidth);
            var staminaTrack = Find<DotTrack>(attributeSection, "Stamina");
            Assert.Equal(0, staminaTrack.Minimum);
            Descendants<Button>(staminaTrack).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(0, model.Editor.Attributes.Items.Single(x => x.Key == "Stamina").Rating);
            Assert.False(model.Editor.HasErrors, model.Editor.ValidationSummary);
            foreach (var (key, rating) in originalAttributes) model.Editor.Attributes.Items.Single(x => x.Key == key).Rating = rating;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.DoesNotContain(Descendants<Expander>(window),
                x => AutomationProperties.GetName(x) == "Criacao e evolucao");
            Assert.DoesNotContain(Descendants<Button>(window),
                x => Equals(x.Content, "Concluir criacao e registrar pontos gratuitos"));
            Assert.Empty(Descendants<PredatorView>(identity));
            Assert.Single(Descendants<PredatorView>(window));
            Assert.DoesNotContain(Descendants<Expander>(window),
                x => AutomationProperties.GetName(x) == "Configuracao de predador");
            Assert.Contains("12 a 16", Assert.IsType<string>(generation.ToolTip));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var predatorType = Find<ComboBox>(window, "Tipo de predador");
            var predatorHelp = Find<Button>(window, "Informacoes do predador");
            Assert.Equal(13, predatorType.Items.Count);
            await AssertComboInteractionAsync(predatorType);
            predatorType.SelectedItem = model.Editor.PredatorTypes.Single(x => x.Id == "alleycat");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            predatorHelp.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var predatorIdentity = Assert.Single(Descendants<IdentitySheetView>(window));
            Assert.Contains("Benefícios:", ActiveHelpText(predatorIdentity));
            Assert.Contains("Defeitos:", ActiveHelpText(predatorIdentity));
            Assert.True(Assert.IsType<System.Windows.Controls.ToolTip>(predatorHelp.ToolTip).StaysOpen);
            predatorIdentity.CloseHelp();
            Find<DotTrack>(window, "Pontos de Briga").SetCurrentValue(DotTrack.ValueProperty, 1);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Contains(Descendants<TextBlock>(predatorType), x => x.Text == "Vira-lata");
            // Choosing the predator applies it at once; discipline and specialization come from the discreet tags.
            Assert.True(model.Editor.HasAppliedPredator, model.Editor.Error);
            Assert.DoesNotContain(Descendants<ComboBox>(window), x => AutomationProperties.GetName(x) == "Especializacao do predador");
            Assert.DoesNotContain(Descendants<Button>(window), x => AutomationProperties.GetName(x) == "Aplicar predador");
            var brawlTag = Find<Button>(Assert.Single(Descendants<SkillsView>(window)), "Predador em Briga");
            Assert.Equal(Visibility.Visible, brawlTag.Visibility);
            Assert.Equal(FontWeights.Normal, brawlTag.FontWeight);
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(brawlTag)).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(FontWeights.Bold, brawlTag.FontWeight);
            var celerityTag = Find<Button>(Assert.Single(Descendants<DisciplineCatalogView>(window)), "Predador em Celeridade");
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(celerityTag)).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(FontWeights.Bold, celerityTag.FontWeight);
            Assert.True(model.Editor.HasAppliedPredator, model.Editor.Error);
            Assert.Equal(6, model.Editor.Humanity);
            var brawlPager = Descendants<SpecializationPager>(window).Single(x => x.EditorName == "Especializacao de Briga");
            var predatorLine = Find<TextBox>(brawlPager, "Especializacao de Briga");
            Assert.Equal("Agarramento", predatorLine.Text);
            Assert.True(predatorLine.IsReadOnly);
            Assert.Equal(Visibility.Visible, Find<Button>(brawlPager, "Adicionar especializacao de briga").Visibility);
            Assert.DoesNotContain(Descendants<TextBox>(window),
                x => AutomationProperties.GetName(x) == "Predador");
            Assert.Equal(1, Find<DotTrack>(window, "Disciplina Celeridade").Minimum);
            Find<NumericField>(window, "Maculas").SetCurrentValue(NumericField.ValueProperty, 2);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var stainDamage = Find<Button>(window, "Aplicar dano de Maculas");
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(stainDamage)).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(1, model.Editor.WillpowerAggravated);
            var remorse = Find<Button>(window, "Registrar falha no Remorso");
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(remorse)).Invoke();
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            Assert.Equal(0, model.Editor.Stains);
            Assert.Equal(5, model.Editor.Humanity);
            Assert.NotNull((await service.GetAsync(model.Editor.Id)).Details.PredatorSelection);
            var removePredator = Find<Button>(window, "Remover predador");
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(removePredator)).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.False(model.Editor.HasAppliedPredator);
            Assert.DoesNotContain(model.Editor.Advantages.Items, x => x.IsPredatorGrant);
            model.Editor.Humanity = 0;
            Assert.True(model.Editor.IsWassail);
            model.Editor.Notes = "Continua editavel em Wassail";
            Assert.False(model.Editor.HasErrors);
            model.Editor.Humanity = 7;
            model.Editor.WillpowerAggravated = 0;
            model.Editor.Skills.Items.Single(x => x.Name == "Briga").Rating = 0;
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            var meritsView = Assert.Single(Descendants<AdvantageCatalogView>(window));
            Assert.Empty(Descendants<NumericField>(meritsView));
            Assert.Empty(Descendants<ComboBox>(meritsView));
            Assert.Empty(Descendants<TabControl>(meritsView));
            Assert.DoesNotContain(Descendants<ComboBox>(window), x => AutomationProperties.GetName(x) == "Bonus de disciplina do predador");
            var disciplinesView = Assert.Single(Descendants<DisciplineCatalogView>(window));
            disciplinesView.BringIntoView();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            SaveSnapshot(window, "disciplinas");
            Assert.Contains(Descendants<TextBlock>(meritsView),
                x => x.Text.Contains("Base da idade:") && x.Text.Contains("XP"));
            Assert.Contains(Descendants<TextBlock>(meritsView),
                x => x.Text.Contains("Defeitos não concedem XP"));
            async Task<ItemsControl> AddMeritRow(string button, string list)
            {
                ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(Find<Button>(meritsView, button))).Invoke();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                return Find<ItemsControl>(meritsView, list);
            }
            SuggestionBox MeritBox(ItemsControl rows, string editor) =>
                Descendants<SuggestionBox>(rows).Last(x => x.EditorName == editor);
            var flawRows = await AddMeritRow("Adicionar defeito", "Defeitos da ficha");
            MeritBox(flawRows, "Nome da caracteristica").Text = "Feio";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Single(model.Editor.Flaws.Items);
            var advantageRows = await AddMeritRow("Adicionar vantagem", "Vantagens da ficha");
            var meritCategory = MeritBox(advantageRows, "Categoria da caracteristica");
            meritCategory.Pick(meritCategory.Suggestions!.Cast<SuggestionItem>().Single(x => x.Value == "Beleza"));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var meritName = MeritBox(advantageRows, "Nome da caracteristica");
            Assert.All(meritName.Suggestions!.Cast<SuggestionItem>(), x => Assert.Contains(x.Value, new[] { "Visual Belo", "Deslumbrante" }));
            meritName.Pick(meritName.Suggestions!.Cast<SuggestionItem>().Single(x => x.Value == "Deslumbrante"));
            await WaitUntilAsync(() => model.Editor.Advantages.Items.Count == 1 && !model.Editor.IsDirty);
            Assert.Empty(model.Editor.Flaws.Items);
            Assert.Empty(model.Editor.AdvantageCatalog!.FlawRows);
            Assert.Equal(4, Find<DotTrack>(advantageRows, "Pontos da caracteristica").Value);
            Assert.Equal("Deslumbrante", model.Editor.Advantages.Items[0].Name);
            Assert.Equal(4, model.Editor.Advantages.Items[0].Rating);
            advantageRows = await AddMeritRow("Adicionar vantagem", "Vantagens da ficha");
            MeritBox(advantageRows, "Categoria da caracteristica").Text = "Refúgio";
            MeritBox(advantageRows, "Nome da caracteristica").Text = "Biblioteca pessoal";
            Descendants<Button>(advantageRows).Last(x => AutomationProperties.GetName(x) == "Extras da caracteristica")
                .Command.Execute(null);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Descendants<TextBox>(advantageRows).Last(x => AutomationProperties.GetName(x) == "Anotacoes da caracteristica").Text = "Livros da cronica.";
            Descendants<DotTrack>(advantageRows).Last().SetCurrentValue(DotTrack.ValueProperty, 2);
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            meritsView.BringIntoView();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            SaveSnapshot(window, "vantagens");
            Assert.Contains((await service.GetAsync(model.Editor.Id)).Traits,
                x => x.Name == "Biblioteca pessoal" && x.Rating == 2 && x.Merit == new MeritInfo("Refúgio", "Livros da cronica."));
            var removePersonal = Descendants<Button>(advantageRows).Last(x => AutomationProperties.GetName(x) == "Remover caracteristica");
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(removePersonal)).Invoke();
            await WaitUntilAsync(() => model.Editor.Advantages.Items.Count == 1 && !model.Editor.IsDirty);
            Assert.Single(model.Editor.AdvantageCatalog!.AdvantageRows);
            // Loresheets are picked in the advantage rows, under the "Ficha de conhecimento" category.
            async Task<TraitItemViewModel?> PickLoresheet(string collection, int points)
            {
                var benefit = model.Editor.LoresheetBenefits.Single(x => x.LoresheetCollection == collection && x.Minimum == points);
                var rows = await AddMeritRow("Adicionar vantagem", "Vantagens da ficha");
                var category = MeritBox(rows, "Categoria da caracteristica");
                var lore = category.Suggestions!.Cast<SuggestionItem>().Single(x => x.Value == AdvantageCatalogViewModel.LoresheetFamily);
                Assert.Contains("benefícios", lore.Detail);
                category.Pick(lore);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var names = MeritBox(rows, "Nome da caracteristica");
                Assert.All(names.Suggestions!.Cast<SuggestionItem>(), x => Assert.True(x.HasNote, x.Value));
                var choice = names.Suggestions!.Cast<SuggestionItem>().Single(x => x.Value == benefit.Name && x.Note == collection);
                names.Pick(choice);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.True(model.Editor.Error.Length == 0, model.Editor.Error);
                Assert.Contains(Descendants<TextBlock>(rows), x => AutomationProperties.GetName(x) == "Origem da caracteristica" &&
                    x.Text.StartsWith(collection) && x.IsVisible);
                return model.Editor.Loresheets.Items.SingleOrDefault(x => x.Key == benefit.Id);
            }
            Assert.NotNull(await PickLoresheet("Os Bahari", 2));
            Assert.Single(model.Editor.Loresheets.Items);
            Assert.Equal("Escarificação Ritual", model.Editor.Loresheets.Items[0].Name);
            Assert.Equal(0, model.Editor.ExperienceSpent);
            Assert.NotNull(await PickLoresheet("Theo Bell", 1));
            Assert.Equal(LoresheetPayment.InitialXp, model.Editor.Loresheets.Items[1].Loresheet!.Payment);
            Assert.Equal(2, model.Editor.Loresheets.Items.Count);
            Assert.Equal(3, model.Editor.AdvantageCatalog!.AdvantageRows.Count);
            var loreRows = Find<ItemsControl>(meritsView, "Vantagens da ficha");
            var removeLore = Descendants<Button>(loreRows).Last(x => AutomationProperties.GetName(x) == "Remover caracteristica");
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(removeLore)).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Single(model.Editor.Loresheets.Items);
            Assert.Equal(2, model.Editor.AdvantageCatalog!.AdvantageRows.Count);
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            var name = Find<TextBox>(window, "Nome do personagem");
            name.Text = "Smoke UI";
            historyDescription.Text = "Descrição longa do personagem.";
            var clan = Find<ComboBox>(window, "Cla");
            await AssertComboInteractionAsync(clan);
            Assert.Contains("Brujah", clan.Items.Cast<string>());
            clan.Text = "Brujah";
            Find<TextBox>(window, "Ambicao").Text = "Ambicao de teste";
            Find<TextBox>(window, "Pilares").Text = "Pilar pessoal";
            Find<TextBox>(window, "Historia").Text = "Historia do personagem";
            var vigor = Find<DotTrack>(window, "Stamina");
            vigor.SetCurrentValue(DotTrack.ValueProperty, 3);
            Find<DotTrack>(window, "Fome na ficha").SetCurrentValue(DotTrack.ValueProperty, 2);
            Find<DotTrack>(window, "Potencia de sangue na ficha").SetCurrentValue(DotTrack.ValueProperty, 4);
            var obfuscate = Find<DotTrack>(window, "Disciplina Ofuscação");
            obfuscate.SetCurrentValue(DotTrack.ValueProperty, 2);
            var health = Find<DamageTrack>(window, "Saude na ficha");
            Assert.Equal(6, health.Capacity);
            health.SetCurrentValue(DamageTrack.SuperficialProperty, 1);
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            var saved = await service.GetAsync(model.Editor.Id);
            Assert.Equal("Ambicao de teste", saved.Details.Ambition);
            Assert.Equal("Pilar pessoal", saved.Details.Touchstones);
            Assert.Equal("Historia do personagem", saved.Details.History);
            Assert.Equal("Descrição longa do personagem.", saved.Details.Description);
            Assert.Equal(2, saved.Details.Hunger);
            Assert.Equal(4, saved.Details.BloodPotency);
            Assert.Contains(saved.Traits, x => x.Category == TraitCategory.Discipline && x.Name == "Ofuscação" && x.Rating == 2);
            Assert.Equal(1, saved.Details.HealthSuperficial);
            Assert.Equal(3, saved.Traits.Single(x => x.Key == "Stamina").Rating);
            name.Text = "";
            Assert.True(model.Editor.HasErrors);
            Assert.True(Validation.GetHasError(name));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal([name], Descendants<FrameworkElement>(window).Where(Validation.GetHasError).ToArray());
            name.Text = "Smoke UI";
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            var skillTrack = Find<DotTrack>(window, "Pontos de Armas Brancas");
            var firstDot = Descendants<Button>(skillTrack).Single(x => x.Tag is 1);
            firstDot.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, skillTrack.Value);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Descendants<Button>(skillTrack).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(0, skillTrack.Value);
            Assert.DoesNotContain(Descendants<Button>(skillTrack), x => Equals(x.Content, "0"));
            var weaponPager = Descendants<SpecializationPager>(window).Single(x => x.EditorName == "Especializacao de Armas Brancas");
            var weaponAdd = Find<Button>(weaponPager, "Adicionar especializacao de armas brancas");
            Assert.Equal(Visibility.Collapsed, weaponAdd.Visibility);
            Find<TextBox>(weaponPager, "Especializacao de Armas Brancas").Text = "Esgrima / estacas";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(Visibility.Visible, weaponAdd.Visibility);
            var weaponRowTop = weaponPager.TranslatePoint(new Point(), skillTrack).Y;
            weaponAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Find<TextBox>(weaponPager, "Especializacao de Armas Brancas 2").Text = "Duelo";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(2, Descendants<TextBox>(weaponPager).Count());
            Assert.Equal(weaponRowTop, weaponPager.TranslatePoint(new Point(), skillTrack).Y, 1);
            Assert.Equal("2 esp.", Find<TextBlock>(weaponPager, "Quantidade de especializacoes de Armas Brancas").Text);
            Assert.Equal(2, model.Editor.Skills.Items.Single(x => x.Name == "Armas Brancas").SpecializationCount);
            Assert.Equal(Visibility.Collapsed, Find<Button>(weaponPager, "Proximas especializacoes").Visibility);
            weaponAdd.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(1, weaponPager.Page);
            Assert.Single(Descendants<TextBox>(weaponPager));
            Find<TextBox>(weaponPager, "Especializacao de Armas Brancas 3").Text = "Facas";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(Visibility.Visible, Find<Button>(weaponPager, "Especializacoes anteriores").Visibility);
            Find<Button>(weaponPager, "Especializacoes anteriores").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(0, weaponPager.Page);
            Assert.Equal("Esgrima / estacas", Find<TextBox>(weaponPager, "Especializacao de Armas Brancas").Text);
            Find<Button>(weaponPager, "Proximas especializacoes").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Find<TextBox>(weaponPager, "Especializacao de Armas Brancas 3").Text = "";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(2, model.Editor.Skills.Items.Single(x => x.Name == "Armas Brancas").SpecializationCount);
            var attributeTrack = Find<DotTrack>(window, "Strength");
            attributeTrack.SetCurrentValue(DotTrack.ValueProperty, 1);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Descendants<Button>(attributeTrack).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(0, attributeTrack.Value);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Descendants<Button>(attributeTrack).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, attributeTrack.Value);
            obfuscate.SetCurrentValue(DotTrack.ValueProperty, 1);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Descendants<Button>(obfuscate).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(0, obfuscate.Value);
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            Assert.Contains((await service.GetAsync(model.Editor.Id)).Traits,
                x => x.Specialization == "Esgrima / estacas\nDuelo");

            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(Find<Button>(window, "Adicionar poder"))).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var powerRow = Assert.Single(model.Editor.Powers.PowerRows);
            var powerName = Descendants<SuggestionBox>(window).Single(x => x.EditorName == "Nome do poder");
            var suggestion = Assert.IsType<SuggestionItem>(powerName.Suggestions!.Cast<object>().First());
            powerName.Pick(suggestion);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(suggestion.Value, powerRow.Name);
            Assert.True(powerRow.IsCatalog);
            Assert.False(string.IsNullOrWhiteSpace(powerRow.Discipline));
            Assert.False(string.IsNullOrWhiteSpace(Find<TextBox>(window, "Custo do poder").Text));
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            Assert.True(await model.Editor.FlushAsync());
            Assert.Contains((await service.GetAsync(model.Editor.Id)).Traits, x => x.Key == powerRow.Key && x.Name == suggestion.Value);
            ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(Find<Button>(window, "Adicionar ritual"))).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var ritualRow = Assert.Single(model.Editor.Powers.RitualRows);
            Descendants<SuggestionBox>(window).Single(x => x.EditorName == "Nome do ritual").Text = "Ritual da casa";
            Find<TextBox>(window, "Ingredientes do ritual").Text = "Cinzas e sangue";
            await WaitUntilAsync(() => !model.Editor.IsDirty);
            Assert.True(await model.Editor.FlushAsync());
            var savedRitual = (await service.GetAsync(model.Editor.Id)).Traits.Single(x => x.Key == ritualRow.Key);
            Assert.Equal("Ritual da casa", savedRitual.Name);
            Assert.True(savedRitual.Power!.IsRitual);
            Assert.Equal("Cinzas e sangue", savedRitual.Power.Ingredients);

    });

    /// <summary>Layout editor: list, tabs and mini bento change the open sheet and persist.</summary>
    [Fact]
    public Task Layout_editor_rearranges_sheets_and_persists() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            await ui.CreatePcAsync(x => x.PhysicalAttributes.Single(a => a.Key == "Stamina").Rating = 3);
            var menu = Descendants<Menu>(window).Single();
            var settingsMenu = (MenuItem)menu.Items[2];
            Assert.Equal(["Layout da ficha...", "Lista lateral de personagens", "Temas"],
                settingsMenu.Items.OfType<MenuItem>().Select(x => (string)x.Header));
            ((MenuItem)settingsMenu.Items[0]).Command.Execute(null);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(model.IsLayoutSettingsOpen);
            Assert.False(model.Display.IsEditingNpc);
            var layoutEditor = Find<Border>(window, "Layout da ficha");
            AssertReadableControls(layoutEditor);
            var modes = Descendants<RadioButton>(layoutEditor).Where(x => x.GroupName == "SheetMode").ToArray();
            Assert.Equal(["Lista", "Abas", "Bento"], modes.Select(x => (string)x.Content));
            modes[1].IsChecked = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(model.Display.UseTabs);
            var tabs = Find<TabControl>(window, "Secoes da ficha");
            Assert.Equal(7, tabs.Items.Count);
            foreach (var section in model.Editor!.VisibleTabs.ToArray())
            {
                tabs.SelectedItem = section;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.Equal(section.Sections.Count, Descendants<SheetSectionView>(window).Count());
                Assert.Equal(section.Sections.Any(x => x.Key == "Background") ? 1 : 0,
                    Descendants<AdvancementView>(window).Count());
            }
            // Tab editor: one column per tab; a section dropped on a column moves into that tab.
            Assert.Equal(7, Find<ItemsControl>(window, "Abas do layout").Items.Count);
            Assert.True(Find<Border>(window, "Nova aba").AllowDrop);
            var pcLayout = model.Display.Pc;
            pcLayout.RenameTab("Identidade", "Ficha principal");
            pcLayout.MoveToTab(pcLayout.Find("Attributes")!, "Ficha principal");
            pcLayout.MoveToTab(pcLayout.Find("State")!, "Ficha principal");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(5, Find<ItemsControl>(window, "Abas do layout").Items.Count);
            modes[0].IsChecked = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(model.Display.IsContinuous);
            Find<TextBox>(window, "Titulo de Estado e sangue").Text = "Estado de sangue";
            Assert.Empty(Descendants<NumericField>(Find<ItemsControl>(window, "Ordem das secoes")));
            var stateHandle = Find<Button>(window, "Mover Estado e sangue");
            Assert.True(Find<Border>(window, "Linha de Estado e sangue").AllowDrop);
            Assert.Contains("Arraste", Assert.IsType<string>(stateHandle.ToolTip));
            model.Display.Pc.MoveSection(model.Display.Pc.Sections.Single(x => x.Key == "State"), 0);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var firstRow = Assert.IsAssignableFrom<DependencyObject>(Find<ItemsControl>(window, "Ordem das secoes")
                .ItemContainerGenerator.ContainerFromIndex(0));
            Assert.Same(Find<Border>(window, "Linha de Estado e sangue"), Descendants<Border>(firstRow).First());
            Assert.Equal(5, model.Editor.VisibleTabs.Count);
            Assert.Equal(3, model.Editor.VisibleTabs[0].Sections.Count);
            Assert.False(model.Editor.IsDirty);
            // Unchecking a section in the list editor hides it from the sheet.
            Descendants<CheckBox>(Find<ItemsControl>(window, "Ordem das secoes")).Single(x => Equals(x.Content, "Atributos")).IsChecked = false;
            // Mini bento: width and height buttons change the live layout.
            modes[2].IsChecked = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            SaveSnapshot(window, "layout-editor-bento");
            var skillsOption = pcLayout.Find("Skills")!;
            var spanBefore = skillsOption.Span;
            Find<Button>(window, "Largura de Habilidades").Command.Execute(null);
            Assert.Equal(spanBefore % 4 + 1, skillsOption.Span);
            Find<Button>(window, "Largura de Habilidades").Command.Execute(null);
            Find<Button>(window, "Largura de Habilidades").Command.Execute(null);
            Find<Button>(window, "Largura de Habilidades").Command.Execute(null);
            Assert.Equal(spanBefore, skillsOption.Span);
            Assert.DoesNotContain(Find<ItemsControl>(window, "Mini bento").Items.Cast<SectionOption>()
                .Where(x => x.IsVisible), x => x.Key == "Attributes");
            modes[1].IsChecked = true;
            model.ToggleLayoutSettingsCommand.Execute(null);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            tabs = Find<TabControl>(window, "Secoes da ficha");
            Assert.Equal(5, tabs.Items.Count);
            Assert.DoesNotContain(model.Editor.VisibleSections, x => x.Key == "Attributes");
            Assert.Equal(3, model.Editor.PhysicalAttributes.Single(x => x.Key == "Stamina").Rating);
            model.Display.UseBento = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var sheetHost = Find<ItemsControl>(window, "Fichas lado a lado");
            Assert.True(model.Display.UseBento);
            Assert.False(model.Display.UseTabs);
            var bento = Assert.Single(Descendants<BentoPanel>(sheetHost));
            var bentoSections = Descendants<SheetSectionView>(window).ToArray();
            Assert.Equal(7, bentoSections.Length);
            Assert.All(bentoSections, x => Assert.True(x.IsCompact));
            Assert.DoesNotContain(Descendants<ScrollViewer>(window), x => x.IsAncestorOf(bento));
            var bentoBottom = bento.TranslatePoint(new Point(0, bento.ActualHeight), window).Y;
            Assert.True(bentoBottom <= window.ActualHeight, $"Bento bottom={bentoBottom}, window={window.ActualHeight}.");
            var bentoTiles = Enumerable.Range(0, bento.Children.Count)
                .Select(i => bento.Children[i]).Where(x => x.RenderSize.Width > 0).ToArray();
            Assert.Equal(7, bentoTiles.Length);
            Assert.All(bentoTiles, x => Assert.True(x.RenderSize.Height > 40));
            SaveSnapshot(window, "bento");
            var bentoRects = bentoTiles.Select(x => new Rect(x.TranslatePoint(new Point(), bento), x.RenderSize)).ToArray();
            for (var i = 0; i < bentoRects.Length; i++)
                for (var j = i + 1; j < bentoRects.Length; j++)
                {
                    var overlap = Rect.Intersect(bentoRects[i], bentoRects[j]);
                    Assert.True(overlap.IsEmpty || overlap.Width < 1 || overlap.Height < 1);
                }
            model.Display.UseBento = false;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(model.Display.IsContinuous);
            Assert.Empty(Descendants<BentoPanel>(sheetHost));
            Assert.Equal(7, Descendants<SheetSectionView>(window).Count());
            Assert.All(Descendants<SheetSectionView>(window), x => Assert.False(x.IsCompact));
            var reopenedSettings = new DisplaySettingsViewModel();
            new DisplaySettingsStore(Path.Combine(directory, "display.json")).Load(reopenedSettings);
            Assert.False(reopenedSettings.Pc.Sections.Single(x => x.Key == "Attributes").IsVisible);
            Assert.Equal("Ficha principal", reopenedSettings.Pc.Sections.Single(x => x.Key == "State").TabName);
            Assert.Equal("Estado de sangue", reopenedSettings.Pc.Sections.Single(x => x.Key == "State").Caption);
            Assert.Equal(1, reopenedSettings.Pc.Sections.Single(x => x.Key == "State").SectionOrder);
    });

    /// <summary>Every application theme applies, stays readable (default themes) and persists.</summary>
    [Fact]
    public Task Application_themes_apply_readably_and_persist() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            await ui.CreatePcAsync(x => x.Clan = "Brujah");
            var menu = Descendants<Menu>(window).Single();
            var settingsMenu = (MenuItem)menu.Items[2];
            var addSheet = Find<Button>(window, "Adicionar ficha");
            var vigor = Find<DotTrack>(window, "Stamina");
            var themeMenu = settingsMenu.Items.OfType<MenuItem>().Last();
            Assert.Equal("Temas", themeMenu.Header);
            var themeScopes = themeMenu.Items.OfType<MenuItem>().ToArray();
            Assert.Equal(["Tema da aplicacao", "Tema da ficha"], themeScopes.Select(x => (string)x.Header));
            var appThemeMenu = themeScopes[0];
            var themeGroups = appThemeMenu.Items.OfType<MenuItem>().ToArray();
            Assert.Equal(["Padrao", "Clas", "Especiais"], themeGroups.Select(x => (string)x.Header));
            Assert.Equal(model.Display.DefaultThemes.Select(x => x.Id),
                ThemeCatalog.All.Where(x => x.Group == ThemeGroup.Default).Select(x => x.Id));
            Assert.Equal(5, model.Display.DefaultThemes.Count());
            Assert.Equal(model.Display.Themes.Length - 5 - model.Display.SpecialThemes.Count(), model.Display.ClanThemes.Count());
            // The predator snapshots need a chosen predator, so the summary line and tags are visible.
            model.Editor!.SelectedPredatorType ??= model.Editor.PredatorTypes.Single(x => x.Id == "alleycat");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            foreach (var option in model.Display.Themes)
            {
                var strict = option.Group == ThemeGroup.Default;
                var groupMenu = themeGroups[(int)option.Group];
                settingsMenu.IsSubmenuOpen = true;
                themeMenu.IsSubmenuOpen = true;
                appThemeMenu.IsSubmenuOpen = true;
                groupMenu.IsSubmenuOpen = true;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var menuPopup = Assert.IsType<Popup>(groupMenu.Template.FindName("PART_Popup", groupMenu));
                Assert.NotNull(menuPopup.Child);
                if (strict) AssertReadableControls(menuPopup.Child);
                var item = Assert.IsType<MenuItem>(groupMenu.ItemContainerGenerator.ContainerFromItem(option));
                Assert.Same(model.Display.SelectThemeCommand, item.Command);
                var peer = new System.Windows.Automation.Peers.MenuItemAutomationPeer(item);
                ((System.Windows.Automation.Provider.IInvokeProvider)peer).Invoke();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.Equal(option.Id, model.Display.ThemeId);
                Assert.True(item.IsChecked);
                Assert.Equal(option.Id, Assert.Single(model.Display.Themes, x => x.IsSelected).Id);
                var palette = ThemeService.Load(ThemeCatalog.Get(option.Id));
                var sheetColors = ThemeService.Load(ThemeCatalog.Get(option.Id), ThemePart.Sheet);
                Assert.Equal(((SolidColorBrush)palette["VcsOuterText"]).Color, ((SolidColorBrush)window.Foreground).Color);
                if (strict)
                    Assert.True(Contrast(((SolidColorBrush)palette["VcsOuterText"]).Color,
                        ((SolidColorBrush)palette["VcsBackground"]).Color) >= 4.5,
                        $"{option.Name}: VcsOuterText/VcsBackground deve ter contraste minimo de 4.5:1.");
                if (option.Id == ThemeCatalog.DefaultId)
                {
                    // Outer text follows the window background; the title bar and top menu use VcsHeaderText;
                    // text inside cards keeps VcsText.
                    var livePalette = app.Resources.MergedDictionaries.Single(x => x.Contains("VcsBackground"));
                    var originalOuter = livePalette["VcsOuterText"];
                    var originalHeader = livePalette["VcsHeaderText"];
                    var sentinel = Color.FromRgb(0x12, 0x9A, 0x34);
                    var headerSentinel = Color.FromRgb(0x34, 0x12, 0x9A);
                    livePalette["VcsOuterText"] = new SolidColorBrush(sentinel);
                    livePalette["VcsHeaderText"] = new SolidColorBrush(headerSentinel);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var topMenu = Descendants<Menu>(window).First().Items.OfType<MenuItem>().First();
                    Assert.Equal(sentinel, ((SolidColorBrush)window.Foreground).Color);
                    Assert.Equal(headerSentinel, ((SolidColorBrush)Descendants<Wpf.Ui.Controls.TitleBar>(window).Single().Foreground).Color);
                    Assert.Equal(headerSentinel, ((SolidColorBrush)topMenu.Foreground).Color);
                    var innerText = ((SolidColorBrush)palette["VcsCardText"]).Color;
                    Assert.Equal(innerText, ((SolidColorBrush)Descendants<TextBlock>(window).First(x => x.Text == "Personagens").Foreground).Color);
                    Assert.DoesNotContain(Descendants<SheetSectionView>(window).SelectMany(Descendants<TextBlock>), x => x.Foreground is SolidColorBrush { Color: var color } && color == sentinel);
                    livePalette["VcsOuterText"] = originalOuter;
                    livePalette["VcsHeaderText"] = originalHeader;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                }
                Assert.Equal(((SolidColorBrush)palette["VcsBackground"]).Color, ((SolidColorBrush)window.Background).Color);
                var titleBar = Descendants<Wpf.Ui.Controls.TitleBar>(window).Single();
                var headerText = ((SolidColorBrush)palette["VcsHeaderText"]).Color;
                Assert.Equal(headerText, ((SolidColorBrush)titleBar.Foreground).Color);
                Assert.Equal(headerText, ((SolidColorBrush)titleBar.ButtonsForeground).Color);
                Assert.Equal(((SolidColorBrush)sheetColors["VcsAccent"]).Color,
                    ((SolidColorBrush)vigor.FindResource("VcsAccent")).Color);
                if (!strict)
                {
                    // Clan palettes keep the user's colors; only check that they load, apply and persist.
                    Assert.Equal(((SolidColorBrush)sheetColors["VcsInputText"]).Color, ((SolidColorBrush)Find<TextBox>(window, "Nome do personagem").Foreground).Color);
                    await WaitUntilAsync(() => !model.Editor.IsDirty);
                    var persistedClan = new DisplaySettingsViewModel();
                    new DisplaySettingsStore(Path.Combine(directory, "display.json")).Load(persistedClan);
                    Assert.Equal(option.Id, persistedClan.ThemeId);
                    continue;
                }
                foreach (var background in new[] { "VcsBackground", "VcsCard", "VcsInput", "VcsHover", "VcsSelection" })
                    foreach (var foreground in new[] { "VcsCardText", "VcsInputDescription", "VcsAccent" })
                        Assert.True(!strict || Contrast(((SolidColorBrush)palette[foreground]).Color,
                            ((SolidColorBrush)palette[background]).Color) >= 4.5,
                            $"{option.Name}: {foreground}/{background} deve ter contraste minimo de 4.5:1.");
                var input = Find<TextBox>(window, "Nome do personagem");
                Assert.Equal(((SolidColorBrush)sheetColors["VcsInput"]).Color, ((SolidColorBrush)input.Background).Color);
                Assert.Equal(((SolidColorBrush)sheetColors["VcsInputText"]).Color, ((SolidColorBrush)input.Foreground).Color);
                Assert.Equal(((SolidColorBrush)sheetColors["VcsSelection"]).Color, ((SolidColorBrush)input.SelectionBrush).Color);
                Assert.Equal(((SolidColorBrush)sheetColors["VcsSelectionText"]).Color, ((SolidColorBrush)input.SelectionTextBrush).Color);
                Assert.Equal(((SolidColorBrush)palette["VcsSelection"]).Color,
                    ((SolidColorBrush)app.Resources[SystemColors.InactiveSelectionHighlightBrushKey]).Color);
                Assert.Equal(((SolidColorBrush)palette["VcsSelectionText"]).Color,
                    ((SolidColorBrush)app.Resources[SystemColors.InactiveSelectionHighlightTextBrushKey]).Color);
                var inputBorder = Assert.IsType<Border>(input.Template.FindName("InputBorder", input));
                Assert.Equal(new CornerRadius(7), inputBorder.CornerRadius);
                Assert.Equal(30, input.MinHeight);
                Assert.True(input.ActualHeight <= 34);
                if (strict) AssertReadableControls(window);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var predatorView = Descendants<PredatorView>(window).Single();
                predatorView.BringIntoView();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                if (strict) AssertReadableControls(predatorView);
                var predatorCombo = Find<ComboBox>(window, "Tipo de predador");
                predatorCombo.IsDropDownOpen = true;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var predatorItem = Assert.IsType<ComboBoxItem>(predatorCombo.ItemContainerGenerator.ContainerFromItem(predatorCombo.SelectedItem));
                Assert.Equal(model.Editor.SelectedPredatorType!.Name, AutomationProperties.GetName(predatorItem));
                Assert.Equal(((SolidColorBrush)sheetColors["VcsSelectionText"]).Color,
                    ((SolidColorBrush)predatorItem.Foreground).Color);
                var predatorChoice = Assert.IsType<Border>(predatorItem.Template.FindName("Choice", predatorItem));
                Assert.True(!strict || Contrast(((SolidColorBrush)predatorItem.Foreground).Color,
                    ((SolidColorBrush)predatorChoice.Background).Color) >= 4.5,
                    $"{option.Name}: a opção selecionada do predador deve continuar legível.");
                if (strict) AssertReadableControls(Assert.IsType<Popup>(predatorCombo.Template.FindName("PART_Popup", predatorCombo)).Child);
                predatorCombo.IsDropDownOpen = false;
                SaveSnapshot(window, option.Id + "-predadores");
                var humanityView = Descendants<HumanityView>(window).Single();
                Find<Expander>(window, "Niveis de Humanidade").IsExpanded = true;
                humanityView.BringIntoView();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                if (strict) AssertReadableControls(humanityView);
                SaveSnapshot(window, option.Id + "-humanidade");
                Find<Expander>(window, "Niveis de Humanidade").IsExpanded = false;
                var loresheetView = Descendants<AdvantageCatalogView>(window).Single();
                loresheetView.BringIntoView();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                if (strict) AssertReadableControls(loresheetView);
                SaveSnapshot(window, option.Id + "-vantagens");
                var mainScroll = Find<ScrollViewer>(window, "Ficha completa");
                mainScroll.ScrollToTop();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                SaveSnapshot(window, option.Id + "-identidade");
                var skillsView = Descendants<SkillsView>(window).Single();
                skillsView.BringIntoView();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                SaveSnapshot(window, option.Id + "-habilidades");
                var personalSkillGroup = model.Editor.SkillGroups[0];
                var personalSkillDraft = Assert.Single(personalSkillGroup.PersonalItems, x => x.IsDraft);
                var personalSkillName = Descendants<TextBox>(skillsView).Single(x =>
                    x.DataContext == personalSkillDraft &&
                    AutomationProperties.GetName(x) == "Nome da habilidade pessoal");
                Assert.Equal(new Thickness(0, 0, 0, 1),
                    Assert.IsType<Border>(personalSkillName.Template.FindName("InputBorder", personalSkillName)).BorderThickness);
                    Assert.DoesNotContain(Descendants<Button>(skillsView),
                        x => AutomationProperties.GetName(x) == "Remover habilidade pessoal");
                    var personalSpecializationCount = Descendants<TextBlock>(skillsView).Single(x =>
                        x.DataContext == personalSkillDraft &&
                        AutomationProperties.GetName(x) == "Quantidade de especializacoes pessoais");
                    var personalSkillTrack = Descendants<DotTrack>(skillsView).Single(x => x.DataContext == personalSkillDraft);
                    Assert.Equal(Visibility.Collapsed, personalSpecializationCount.Visibility);
                    Assert.Equal("+ especialização", Descendants<TextBox>(skillsView).Single(x =>
                        x.DataContext == personalSkillDraft &&
                        AutomationProperties.GetName(x) == "Especializacao da habilidade pessoal").Tag);
                    personalSkillName.Text = "Pista";
                    personalSkillTrack.SetCurrentValue(DotTrack.ValueProperty, 1);
                var personalSpecialization = Descendants<TextBox>(skillsView).Single(x =>
                    x.DataContext == personalSkillDraft &&
                    AutomationProperties.GetName(x) == "Especializacao da habilidade pessoal");
                personalSpecialization.Text = "Investigacao";
                Assert.Equal(1, personalSkillDraft.SpecializationCount);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.Equal(Visibility.Visible, personalSpecializationCount.Visibility);
                Assert.Equal("1 esp.", personalSpecializationCount.Text);
                Assert.Single(personalSkillGroup.PersonalItems, x => x.IsDraft);
                await WaitUntilAsync(() => !model.Editor.IsDirty);
                Assert.Contains((await service.GetAsync(model.Editor.Id)).Traits,
                    x => x.Name == "Pista" && x.Skill == new SkillInfo("Físicas") &&
                         x.Specialization == "Investigacao");
                var skillDescription = Descendants<TextBlock>(skillsView).Single(x => Equals(x.ToolTip, model.Editor.SkillGroups[0].Items[0].Description));
                Assert.False(string.IsNullOrEmpty((string)skillDescription.ToolTip));
                Assert.DoesNotContain(Descendants<Expander>(skillsView), x => Equals(x.Header, "Descricao"));
                var tooltip = new ToolTip { Content = skillDescription.ToolTip, PlacementTarget = skillDescription, IsOpen = true };
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.True(!strict || Contrast(((SolidColorBrush)tooltip.Foreground).Color, ((SolidColorBrush)tooltip.Background).Color) >= 4.5);
                tooltip.IsOpen = false;
                var clanChoice = Find<ComboBox>(window, "Cla");
                clanChoice.ApplyTemplate();
                var clanInput = Assert.IsType<TextBox>(clanChoice.Template.FindName("PART_EditableTextBox", clanChoice));
                Assert.True(!strict || Contrast(((SolidColorBrush)clanInput.Foreground).Color, ((SolidColorBrush)clanInput.Background).Color) >= 4.5);
                Assert.Equal(((SolidColorBrush)sheetColors["VcsSelection"]).Color,
                    ((SolidColorBrush)clanInput.SelectionBrush).Color);
                Assert.Equal(((SolidColorBrush)sheetColors["VcsSelectionText"]).Color,
                    ((SolidColorBrush)clanInput.SelectionTextBrush).Color);
                clanChoice.IsDropDownOpen = true;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var clanRow = Assert.IsType<ComboBoxItem>(clanChoice.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.True(!strict || Contrast(((SolidColorBrush)clanRow.Foreground).Color, ((SolidColorBrush)clanRow.Background).Color) >= 4.5);
                var selectedClanRow = Assert.IsType<ComboBoxItem>(
                    clanChoice.ItemContainerGenerator.ContainerFromItem("Brujah"));
                var selectedClanBackground = Assert.IsType<Border>(
                    selectedClanRow.Template.FindName("Choice", selectedClanRow)).Background;
                Assert.Equal(((SolidColorBrush)sheetColors["VcsSelectionText"]).Color,
                    ((SolidColorBrush)selectedClanRow.Foreground).Color);
                Assert.True(!strict || Contrast(((SolidColorBrush)selectedClanRow.Foreground).Color,
                    ((SolidColorBrush)selectedClanBackground).Color) >= 4.5);
                var clanPopup = Assert.IsType<Popup>(clanChoice.Template.FindName("PART_Popup", clanChoice));
                if (strict) AssertReadableControls(clanPopup.Child);
                clanChoice.IsDropDownOpen = false;
                model.Display.UseTabs = true;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var themedTabs = Find<TabControl>(window, "Secoes da ficha");
                var tabPanel = Assert.IsType<System.Windows.Controls.Primitives.TabPanel>(themedTabs.Template.FindName("HeaderPanel", themedTabs));
                var selectedTab = Assert.IsType<TabItem>(themedTabs.ItemContainerGenerator.ContainerFromItem(themedTabs.SelectedItem));
                Assert.True(!strict || Contrast(((SolidColorBrush)selectedTab.Foreground).Color, ((SolidColorBrush)tabPanel.Background).Color) >= 4.5);
                if (strict) AssertReadableControls(window);
                model.Display.UseTabs = false;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                addSheet.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                if (strict) AssertReadableControls(addSheet.ContextMenu);
                addSheet.ContextMenu.IsOpen = false;
                Assert.False(model.Editor.IsDirty);
                var persisted = new DisplaySettingsViewModel();
                new DisplaySettingsStore(Path.Combine(directory, "display.json")).Load(persisted);
                Assert.Equal(option.Id, persisted.ThemeId);
                // Selecting the current theme must not uncheck the only selected item.
                settingsMenu.IsSubmenuOpen = true;
                themeMenu.IsSubmenuOpen = true;
                appThemeMenu.IsSubmenuOpen = true;
                groupMenu.IsSubmenuOpen = true;
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                ((System.Windows.Automation.Provider.IInvokeProvider)peer).Invoke();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                Assert.True(item.IsChecked);
            }
            settingsMenu.IsSubmenuOpen = false;
    });

    /// <summary>The notes section lists titled notes and edits the selected one, text starting at the top.</summary>
    [Fact]
    public Task Notes_section_lists_and_edits_notes() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            var editor = await ui.CreatePcAsync();
            editor.Notebook.AddCommand.Execute(null);
            editor.Notebook.SelectedNote!.Title = "Pistas";
            editor.Notebook.SelectedNote.Text = "Primeira linha" + Environment.NewLine + "Segunda linha";
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var list = Find<ListBox>(window, "Lista de notas");
            list.BringIntoView();
            await WaitUntilAsync(() => list.IsVisible && list.Items.Count == 1);
            var text = Find<TextBox>(window, "Texto da nota");
            Assert.Equal(VerticalAlignment.Top, text.VerticalContentAlignment);
            Assert.Equal("Pistas", Find<TextBox>(window, "Titulo da nota").Text);
            text.BringIntoView();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            AssertReadableControls(Descendants<SheetSectionView>(window).Single(x => ((EditorSection)x.DataContext).Key == "Notes"));
            SaveSnapshot(window, "notas");
    });

    /// <summary>Editar › Banco de dados lists the four catalogs and adds a skill from the form.</summary>
    [Fact]
    public Task Database_editor_adds_a_skill() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            var editMenu = (MenuItem)Descendants<Menu>(window).Single().Items[1];
            var databaseItem = editMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Header, "_Banco de dados..."));
            await ((CommunityToolkit.Mvvm.Input.IAsyncRelayCommand)databaseItem.Command).ExecuteAsync(null);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(model.IsDatabaseEditorOpen);
            var tabs = Find<TabControl>(window, "Catalogos do banco");
            Assert.Equal(["Habilidades", "Vantagens e defeitos", "Disciplinas e poderes", "Clas"],
                model.Database!.Tabs.Select(x => x.Title));
            var skills = model.Database.Tabs[0];
            Assert.Equal(27, skills.Rows.Count);
            Press(Find<Button>(window, "Novo item em Habilidades"));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Find<TextBox>(window, "Nome da habilidade no banco").Text = "Navegacao";
            await skills.SaveCommand.ExecuteAsync(null);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal("", skills.Error);
            Assert.Equal("Navegacao", skills.SelectedRow!.Title);
            Assert.Equal("seu", skills.SelectedRow.Badge);
            Assert.True(skills.CanDelete);
            AssertReadableControls(Find<Border>(window, "Banco de dados"));
            SaveSnapshot(window, "banco-de-dados");
            tabs.SelectedIndex = 3;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(Find<TextBox>(window, "Nome do cla no banco").IsReadOnly);
            await model.ToggleDatabaseEditorCommand.ExecuteAsync(null);
            Assert.False(model.IsDatabaseEditorOpen);
    });

    /// <summary>A sheet theme recolors only that sheet and persists per character.</summary>
    [Fact]
    public Task Sheet_theme_overrides_only_its_sheet() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            await ui.CreatePcAsync();
            // A sheet theme recolors only that sheet; the application keeps its own theme.
            var appPalette = ThemeService.Load(ThemeCatalog.Get(model.Display.ThemeId));
            var followedPalette = ThemeService.Load(ThemeCatalog.Get(model.Display.ThemeId), ThemePart.Sheet);
            var sheetPalette = ThemeService.Load(ThemeCatalog.Get("brujah"), ThemePart.Sheet);
            var appInput = ((SolidColorBrush)followedPalette["VcsInput"]).Color;
            var sheetInput = ((SolidColorBrush)sheetPalette["VcsInput"]).Color;
            Assert.NotEqual(appInput, sheetInput);
            model.SelectSheetThemeCommand.Execute("brujah");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.False(model.Display.SheetFollowsApp);
            Assert.True(Assert.Single(model.Display.SheetThemes, x => x.IsSelected).Id == "brujah");
            Assert.Equal(sheetInput, ((SolidColorBrush)Find<TextBox>(window, "Nome do personagem").Background).Color);
            Assert.Equal(((SolidColorBrush)appPalette["VcsBackground"]).Color, ((SolidColorBrush)window.Background).Color);
            var sheetSettings = new DisplaySettingsViewModel();
            new DisplaySettingsStore(Path.Combine(directory, "display.json")).Load(sheetSettings);
            Assert.Equal("brujah", sheetSettings.SheetThemeFor(model.Editor!.Id));
            model.SelectSheetThemeCommand.Execute("");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(model.Display.SheetFollowsApp);
            Assert.Equal(appInput, ((SolidColorBrush)Find<TextBox>(window, "Nome do personagem").Background).Color);
            window.Width = 1000;
            window.Height = 680;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var scroll = Find<ScrollViewer>(window, "Ficha completa");
            Assert.Equal(0, scroll.ScrollableWidth);
            Assert.True(scroll.ScrollableHeight > 0);
            scroll.ScrollToBottom();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(scroll.VerticalOffset > 0);
    });

    /// <summary>Home list, sidebar navigation and scroll chaining.</summary>
    [Fact]
    public Task Home_list_sidebar_and_scrolling_work() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            await ui.CreatePcAsync();
            var menu = Descendants<Menu>(window).Single();
            var settingsMenu = (MenuItem)menu.Items[2];
            var reopenedSettings = new DisplaySettingsViewModel();
            await model.BackCommand.ExecuteAsync(null);
            Assert.True(model.IsHome);
            Assert.Single(model.Characters);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var homeList = Find<ListBox>(window, "Lista de personagens");
            var homeRow = Assert.IsType<ListBoxItem>(homeList.ItemContainerGenerator.ContainerFromIndex(0));
            Assert.InRange(homeRow.ActualHeight, 1, 50);
            var homeRowSurface = Descendants<Grid>(homeRow).Single(x => x.ContextMenu is not null);
            var homeContextMenu = homeRowSurface.ContextMenu!;
            homeContextMenu.PlacementTarget = homeRowSurface;
            homeContextMenu.IsOpen = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Same(model, homeContextMenu.DataContext);
            Assert.Equal(model.Characters[0].Id,
                Assert.IsType<CharacterSummary>(((MenuItem)homeContextMenu.Items[0]).CommandParameter).Id);
            homeContextMenu.IsOpen = false;
            foreach (var themeOption in model.Display.Themes)
            {
                model.Display.SelectThemeCommand.Execute(themeOption.Id);
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var list = Find<ListBox>(window, "Lista de personagens");
                var row = Assert.IsType<ListBoxItem>(list.ItemContainerGenerator.ContainerFromIndex(0));
                row.IsSelected = true;
                row.Focus();
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                var border = Assert.IsType<Border>(row.Template.FindName("Row", row));
                Assert.Equal(((SolidColorBrush)window.FindResource("VcsSelection")).Color,
                    ((SolidColorBrush)border.Background).Color);
                Assert.Equal(((SolidColorBrush)window.FindResource("VcsAccent")).Color,
                    ((SolidColorBrush)border.BorderBrush).Color);
            }
            var originalId = model.Characters[0].Id;
            var newSide = Find<Button>(window, "Adicionar ficha");
            var newSidePeer = new System.Windows.Automation.Peers.ButtonAutomationPeer(newSide);
            ((System.Windows.Automation.Provider.IInvokeProvider)newSidePeer).Invoke();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(newSide.ContextMenu.IsOpen);
            var pcMenuItem = newSide.ContextMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Header, "PC / Personagem"));
            Assert.Same(model.CreateCommand, pcMenuItem.Command);
            Assert.Same(model.CreateNpcCommand, newSide.ContextMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Header, "NPC / Ficha pratica")).Command);
            var pcPeer = new System.Windows.Automation.Peers.MenuItemAutomationPeer(pcMenuItem);
            ((System.Windows.Automation.Provider.IInvokeProvider)pcPeer).Invoke();
            await WaitUntilAsync(() => model.Editor is not null);
            Assert.NotEqual(originalId, model.Editor!.Id);
            model.Editor.Name = "Ficha da lateral";
            var lateral = Find<ListBox>(window, "Lista lateral de personagens");
            lateral.UpdateLayout();
            var originalButton = Descendants<Button>(lateral).Single(x =>
                x.DataContext is CharacterSummary summary && summary.Id == originalId);
            var sidePeer = new System.Windows.Automation.Peers.ButtonAutomationPeer(originalButton);
            ((System.Windows.Automation.Provider.IInvokeProvider)sidePeer).Invoke();
            await WaitUntilAsync(() => model.Editor?.Id == originalId);
            Assert.Contains(await service.ListAsync(), x => x.Name == "Ficha da lateral");
            var sideOption = settingsMenu.Items.OfType<MenuItem>().Single(x => Equals(x.Header, "Lista lateral de personagens"));
            sideOption.IsChecked = false;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.False(lateral.IsVisible);
            new DisplaySettingsStore(Path.Combine(directory, "display.json")).Load(reopenedSettings);
            Assert.False(reopenedSettings.ShowSidebar);
            await VerifyScrollInteractionAsync(window);
    });

    /// <summary>NPC sheet sections, tabs, PDF export and side-by-side panes with sheet themes.</summary>
    [Fact]
    public Task Npc_sheet_and_side_by_side_panes_work() => _wpf.RunAsync(async ui =>
    {
            var (app, window, model, service, dialogs, directory) = ui;
            var originalId = (await ui.CreatePcAsync()).Id;
            var openBefore = model.OpenEditors.Count;
            await model.CreateNpcCommand.ExecuteAsync(null);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var npc = model.Editor!;
            Assert.True(npc.IsNpc);
            Assert.Equal(openBefore + 1, model.OpenEditors.Count);
            Assert.Contains(model.OpenEditors, x => x.Id == originalId);
            Assert.Equal("FICHA DE NPC", Find<TextBlock>(window, "FICHA DE NPC").Text);
            // The NPC sheet is split into sections like the PC sheet, one NPC part per section.
            Assert.Equal(7, Descendants<NpcQuickSheetView>(window).Count());
            Assert.Equal(7, Descendants<SheetSectionView>(window).Count());
            Assert.Empty(Descendants<AdvancementView>(window));
            Find<TextBox>(window, "Nome do NPC").Text = "NPC visual";
            Assert.Equal("NPC visual", npc.Name);
            Assert.Equal("+1 dados", npc.NpcBloodSurge.Value);
            Find<DotTrack>(window, "Potencia de sangue NPC").SetCurrentValue(DotTrack.ValueProperty, 3);
            Assert.Equal("+3 dados", npc.NpcBloodSurge.Value);
            Assert.Equal("2 superficial", npc.NpcDamageMend.Value);
            Assert.Equal("+1 dados", npc.NpcPowerBonus.Value);
            Find<TextBox>(window, "Bonus de poder NPC").Text = "+2 (Fortitude)";
            Find<DotTrack>(window, "Potencia de sangue NPC").SetCurrentValue(DotTrack.ValueProperty, 4);
            Assert.Equal("+3 dados", npc.NpcBloodSurge.Value);
            Assert.Equal("+2 (Fortitude)", npc.NpcPowerBonus.Value);
            var npcHealth = Find<BoxTrack>(window, "Vitalidade NPC");
            Descendants<Button>(npcHealth).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, npc.HealthSuperficial);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Descendants<Button>(npcHealth).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, npc.HealthAggravated);
            Assert.Equal(0, npc.HealthSuperficial);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Descendants<Button>(npcHealth).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(0, npc.HealthAggravated);
            var npcWillpower = Find<BoxTrack>(window, "Vontade NPC");
            Descendants<Button>(npcWillpower).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Assert.Equal(1, npc.WillpowerAggravated);
            Assert.Equal(0, npc.WillpowerSuperficial);
            for (var i = 0; i < 25; i++) Press(Find<Button>(window, "Adicionar caixa de vitalidade"));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(30, npc.NpcHealthCapacity);
            Assert.Equal(30, Descendants<Button>(Find<BoxTrack>(window, "Vitalidade NPC")).Count());
            var willpowerBoxes = npc.NpcWillpowerCapacity;
            Press(Find<Button>(window, "Remover caixa de vontade"));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(willpowerBoxes - 1, npc.NpcWillpowerCapacity);
            Assert.Single(npc.NpcAttacks);
            Find<TextBox>(window, "Ataque").Text = "Garras";
            Press(Find<Button>(window, "Adicionar ataque"));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(2, npc.NpcAttacks.Count);
            Descendants<TextBox>(window).Last(x => AutomationProperties.GetName(x) == "Parada de dados").Text = "Forca + Briga 6";
            Assert.Equal("Forca + Briga 6", npc.NpcAttacks[1].DicePool);
            Press(Descendants<Button>(window).Last(x => AutomationProperties.GetName(x) == "Remover ataque"));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal("Garras", Assert.Single(npc.NpcAttacks).Name);
            Find<TextBox>(window, "Cronica do NPC").Text = "Chicago";
            Assert.Equal("Chicago", npc.Chronicle);
            Find<DotTrack>(window, "Fisicos do NPC").SetCurrentValue(DotTrack.ValueProperty, 3);
            Assert.Equal(3, npc.NpcPhysical);
            Assert.False(Find<ItemsControl>(window, "Disciplinas da ficha").IsVisible);
            Assert.Equal(CharacterEditorViewModel.NpcDisciplineSlots,
                Descendants<SuggestionBox>(Find<ItemsControl>(window, "Disciplinas do NPC")).Count());
            Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind, Find<ItemsControl>(window, "Vantagens da ficha").Items.Count);
            Assert.Equal(CharacterEditorViewModel.NpcMeritSlotsPerKind, Find<ItemsControl>(window, "Defeitos da ficha").Items.Count);
            Find<TextBox>(window, "Idade verdadeira do NPC").Text = "300";
            Find<TextBox>(window, "Historia do NPC").Text = "Chegou em 1720.";
            Assert.Equal("300", npc.NpcTrueAge.Value);
            Assert.Equal("Chegou em 1720.", npc.NpcHistory.Value);
            SaveSnapshot(window, "npc-sheet");
            Assert.True(await npc.FlushAsync());
            var savedNpc = await service.GetAsync(npc.Id);
            Assert.Equal(30, savedNpc.Details.NpcHealthCapacity);
            Assert.Equal("+3 dados", savedNpc.Details.BloodSurge);
            Assert.Equal("Garras", Assert.Single(savedNpc.Details.NpcAttacks).Name);
            await model.ExportPdfCommand.ExecuteAsync(null);
            Assert.Equal("", model.Error);
            Assert.True(File.Exists(dialogs.PdfPath));
            model.Display.UseTabs = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            // NPCs follow the tab layout too: only the selected tab's parts are shown.
            Assert.Equal(["Combate", "Atributos", "Disciplinas", "Vantagens", "Histórico"], npc.VisibleTabs.Select(x => x.Title));
            Assert.Equal(npc.SelectedTab!.Sections.Count, Descendants<NpcQuickSheetView>(window).Count());
            model.Display.UseTabs = false;
            var openTabs = Find<ItemsControl>(window, "Fichas abertas");
            Assert.True(openTabs.IsVisible);
            Assert.Equal(model.OpenEditors.Count, openTabs.Items.Count);
            var original = model.OpenEditors.Single(x => x.Id == originalId);
            Press(Descendants<Button>(openTabs).Single(x => ReferenceEquals(x.CommandParameter, original) &&
                AutomationProperties.GetName(x).StartsWith("Aba ")));
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Same(original, model.Editor);
            Assert.True(original.IsActive);
            Assert.False(npc.IsActive);
            model.Display.UseBento = true;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var sheetGrid = Find<ItemsControl>(window, "Fichas lado a lado");
            Assert.True(sheetGrid.IsVisible);
            // Right click: sheet theme on the sheets, application theme everywhere else.
            Assert.Equal(["Tema da ficha", "Tema da aplicacao"], sheetGrid.ContextMenu.Items.OfType<MenuItem>().Select(x => (string)x.Header));
            Assert.Equal(["Tema da aplicacao"], ((FrameworkElement)window.Content).ContextMenu.Items.OfType<MenuItem>().Select(x => (string)x.Header));
            // Without a background, empty space is not hit-testable and the right click opens nothing.
            Assert.NotNull(((Panel)window.Content).Background);
            Assert.NotNull(sheetGrid.Background);
            var panes = Descendants<CharacterEditorView>(sheetGrid).ToArray();
            await WaitUntilAsync(() => Descendants<CharacterEditorView>(window).Count() == model.OpenEditors.Count);
            Assert.Equal(model.OpenEditors.Count, panes.Length);
            Assert.All(panes, x => Assert.True(x.IsPane));
            Assert.Contains(Descendants<System.Windows.Controls.Primitives.UniformGrid>(sheetGrid),
                x => x.Columns == model.SheetColumns && x.Children.Count == model.OpenEditors.Count);
            Assert.Equal(7, Descendants<NpcQuickSheetView>(sheetGrid).Count());
            SaveSnapshot(window, "npc-side-by-side");
            // Side by side, each pane keeps its own sheet theme.
            Assert.Same(original, model.Editor);
            model.SelectSheetThemeCommand.Execute("brujah");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Color PaneCard(CharacterEditorViewModel editor) => ((SolidColorBrush)Descendants<Border>(sheetGrid)
                .Single(x => x.Name == "Pane" && ReferenceEquals(x.DataContext, editor)).Background).Color;
            var brujahCard = ((SolidColorBrush)ThemeService.Load(ThemeCatalog.Get("brujah"), ThemePart.Sheet)["VcsCard"]).Color;
            var appCard = ((SolidColorBrush)ThemeService.Load(ThemeCatalog.Get(model.Display.ThemeId), ThemePart.Sheet)["VcsCard"]).Color;
            Assert.Equal(brujahCard, PaneCard(original));
            Assert.Equal(appCard, PaneCard(npc));
            SaveSnapshot(window, "sheet-themes-side-by-side");
            model.SelectSheetThemeCommand.Execute("");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Equal(appCard, PaneCard(original));
            var beforeClose = model.OpenEditors.Count;
            Press(Find<Button>(sheetGrid, $"Fechar painel {npc.TabTitle}"));
            await WaitUntilAsync(() => model.OpenEditors.Count == beforeClose - 1);
            Assert.DoesNotContain(npc, model.OpenEditors);
            model.Display.UseBento = false;
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.Single(Descendants<CharacterEditorView>(window));
            await model.BackCommand.ExecuteAsync(null);
            Assert.True(model.IsHome);
            Assert.Equal(beforeClose - 1, model.OpenEditors.Count);
    });

    private static void Press(Button button) =>
        ((System.Windows.Automation.Provider.IInvokeProvider)new System.Windows.Automation.Peers.ButtonAutomationPeer(button)).Invoke();

    private static T Find<T>(DependencyObject root, string name) where T : DependencyObject
    {
        var values = Descendants<T>(root).ToArray();
        Assert.Contains(values, x => AutomationProperties.GetName(x) == name);
        return values.Single(x => AutomationProperties.GetName(x) == name);
    }

    private static async Task AssertComboInteractionAsync(ComboBox combo)
    {
        var originalIndex = combo.SelectedIndex;
        var originalText = combo.Text;
        combo.BringIntoView();
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        var toggle = Assert.IsType<ToggleButton>(combo.Template.FindName("DropButton", combo));
        var placeholder = Assert.IsType<TextBlock>(combo.Template.FindName("Placeholder", combo));
        Assert.False(string.IsNullOrWhiteSpace(placeholder.Text));
        Assert.Equal(string.IsNullOrEmpty(combo.Text) ? Visibility.Visible : Visibility.Collapsed, placeholder.Visibility);
        var arrow = Assert.Single(Descendants<System.Windows.Shapes.Path>(toggle));
        var arrowX = arrow.TransformToAncestor(combo).Transform(new Point()).X;
        Assert.True(arrowX > combo.ActualWidth / 2,
            $"Arrow X={arrowX}, combo width={combo.ActualWidth}, toggle width={toggle.ActualWidth}, alignment={toggle.HorizontalAlignment}.");
        if (!combo.IsEditable)
        {
            Assert.Equal(combo.ActualWidth, toggle.ActualWidth, 1);
            Assert.True(toggle.IsAncestorOf(Assert.IsAssignableFrom<DependencyObject>(
                combo.InputHitTest(new Point(12, combo.ActualHeight / 2)))));
        }
        ((System.Windows.Automation.Provider.IToggleProvider)new System.Windows.Automation.Peers.ToggleButtonAutomationPeer(toggle)
            .GetPattern(System.Windows.Automation.Peers.PatternInterface.Toggle)).Toggle();
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        var popup = Assert.IsType<Popup>(combo.Template.FindName("PART_Popup", combo));
        Assert.True(popup.IsOpen);
        Assert.Same(combo, popup.PlacementTarget);
        Assert.True(popup.Child.RenderSize.Width >= combo.ActualWidth);
        var choices = new System.Windows.Automation.Peers.ComboBoxAutomationPeer(combo).GetChildren()
            .Select(x => x.GetPattern(System.Windows.Automation.Peers.PatternInterface.SelectionItem))
            .OfType<System.Windows.Automation.Provider.ISelectionItemProvider>().ToArray();
        Assert.Equal(combo.Items.Count, choices.Length);
        choices[1].Select();
        Assert.Equal(1, combo.SelectedIndex);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
        combo.IsDropDownOpen = false;
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
        Assert.False(popup.IsOpen);
        combo.SetCurrentValue(ComboBox.SelectedIndexProperty, originalIndex);
        if (combo.IsEditable) combo.SetCurrentValue(ComboBox.TextProperty, originalText);
        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    }

    private static void AssertReadableControls(DependencyObject root)
    {
        foreach (var text in Descendants<TextBlock>(root).Where(x => x.IsVisible && !string.IsNullOrEmpty(x.Text)))
        {
            DependencyObject? current = text;
            SolidColorBrush? background = null;
            while (current is not null)
            {
                var brush = current switch
                {
                    Border border => border.Background, Panel panel => panel.Background,
                    // A Wpf.Ui radio button paints its Background inside the check circle, not behind the label.
                    RadioButton => null,
                    Control control => control.Background, _ => null
                };
                if (brush is SolidColorBrush { Color.A: 255 } solid) { background = solid; break; }
                current = VisualTreeHelper.GetParent(current);
            }

            if (background is not null && text.Foreground is SolidColorBrush foreground)
                Assert.True(Contrast(foreground.Color, background.Color) >= 4.5,
                    $"Texto '{text.Text}': {foreground.Color} sobre {background.Color}.");
        }
        foreach (var bar in Descendants<ScrollBar>(root).Where(x => x.IsVisible))
        {
            var track = Assert.IsType<Track>(bar.Template.FindName("PART_Track", bar));
            var grip = Assert.IsType<Border>(track.Thumb.Template.FindName("Grip", track.Thumb));
            Assert.True(Contrast(((SolidColorBrush)grip.Background).Color, ((SolidColorBrush)bar.Background).Color) >= 3);
            Assert.True(bar.Orientation == Orientation.Vertical ? bar.ActualWidth >= 16 : bar.ActualHeight >= 16);
        }
    }

    private static string ActiveHelpText(IdentitySheetView view) =>
        Assert.IsType<TextBlock>(Assert.IsType<ToolTip>(view.ActiveHelp).Content).Text;

    private static void SaveSnapshot(FrameworkElement element, string name)
    {
        var directory = Environment.GetEnvironmentVariable("VCS_UI_SNAPSHOTS");
        if (string.IsNullOrWhiteSpace(directory)) return;
        Directory.CreateDirectory(directory);
        var bitmap = new RenderTargetBitmap((int)element.ActualWidth, (int)element.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(element);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var stream = File.Create(Path.Combine(directory, name + ".png"));
        encoder.Save(stream);
    }

    private static async Task VerifyScrollInteractionAsync(Window owner)
    {
        var inner = new ScrollViewer { Height = 100, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new Border { Height = 700, Width = 700 }, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto };
        var outer = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel { Children = { inner, new Border { Height = 1200 } } } };
        var host = new Window { Owner = owner, Width = 300, Height = 260, Content = outer };
        try
        {
            host.Show();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var vertical = Descendants<ScrollBar>(inner).Single(x => x.Orientation == Orientation.Vertical);
            var track = Assert.IsType<Track>(vertical.Template.FindName("PART_Track", vertical));
            track.Thumb.RaiseEvent(new DragDeltaEventArgs(0, 15) { RoutedEvent = Thumb.DragDeltaEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(inner.VerticalOffset > 0);
            var horizontal = Descendants<ScrollBar>(inner).Single(x => x.Orientation == Orientation.Horizontal);
            ((Track)horizontal.Template.FindName("PART_Track", horizontal)).Thumb.RaiseEvent(
                new DragDeltaEventArgs(20, 0) { RoutedEvent = Thumb.DragDeltaEvent });
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(inner.HorizontalOffset > 0);
            inner.ScrollToBottom();
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var wheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent };
            inner.RaiseEvent(wheel);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            Assert.True(wheel.Handled);
            Assert.True(outer.VerticalOffset > 0);
            inner.ScrollToVerticalOffset(100);
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            var offset = outer.VerticalOffset;
            var innerWheel = new MouseWheelEventArgs(Mouse.PrimaryDevice, 0, -120) { RoutedEvent = Mouse.PreviewMouseWheelEvent };
            inner.RaiseEvent(innerWheel);
            Assert.False(innerWheel.Handled);
            Assert.Equal(offset, outer.VerticalOffset);
            AssertReadableControls(host);
        }
        finally { host.Close(); }
    }

    private static double Contrast(Color first, Color second)
    {
        static double Channel(byte value)
        {
            var normalized = value / 255.0;
            return normalized <= 0.04045 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }
        static double Luminance(Color color) => 0.2126 * Channel(color.R) + 0.7152 * Channel(color.G) + 0.0722 * Channel(color.B);
        var a = Luminance(first);
        var b = Luminance(second);
        return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        var timeout = Stopwatch.StartNew();
        while (!condition() && timeout.Elapsed < TimeSpan.FromSeconds(10)) await Task.Delay(50);
        Assert.True(condition(), "A operacao visual nao concluiu no prazo.");
    }

    /// <summary>Visual descendants; open sheets that are built but not shown are skipped.</summary>
    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is CharacterEditorView { IsVisible: false }) continue;
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }


}
