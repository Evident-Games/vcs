using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using VCS.Application.Characters;
using VCS.Desktop.Controls;
using VCS.Desktop.Services;
using VCS.Desktop.ViewModels;
using VCS.Desktop.Views;
using VCS.Infrastructure.Files;
using VCS.Infrastructure.Persistence;
using VCS.Infrastructure.Repositories;

namespace VCS.Tests;

public sealed class DesktopUiTests
{
    [Fact]
    public async Task Full_sheet_menus_tabs_sections_themes_and_autosave_work()
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext());
            Dispatcher.CurrentDispatcher.BeginInvoke(async () =>
            {
                var directory = Path.Combine(Path.GetTempPath(), "VCS-ui-" + Guid.NewGuid().ToString("N"));
                var app = new System.Windows.Application();
                var bindings = new BindingErrorListener();
                MainWindow? window = null;
                try
                {
                    app.Resources = new ResourceDictionary { Source = new Uri("/VCS.Desktop;component/Themes/Resources.xaml", UriKind.Relative) };
                    app.ShutdownMode = ShutdownMode.OnExplicitShutdown;
                    PresentationTraceSources.DataBindingSource.Listeners.Add(bindings);
                    PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Error;
                    var database = new DatabaseAccess(Path.Combine(directory, "test.db"));
                    await new SqliteDatabaseMaintenance(database).InitializeAsync();
                    var service = new CharacterService(new SqliteCharacterRepository(database));
                    var theme = new ThemeService();
                    var dialogs = new TestDialogs { PdfPath = Path.Combine(directory, "ui-npc.pdf") };
                    var model = new MainViewModel(service, new JsonCharacterFiles(), new SqliteDatabaseMaintenance(database),
                        dialogs, theme, dto => new(dto, service, NullLogger<CharacterEditorViewModel>.Instance,
                            new DispatcherAutosaveScheduler(), new ProvidedDisciplineCatalog()),
                        NullLogger<MainViewModel>.Instance, new DisplaySettingsStore(Path.Combine(directory, "display.json")),
                        new SqliteAdvantageCatalog(database), new SqliteSkillCatalog(database),
                        new CharacterPdfExporter(new ProvidedDisciplineCatalog(), new SqliteSkillCatalog(database)));
                    window = new MainWindow(model);
                    app.MainWindow = window;
                    window.Show();
                    await model.InitializeAsync();
                    var searchField = Find<TextBox>(window, "Busca lateral de personagens");
                    var addSheet = Find<Button>(window, "Adicionar ficha");
                    Assert.Equal(24, searchField.ActualHeight);
                    Assert.Equal(24, addSheet.ActualHeight);
                    var searchBar = Assert.IsType<Border>(VisualTreeHelper.GetParent(VisualTreeHelper.GetParent(searchField)));
                    Assert.Equal(30, searchBar.ActualHeight);
                    Assert.Equal(new CornerRadius(15), searchBar.CornerRadius);
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
                    var advantageSearch = Find<TextBox>(window, "Buscar vantagem ou defeito");
                    advantageSearch.Text = "deslumbrante";
                    var advantageChoice = Find<ListBox>(window, "Catalogo de vantagens e defeitos");
                    Assert.Single(advantageChoice.Items.Cast<object>());
                    var advantageType = Find<ComboBox>(window, "Filtrar vantagens ou defeitos");
                    advantageType.SelectedItem = "Defeitos";
                    Assert.Empty(advantageChoice.Items.Cast<object>());
                    advantageType.SelectedItem = "Vantagens";
                    Assert.Single(advantageChoice.Items.Cast<object>());
                    Assert.Equal(4, Find<NumericField>(window, "Pontos da vantagem ou defeito").Value);
                    var addAdvantage = Descendants<Button>(window).Single(x => Equals(x.Content, "Adicionar a ficha"));
                    var advantagePeer = new System.Windows.Automation.Peers.ButtonAutomationPeer(addAdvantage);
                    ((System.Windows.Automation.Provider.IInvokeProvider)advantagePeer).Invoke();
                    await WaitUntilAsync(() => model.Editor.Advantages.Items.Count == 1 && !model.Editor.IsDirty);
                    Assert.Equal("Deslumbrante", model.Editor.Advantages.Items[0].Name);
                    Assert.Equal(4, model.Editor.Advantages.Items[0].Rating);
                    var loreCollection = Find<ComboBox>(window, "Colecao de loresheet");
                    loreCollection.SelectedItem = "Os Bahari";
                    var loreList = Find<ListBox>(window, "Catalogo de loresheets");
                    Assert.Equal(5, loreList.Items.Count);
                    loreList.SelectedItem = model.Editor.LoresheetBenefits.Single(x => x.Minimum == 2);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var addLore = Find<Button>(window, "Adicionar beneficio de loresheet");
                    Assert.NotNull(model.Editor.SelectedLoresheetBenefit);
                    Assert.Equal(2, model.Editor.SelectedLoresheetBenefit.Minimum);
                    Assert.True(addLore.IsEnabled);
                    Assert.Same(model.Editor.AddLoresheetCommand, addLore.Command);
                    var lorePeer = new System.Windows.Automation.Peers.ButtonAutomationPeer(addLore);
                    ((System.Windows.Automation.Provider.IInvokeProvider)lorePeer).Invoke();
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.True(model.Editor.Error.Length == 0, model.Editor.Error);
                    Assert.Single(model.Editor.Loresheets.Items);
                    Assert.Equal("Escarificação Ritual", model.Editor.Loresheets.Items[0].Name);
                    Assert.Equal(0, model.Editor.ExperienceSpent);
                    loreCollection.SelectedItem = "Theo Bell";
                    loreList.SelectedItem = model.Editor.LoresheetBenefits.Single(x => x.Minimum == 1);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    ((System.Windows.Automation.Provider.IInvokeProvider)lorePeer).Invoke();
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.Contains("somente um beneficio geral", model.Editor.Error);
                    model.Editor.RemoveInitialLoresheetCommand.Execute(model.Editor.Loresheets.Items[0]);
                    loreCollection.SelectedItem = "Os Bahari";
                    loreList.SelectedItem = model.Editor.LoresheetBenefits.Single(x => x.Minimum == 2);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    ((System.Windows.Automation.Provider.IInvokeProvider)lorePeer).Invoke();
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    await WaitUntilAsync(() => !model.Editor.IsDirty);
                    var name = Find<TextBox>(window, "Nome do personagem");
                    name.Text = "Smoke UI";
                    var clan = Find<ComboBox>(window, "Cla");
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
                    Assert.Equal(2, saved.Details.Hunger);
                    Assert.Equal(4, saved.Details.BloodPotency);
                    Assert.Contains(saved.Traits, x => x.Category == VCS.Domain.Characters.TraitCategory.Discipline && x.Name == "Ofuscação" && x.Rating == 2);
                    Assert.Equal(1, saved.Details.HealthSuperficial);
                    Assert.Equal(3, saved.Traits.Single(x => x.Key == "Stamina").Rating);
                    name.Text = "";
                    Assert.True(model.Editor.HasErrors);
                    Assert.True(Validation.GetHasError(name));
                    name.Text = "Smoke UI";
                    await WaitUntilAsync(() => !model.Editor.IsDirty);
                    var skillTrack = Find<DotTrack>(window, "Armas Brancas");
                    var firstDot = Descendants<Button>(skillTrack).Single(x => x.Tag is 1);
                    firstDot.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(1, skillTrack.Value);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Descendants<Button>(skillTrack).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(0, skillTrack.Value);
                    Assert.DoesNotContain(Descendants<Button>(skillTrack), x => Equals(x.Content, "0"));
                    Find<TextBox>(window, "Especializacao de Armas Brancas").Text = "Esgrima / estacas";
                    var attributeTrack = Find<DotTrack>(window, "Strength");
                    Descendants<Button>(attributeTrack).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(1, attributeTrack.Value);
                    obfuscate.SetCurrentValue(DotTrack.ValueProperty, 1);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Descendants<Button>(obfuscate).Single(x => x.Tag is 1).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(0, obfuscate.Value);
                    await WaitUntilAsync(() => !model.Editor.IsDirty);
                    Assert.Contains((await service.GetAsync(model.Editor.Id)).Traits,
                        x => x.Specialization == "Esgrima / estacas");

                    var power = model.Editor.Powers.Powers.First();
                    var powerCheckbox = Descendants<CheckBox>(window).First(x =>
                        ReferenceEquals(x.DataContext, power) && AutomationProperties.GetName(x) == power.Definition.Name);
                    powerCheckbox.IsChecked = true;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    await WaitUntilAsync(() => power.IsKnown && !model.Editor.IsDirty);
                    Assert.True(await model.Editor.FlushAsync());
                    Assert.Contains((await service.GetAsync(model.Editor.Id)).Traits, x => x.Key == power.Definition.Id);

                    var settingsMenu = (MenuItem)menu.Items[2];
                    var tabsOption = (MenuItem)settingsMenu.Items[0];
                    tabsOption.IsChecked = true;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.True(model.Display.UseTabs);
                    var tabs = Find<TabControl>(window, "Secoes da ficha");
                    Assert.Equal(7, tabs.Items.Count);
                    foreach (var section in model.Editor.VisibleTabs.ToArray())
                    {
                        tabs.SelectedItem = section;
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Assert.Equal(section.Sections.Count, Descendants<SheetSectionView>(window).Count());
                    }
                    var organize = (MenuItem)settingsMenu.Items[1];
                    organize.Command.Execute(null);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.True(model.IsLayoutSettingsOpen);
                    Find<TextBox>(window, "Aba de Identidade").Text = "Ficha principal";
                    Find<TextBox>(window, "Aba de Atributos").Text = "Ficha principal";
                    Find<TextBox>(window, "Aba de Estado e sangue").Text = "Ficha principal";
                    Find<TextBox>(window, "Titulo de Estado e sangue").Text = "Estado de sangue";
                    Find<NumericField>(window, "Ordem de Estado e sangue").SetCurrentValue(NumericField.ValueProperty, 1);
                    Assert.Equal(6, model.Editor.VisibleTabs.Count);
                    Assert.Equal(3, model.Editor.VisibleTabs[0].Sections.Count);
                    Assert.False(model.Editor.IsDirty);
                    model.ToggleLayoutSettingsCommand.Execute(null);
                    var sectionMenu = (MenuItem)settingsMenu.Items[2];
                    settingsMenu.IsSubmenuOpen = true;
                    sectionMenu.IsSubmenuOpen = true;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var attributesOption = model.Display.Sections.Single(x => x.Key == "Attributes");
                    var attributesMenu = Assert.IsType<MenuItem>(sectionMenu.ItemContainerGenerator.ContainerFromItem(attributesOption));
                    attributesMenu.IsChecked = false;
                    sectionMenu.IsSubmenuOpen = false;
                    settingsMenu.IsSubmenuOpen = false;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.Equal(6, tabs.Items.Count);
                    Assert.DoesNotContain(model.Editor.VisibleSections, x => x.Key == "Attributes");
                    Assert.Equal(3, model.Editor.PhysicalAttributes.Single(x => x.Key == "Stamina").Rating);
                    model.Display.UseTabs = false;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.Equal(7, Descendants<SheetSectionView>(window).Count());
                    var themeMenu = settingsMenu.Items.OfType<MenuItem>().Last();
                    Assert.Equal("Temas", themeMenu.Header);
                    Assert.Equal(5, themeMenu.Items.Count);
                    foreach (var option in model.Display.Themes)
                    {
                        settingsMenu.IsSubmenuOpen = true;
                        themeMenu.IsSubmenuOpen = true;
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        var menuPopup = Assert.IsType<Popup>(themeMenu.Template.FindName("PART_Popup", themeMenu));
                        Assert.NotNull(menuPopup.Child);
                        AssertReadableControls(menuPopup.Child);
                        var item = Assert.IsType<MenuItem>(themeMenu.ItemContainerGenerator.ContainerFromItem(option));
                        Assert.Same(model.Display.SelectThemeCommand, item.Command);
                        var peer = new System.Windows.Automation.Peers.MenuItemAutomationPeer(item);
                        ((System.Windows.Automation.Provider.IInvokeProvider)peer).Invoke();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Assert.Equal(option.Id, model.Display.ThemeId);
                        Assert.True(item.IsChecked);
                        Assert.Equal(option.Id, Assert.Single(model.Display.Themes, x => x.IsSelected).Id);
                        var palette = new ResourceDictionary
                        {
                            Source = new Uri($"/VCS.Desktop;component/Themes/{ThemeCatalog.Get(option.Id).ResourceName}.xaml", UriKind.Relative)
                        };
                        Assert.Equal(((SolidColorBrush)palette["VcsBackground"]).Color, ((SolidColorBrush)window.Background).Color);
                        var titleBar = Descendants<Wpf.Ui.Controls.TitleBar>(window).Single();
                        Assert.Equal(((SolidColorBrush)window.Foreground).Color, ((SolidColorBrush)titleBar.Foreground).Color);
                        Assert.Equal(((SolidColorBrush)window.Foreground).Color, ((SolidColorBrush)titleBar.ButtonsForeground).Color);
                        Assert.Equal(((SolidColorBrush)palette["VcsAccent"]).Color,
                            ((SolidColorBrush)vigor.FindResource("VcsAccent")).Color);
                        foreach (var background in new[] { "VcsBackground", "VcsCard", "VcsInput", "VcsHover", "VcsSelection" })
                            foreach (var foreground in new[] { "VcsText", "VcsMuted", "VcsAccent" })
                                Assert.True(Contrast(((SolidColorBrush)palette[foreground]).Color,
                                    ((SolidColorBrush)palette[background]).Color) >= 4.5,
                                    $"{option.Name}: {foreground}/{background} deve ter contraste minimo de 4.5:1.");
                        var input = Find<TextBox>(window, "Nome do personagem");
                        Assert.Equal(((SolidColorBrush)palette["VcsInput"]).Color, ((SolidColorBrush)input.Background).Color);
                        Assert.Equal(((SolidColorBrush)palette["VcsText"]).Color, ((SolidColorBrush)input.Foreground).Color);
                        var inputBorder = Assert.IsType<Border>(input.Template.FindName("InputBorder", input));
                        Assert.Equal(new CornerRadius(7), inputBorder.CornerRadius);
                        Assert.Equal(30, input.MinHeight);
                        Assert.True(input.ActualHeight <= 34);
                        AssertReadableControls(window);
                        var loresheetView = Descendants<LoresheetCatalogView>(window).Single();
                        loresheetView.BringIntoView();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        AssertReadableControls(loresheetView);
                        SaveSnapshot(window, option.Id + "-loresheets");
                        var mainScroll = Find<ScrollViewer>(window, "Ficha completa");
                        mainScroll.ScrollToTop();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        SaveSnapshot(window, option.Id + "-identidade");
                        var skillsView = Descendants<SkillsView>(window).Single();
                        skillsView.BringIntoView();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        SaveSnapshot(window, option.Id + "-habilidades");
                        var skillDescription = Descendants<TextBlock>(skillsView).Single(x => Equals(x.ToolTip, model.Editor.SkillGroups[0].Items[0].Description));
                        Assert.False(string.IsNullOrEmpty((string)skillDescription.ToolTip));
                        Assert.DoesNotContain(Descendants<Expander>(skillsView), x => Equals(x.Header, "Descricao"));
                        var tooltip = new ToolTip { Content = skillDescription.ToolTip, PlacementTarget = skillDescription, IsOpen = true };
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Assert.True(Contrast(((SolidColorBrush)tooltip.Foreground).Color, ((SolidColorBrush)tooltip.Background).Color) >= 4.5);
                        tooltip.IsOpen = false;
                        var clanChoice = Find<ComboBox>(window, "Cla");
                        clanChoice.ApplyTemplate();
                        var clanInput = Assert.IsType<TextBox>(clanChoice.Template.FindName("PART_EditableTextBox", clanChoice));
                        Assert.True(Contrast(((SolidColorBrush)clanInput.Foreground).Color, ((SolidColorBrush)clanInput.Background).Color) >= 4.5);
                        clanChoice.IsDropDownOpen = true;
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        var clanRow = Assert.IsType<ComboBoxItem>(clanChoice.ItemContainerGenerator.ContainerFromIndex(0));
                        Assert.True(Contrast(((SolidColorBrush)clanRow.Foreground).Color, ((SolidColorBrush)clanRow.Background).Color) >= 4.5);
                        var clanPopup = Assert.IsType<Popup>(clanChoice.Template.FindName("PART_Popup", clanChoice));
                        AssertReadableControls(clanPopup.Child);
                        clanChoice.IsDropDownOpen = false;
                        model.Display.UseTabs = true;
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        var themedTabs = Find<TabControl>(window, "Secoes da ficha");
                        var tabPanel = Assert.IsType<System.Windows.Controls.Primitives.TabPanel>(themedTabs.Template.FindName("HeaderPanel", themedTabs));
                        var selectedTab = Assert.IsType<TabItem>(themedTabs.ItemContainerGenerator.ContainerFromItem(themedTabs.SelectedItem));
                        Assert.True(Contrast(((SolidColorBrush)selectedTab.Foreground).Color, ((SolidColorBrush)tabPanel.Background).Color) >= 4.5);
                        AssertReadableControls(window);
                        model.Display.UseTabs = false;
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        addSheet.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        AssertReadableControls(addSheet.ContextMenu);
                        addSheet.ContextMenu.IsOpen = false;
                        Assert.False(model.Editor.IsDirty);
                        var persisted = new DisplaySettingsViewModel();
                        new DisplaySettingsStore(Path.Combine(directory, "display.json")).Load(persisted);
                        Assert.Equal(option.Id, persisted.ThemeId);
                        // Selecting the current theme must not uncheck the only selected item.
                        settingsMenu.IsSubmenuOpen = true;
                        themeMenu.IsSubmenuOpen = true;
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        ((System.Windows.Automation.Provider.IInvokeProvider)peer).Invoke();
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Assert.True(item.IsChecked);
                    }
                    settingsMenu.IsSubmenuOpen = false;
                    window.Width = 1000;
                    window.Height = 680;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var scroll = Find<ScrollViewer>(window, "Ficha completa");
                    Assert.Equal(0, scroll.ScrollableWidth);
                    Assert.True(scroll.ScrollableHeight > 0);
                    scroll.ScrollToBottom();
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.True(scroll.VerticalOffset > 0);
                    var reopenedSettings = new DisplaySettingsViewModel();
                    new DisplaySettingsStore(Path.Combine(directory, "display.json")).Load(reopenedSettings);
                    Assert.False(reopenedSettings.Sections.Single(x => x.Key == "Attributes").IsVisible);
                    Assert.Equal("Ficha principal", reopenedSettings.Sections.Single(x => x.Key == "State").TabName);
                    Assert.Equal("Estado de sangue", reopenedSettings.Sections.Single(x => x.Key == "State").Caption);
                    Assert.Equal(1, reopenedSettings.Sections.Single(x => x.Key == "State").SectionOrder);
                    await model.BackCommand.ExecuteAsync(null);
                    Assert.True(model.IsHome);
                    Assert.Single(model.Characters);
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
                        x.DataContext is VCS.Application.Characters.CharacterSummary summary && summary.Id == originalId);
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
                    await model.CreateNpcCommand.ExecuteAsync(null);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.True(model.Editor!.IsNpc);
                    Assert.Equal(model.Editor.VisibleSections.Count, Descendants<NpcSheetView>(window).Count());
                    Assert.DoesNotContain(Descendants<DotTrack>(window), x => AutomationProperties.GetName(x) == "Fisicos do NPC");
                    model.Display.Sections.Single(x => x.Key == "Attributes").IsVisible = true;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Assert.Equal(27, model.Editor.SkillGroups.Sum(x => x.Items.Count));
                    Find<TextBox>(window, "Nome do NPC").Text = "NPC visual";
                    Find<DotTrack>(window, "Fisicos do NPC").SetCurrentValue(DotTrack.ValueProperty, 4);
                    var npcHealth = Find<BoxTrack>(window, "Vitalidade NPC");
                    var emptyBox = Descendants<Button>(npcHealth).First();
                    emptyBox.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(1, model.Editor.HealthSuperficial);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Descendants<Button>(npcHealth).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(1, model.Editor.HealthAggravated);
                    Assert.Equal(0, model.Editor.HealthSuperficial);
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    Descendants<Button>(npcHealth).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(0, model.Editor.HealthAggravated);
                    var npcWillpower = Find<BoxTrack>(window, "Vontade NPC");
                    Descendants<Button>(npcWillpower).First().RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.Equal(1, model.Editor.WillpowerAggravated);
                    Assert.Equal(0, model.Editor.WillpowerSuperficial);
                    model.Editor.NpcDisciplines[0].SelectedDiscipline = "Ofuscação";
                    model.Editor.NpcDisciplines[0].Rating = 2;
                    model.Editor.NpcMerits[0].Selected = model.Editor.NpcMerits[0].Options.First();
                    model.Editor.NpcMerits[0].Rating = 2;
                    await model.ExportPdfCommand.ExecuteAsync(null);
                    Assert.Equal("", model.Error);
                    Assert.True(File.Exists(dialogs.PdfPath));
                    Assert.Equal(4, (await service.GetAsync(model.Editor.Id)).Details.NpcPhysical);
                    model.Display.UseTabs = true;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    var npcTabs = Find<TabControl>(window, "Secoes da ficha");
                    Assert.Equal(model.Editor.VisibleTabs.Count, npcTabs.Items.Count);
                    foreach (var tab in model.Editor.VisibleTabs)
                    {
                        npcTabs.SelectedItem = tab;
                        await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                        Assert.Equal(tab.Sections.Count, Descendants<NpcSheetView>(window).Count());
                    }
                    model.Display.UseTabs = false;
                    var evolution = Descendants<Expander>(window).Single(x => Equals(x.Header, "Criacao e evolucao / XP"));
                    evolution.IsExpanded = true;
                    await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
                    AssertReadableControls(evolution);
                    model.Editor.TrackExistingCommand.Execute(null);
                    model.Editor.ExperienceTotal = 50;
                    model.Editor.SelectedAdvancement = model.Editor.AdvancementOptions.Single(x => x.Key == "NpcPhysical");
                    model.Editor.BuyLevelCommand.Execute(null);
                    Assert.Equal(25, model.Editor.ExperienceSpent);
                    Assert.Equal(5, model.Editor.NpcPhysical);
                    await WaitUntilAsync(() => !model.Editor.IsDirty);
                    model.Editor.SelectedLoresheetCollection = "Carna";
                    model.Editor.SelectedLoresheetBenefit = model.Editor.LoresheetBenefits.Single(x => x.Minimum == 1);
                    model.Editor.AddLoresheetCommand.Execute(null);
                    Assert.Contains("campanha insuficiente", model.Editor.Error);
                    model.Editor.CampaignXpAward = 3;
                    model.Editor.AwardCampaignXpCommand.Execute(null);
                    model.Editor.AddLoresheetCommand.Execute(null);
                    Assert.Equal(28, model.Editor.ExperienceSpent);
                    Assert.Equal(3, model.Editor.CampaignExperienceSpent);
                    Assert.Single(model.Editor.Loresheets.Items);
                    await WaitUntilAsync(() => !model.Editor.IsDirty);
                    Assert.Empty(bindings.Errors);
                    Assert.True(await model.TryCloseAsync());
                    var closed = false;
                    window.Closed += (_, _) => closed = true;
                    window.Close();
                    await WaitUntilAsync(() => closed);
                    window = null;
                    completion.SetResult();
                }
                catch (Exception exception) { completion.SetException(exception); }
                finally
                {
                    PresentationTraceSources.DataBindingSource.Listeners.Remove(bindings);
                    window?.Hide();
                    app.Shutdown();
                    if (Directory.Exists(directory)) Directory.Delete(directory, true);
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(60));
        thread.Join(TimeSpan.FromSeconds(5));
    }

    private static T Find<T>(DependencyObject root, string name) where T : DependencyObject
    {
        var values = Descendants<T>(root).ToArray();
        Assert.Contains(values, x => AutomationProperties.GetName(x) == name);
        return values.Single(x => AutomationProperties.GetName(x) == name);
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

    private static IEnumerable<T> Descendants<T>(DependencyObject root) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match) yield return match;
            foreach (var descendant in Descendants<T>(child)) yield return descendant;
        }
    }

    private sealed class BindingErrorListener : TraceListener
    {
        public List<string> Errors { get; } = [];
        public override void Write(string? message) { if (message is not null) Errors.Add(message); }
        public override void WriteLine(string? message) { if (message is not null) Errors.Add(message); }
    }

    private sealed class TestDialogs : IDesktopDialogs
    {
        public string PdfPath { get; init; } = "";
        public string? SavePdf() => PdfPath;
        public bool Confirm(string message) => false;
        public string? OpenJson() => null;
        public string? SaveJson() => null;
        public string? OpenBackup() => null;
        public string? SaveBackup() => null;
    }
}
