namespace RunescapeTools.Tests.Wpf.Views;

[TestFixture]
[Category("Wpf")]
[Apartment(ApartmentState.STA)]
[NonParallelizable]
public sealed class WpfViewSmokeTests
{
    [Test]
    [Property("LegacyScenario", "WpfViewsConstruct")]
    [Description("WPF profile, Favourites, Money Makers, and XP Planner views construct successfully")]
    public void WpfViewsConstruct()
    {
        RunescapeTools.Wpf.App? application = null;
        try
        {
            application = new RunescapeTools.Wpf.App();
            application.InitializeComponent();
            application.ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown;
            var safeImage = new RunescapeTools.Wpf.Controls.SafeImage
            {
                Source = new System.Windows.Media.Imaging.WriteableBitmap(
                    1,
                    1,
                    96,
                    96,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null)
            };
            Assert.That(safeImage.HasLoadedImage, Is.True, "valid image source hides its fallback");
            safeImage.Source = null;
            Assert.That(!safeImage.HasLoadedImage, Is.True, "cleared image source restores its fallback");
            var frozenImage = new System.Windows.Media.Imaging.WriteableBitmap(
                1,
                1,
                96,
                96,
                System.Windows.Media.PixelFormats.Bgra32,
                null);
            frozenImage.Freeze();
            safeImage.Source = frozenImage;
            Assert.That(safeImage.HasLoadedImage, Is.True, "frozen cached image is already loaded");
            safeImage.Source = null;
            Assert.That(!safeImage.HasLoadedImage, Is.True, "frozen cached image can be replaced safely");
            var market = new FakeMarketDataService
            {
                Latest = new Dictionary<int, ItemPrice>
                {
                    [24777] = Quote(24777, 500),
                    [24511] = Quote(24511, 900)
                }
            };
            var profileContext = new CurrentProfileContext(
                new FakeHiscoreClient(),
                new HiscoreParser(TimeProvider.System),
                new MemoryProfilePreferenceStore("bottleo"));
            var trainingStore = new MemoryTrainingPlanStore();
            var plans = new TrainingPlanPersistenceService(trainingStore);
            var viewModel = new XpPlannerViewModel(
                new MainEhpCatalogue(),
                new TrainingPlanCalculator(),
                new TrainingMoneyMakingCalculator(),
                market,
                plans,
                profileContext,
                new MoneyMakerSelectionContext());
            viewModel.LoadAsync().GetAwaiter().GetResult();
            var moneyViewModel = new MoneyMakersViewModel(
                [new VyrewatchMethod()],
                new MoneyMakingCalculator(),
                market,
                new MemoryMoneyMakingPreferenceStore(),
                new MoneyMakerSelectionContext());

            _ = new ProfileView();
            var plannerView = new XpPlannerView { DataContext = viewModel };
            var moneyView = new MoneyMakersView { DataContext = moneyViewModel };
            var favouritesViewModel = new FavouritesViewModel(
                new MemoryFavouriteStore(
                    new FavouriteItem(24777, "Blood shard", DateTimeOffset.UtcNow),
                    new FavouriteItem(24511, "Harmonised orb", DateTimeOffset.UtcNow)),
                market,
                new FakeItemIconService(),
                TimeProvider.System);
            favouritesViewModel.LoadAsync().GetAwaiter().GetResult();
            var favouritesView = new FavouritesView
            {
                DataContext = favouritesViewModel
            };
            var window = new System.Windows.Window
            {
                Content = plannerView,
                Width = 1280,
                Height = 800,
                ShowInTaskbar = false,
                WindowStyle = System.Windows.WindowStyle.None
            };
            window.Show();
            plannerView.UpdateLayout();
            var skillRowsScrollViewer = (System.Windows.Controls.ScrollViewer)plannerView.FindName(
                "SkillRowsScrollViewer");
            Assert.That((decimal)skillRowsScrollViewer.VerticalOffset, Is.EqualTo(0m).Within(0m), "XP Planner skill rows start at the top");
            var skillRows = (System.Windows.Controls.ItemsControl)plannerView.FindName("SkillRows");
            var skillIconBar = (System.Windows.Controls.ItemsControl)plannerView.FindName("SkillIconBar");
            Assert.That(ReferenceEquals(
                    RunescapeTools.Wpf.Controls.RightClickItemNavigation.GetTargetItemsControl(skillIconBar),
                    skillRows), Is.True, "XP Planner icon bar attaches right-click navigation to the skill rows");
            Assert.That(RunescapeTools.Wpf.Controls.RightClickItemNavigation.ScrollToItem(
                    skillRows,
                    viewModel.Rows.Single(row => row.Skill == "Construction")), Is.True, "XP Planner can resolve a skill row from its icon data context");
            plannerView.UpdateLayout();
            Assert.That(skillRowsScrollViewer.VerticalOffset > 0, Is.True, "right-click skill navigation moves the planner scroll position");
            var runecraftConfiguration = new MainEhpCatalogue().Skills
                .Single(skill => skill.Skill == "Runecraft")
                .Configurator!;
            var configurationDialog = new TrainingConfigurationDialog(
                new TrainingConfigurationDialogViewModel(
                    "Runecraft",
                    "Solo lava runes",
                    runecraftConfiguration.Definition,
                    runecraftConfiguration.Definition.Normalize().Values,
                    "solo-lava-runes"))
            {
                Owner = window
            };
            configurationDialog.Show();
            configurationDialog.UpdateLayout();
            configurationDialog.Close();
            var constructionRow = viewModel.Rows.Single(row => row.Skill == "Construction");
            var priceDialog = new TrainingPriceDialog(
                new TrainingPriceDialogViewModel(
                    constructionRow.Skill,
                    constructionRow.Result,
                    market.Latest))
            {
                Owner = window
            };
            priceDialog.Show();
            priceDialog.UpdateLayout();
            // Exercise rendering before closing, rather than leave its rendered callback queued.
            priceDialog.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            priceDialog.Close();
            window.Content = moneyView;
            moneyView.UpdateLayout();
            window.Content = favouritesView;
            favouritesView.UpdateLayout();
            var favouritesList = (System.Windows.Controls.ListBox)favouritesView.FindName("FavouritesList");
            Assert.That(favouritesList.AllowDrop, Is.True, "watch list accepts drag reordering");
            Assert.That(RunescapeTools.Wpf.Behaviors.ListReorderCommand.GetCommand(favouritesList),
                Is.SameAs(favouritesViewModel.ReorderFavouriteCommand), "drag behavior reaches the reorder command");
            var selectedFavouriteName = (System.Windows.Controls.TextBlock)favouritesView.FindName("SelectedFavouriteName");
            var selectedFavouriteItemNumber = (System.Windows.Controls.TextBlock)favouritesView.FindName("SelectedFavouriteItemNumber");
            Assert.That(favouritesViewModel.SelectedFavourite?.ItemId ?? 0, Is.EqualTo(24777), "first favourite starts selected");
            Assert.That(selectedFavouriteName.Text, Is.EqualTo("Blood shard"), "initial favourite header name");
            favouritesList.SelectedIndex = 1;
            Assert.That(favouritesViewModel.SelectedFavourite?.ItemId ?? 0, Is.EqualTo(24511), "list selection reaches the view-model");
            Assert.That(selectedFavouriteName.Text, Is.EqualTo("Harmonised orb"), "selected favourite header name follows the list");
            Assert.That(selectedFavouriteItemNumber.Text, Is.EqualTo("Item 24511"), "selected favourite header ID follows the list");
            Assert.That(favouritesViewModel.CurrentMidpoint, Is.EqualTo("900 gp"), "selected favourite quote follows the list");
            var selectedRow = favouritesViewModel.SelectedFavourite;
            var dragData = RunescapeTools.Wpf.Behaviors.ListReorderCommand.CreateDragData(favouritesList, selectedRow!);
            var targetRow = (System.Windows.Controls.ListBoxItem)favouritesList.ItemContainerGenerator.ContainerFromIndex(0);
            var dropPoint = targetRow.TranslatePoint(new System.Windows.Point(5, 1), favouritesList);
            var dragOver = RunescapeTools.Tests.TestSupport.Builders.WpfDragTestData.DragEvent(
                dragData, favouritesList, dropPoint, System.Windows.DragDrop.PreviewDragOverEvent);
            favouritesList.RaiseEvent(dragOver);
            Assert.That(dragOver.Effects, Is.EqualTo(System.Windows.DragDropEffects.Move), "same-list drag is accepted");
            var drop = RunescapeTools.Tests.TestSupport.Builders.WpfDragTestData.DragEvent(
                dragData, favouritesList, dropPoint, System.Windows.DragDrop.PreviewDropEvent);
            favouritesList.RaiseEvent(drop);
            Assert.That(drop.Effects, Is.EqualTo(System.Windows.DragDropEffects.Move), "drop reaches the move command");
            favouritesView.UpdateLayout();
            Assert.That(favouritesList.SelectedItem, Is.SameAs(selectedRow), "moving the selected row retains list selection");
            Assert.That(favouritesViewModel.SelectedFavourite, Is.SameAs(selectedRow), "moving a row retains the selected profile data");
            Assert.That(selectedFavouriteName.Text, Is.EqualTo("Harmonised orb"), "reordering keeps the selected header intact");
            Assert.That(favouritesList.Items[0], Is.SameAs(selectedRow), "saved order reaches the actual watch list");
            var foreignList = new System.Windows.Controls.ListBox();
            foreignList.Items.Add(selectedRow);
            var foreignDrop = RunescapeTools.Tests.TestSupport.Builders.WpfDragTestData.DragEvent(
                RunescapeTools.Wpf.Behaviors.ListReorderCommand.CreateDragData(foreignList, selectedRow!),
                favouritesList, new System.Windows.Point(5, favouritesList.ActualHeight - 2), System.Windows.DragDrop.PreviewDropEvent);
            favouritesList.RaiseEvent(foreignDrop);
            Assert.That(foreignDrop.Effects, Is.EqualTo(System.Windows.DragDropEffects.None), "foreign-list drops cannot mutate favourites");
            var appendDrop = RunescapeTools.Tests.TestSupport.Builders.WpfDragTestData.DragEvent(
                dragData, favouritesList, new System.Windows.Point(5, favouritesList.ActualHeight - 2), System.Windows.DragDrop.PreviewDropEvent);
            favouritesList.RaiseEvent(appendDrop);
            favouritesView.UpdateLayout();
            Assert.That(favouritesList.Items[1], Is.SameAs(selectedRow), "dropping below the last row appends it");
            Assert.That(favouritesList.SelectedItem, Is.SameAs(selectedRow), "append drop retains selection");
            window.Close();

            var shell = new ShellViewModel(new ProfileViewModel(profileContext),
                new DashboardViewModel(new MemoryFavouriteStore(), market, new FakeItemIconService(), []),
                favouritesViewModel, moneyViewModel, viewModel);
            VerifyMainWindowWaitsForPlannerSave(shell, viewModel, plans, trainingStore);
        }
        finally
        {
            application?.Shutdown();
        }
    }

    private static void VerifyMainWindowWaitsForPlannerSave(ShellViewModel shell,
        XpPlannerViewModel planner, ITrainingPlanPersistenceService plans, MemoryTrainingPlanStore store)
    {
        var persistence = new GatedPersistence(plans);
        var window = new RunescapeTools.Wpf.MainWindow(shell, persistence) { ShowInTaskbar = false };
        var row = planner.Rows.Single(value => value.Skill == "Construction");
        var editor = new System.Windows.Controls.TextBox();
        editor.SetBinding(System.Windows.Controls.TextBox.TextProperty, new System.Windows.Data.Binding(
            nameof(XpPlannerRowViewModel.TargetExperience))
        {
            Source = row,
            Mode = System.Windows.Data.BindingMode.TwoWay,
            UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.Explicit
        });
        window.Content = editor;
        var closed = false;
        window.Closed += (_, _) => closed = true;
        window.Show();
        window.UpdateLayout();
        Assert.That(editor.Focus(), Is.True);
        editor.Text = "150000000";
        window.Close();

        Assert.That(row.TargetExperience, Is.EqualTo(150_000_000), "closing commits the focused text edit");
        Assert.That(closed, Is.False, "the main window stays alive while saving");
        Assert.That(window.IsEnabled, Is.False, "edits are disabled while the closing snapshot is saved");
        window.Close();
        Assert.That(persistence.FlushCalls, Is.EqualTo(1), "repeated close requests share the active flush");
        persistence.Release.SetResult();

        var frame = new System.Windows.Threading.DispatcherFrame();
        var deadline = DateTime.UtcNow.AddSeconds(5);
        var timer = new System.Windows.Threading.DispatcherTimer(
            TimeSpan.FromMilliseconds(10), System.Windows.Threading.DispatcherPriority.Background,
            (_, _) => { if (closed || DateTime.UtcNow >= deadline) frame.Continue = false; }, window.Dispatcher);
        try
        {
            System.Windows.Threading.Dispatcher.PushFrame(frame);
        }
        finally
        {
            timer.Stop();
        }

        Assert.That(closed, Is.True, "saving completes before the final close is approved");
        var saved = store.GetAsync(planner.ProfileName).GetAwaiter().GetResult();
        Assert.That(saved["Construction"].TargetExperience, Is.EqualTo(150_000_000));
    }

    private sealed class GatedPersistence(ITrainingPlanPersistenceService inner) : ITrainingPlanPersistenceService
    {
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public int FlushCalls { get; private set; }
        public Task<IReadOnlyDictionary<string, TrainingSkillPreference>> GetAsync(
            string rsn, CancellationToken cancellationToken = default) => inner.GetAsync(rsn, cancellationToken);
        public void QueueSave(string rsn, IReadOnlyCollection<TrainingSkillPreference> preferences) =>
            inner.QueueSave(rsn, preferences);
        public async Task FlushAsync(CancellationToken cancellationToken = default)
        {
            FlushCalls++;
            await Release.Task.WaitAsync(cancellationToken);
            await inner.FlushAsync(cancellationToken);
        }
    }
}
