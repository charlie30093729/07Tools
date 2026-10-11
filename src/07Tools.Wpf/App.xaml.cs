using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RunescapeTools.Application.Favourites;
using RunescapeTools.Application.Market;
using RunescapeTools.Application.Profiles;
using RunescapeTools.Infrastructure.Configuration;
using RunescapeTools.Infrastructure.DependencyInjection;
using RunescapeTools.Infrastructure.Persistence;
using RunescapeTools.Wpf.Services;
using RunescapeTools.Wpf.ViewModels;

namespace RunescapeTools.Wpf;

// Coordinates application startup, dependency registration and shutdown.
public partial class App : System.Windows.Application
{
    private const string ProductDirectoryName = "07Tools";

    // Signals cancellation to operations that receive the application's shutdown token.
    private readonly CancellationTokenSource shutdown = new();

    // Holds the service provider and manages the registered services' lifetime.
    private IHost? host;

    // Tracks whether this process owns the application's single-instance mutex.
    private Mutex? singleInstanceMutex;
    private bool ownsMutex;

    // Starts the host, displays the main window and loads the initial page.
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Prevent a second application instance from running in this Windows session.
        singleInstanceMutex = new Mutex(true, @"Local\07Tools.Desktop", out ownsMutex);
        if (!ownsMutex)
        {
            Shutdown();
            return;
        }

        try
        {
            // Build the service provider, then start the host's managed services.
            host = BuildHost();
            await host.StartAsync(shutdown.Token);

            // Resolve MainWindow through DI, which also supplies its constructor dependencies.
            var window = host.Services.GetRequiredService<MainWindow>();
            MainWindow = window;
            window.Show();

            // Load the shell's initial page before starting the favourites history warmup.
            await host.Services.GetRequiredService<ShellViewModel>().InitializeAsync(shutdown.Token);

            // Start warming favourites history without waiting for it to finish here.
            _ = WarmFavouritesAsync(host.Services, shutdown.Token);
        }
        catch (Exception exception)
        {
            // Display startup failures and exit with a non-zero exit code.
            MessageBox.Show(
                $"07Tools could not start.\n\n{exception.Message}",
                "07Tools",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    // Requests cancellation and cleans up application-owned resources during exit.
    protected override async void OnExit(ExitEventArgs e)
    {
        shutdown.Cancel();

        if (host is not null)
        {
            // Request a graceful host shutdown with a five-second cancellation timeout.
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            try
            {
                await host.StopAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                // The process is already exiting; do not block on a slow service shutdown.
            }

            // Dispose the host and disposable services managed by its service provider.
            host.Dispose();
        }

        // Release the mutex only if this process acquired ownership.
        if (ownsMutex)
            singleInstanceMutex?.ReleaseMutex();

        singleInstanceMutex?.Dispose();
        shutdown.Dispose();

        base.OnExit(e);
    }

    // Defines the application's service registrations, configuration and logging.
    private static IHost BuildHost()
    {
        // Create Microsoft's host builder;
        var builder = Host.CreateApplicationBuilder();

        builder.Logging.ClearProviders();
        builder.Logging.AddDebug();

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var localData = Path.Combine(localAppData, ProductDirectoryName);

        // Register shared backend services using the API and storage options supplied below.
        builder.Services.AddRunescapeToolsServices(
            new OsrsWikiOptions(),
            new FavouriteStoreOptions
            {
                FilePath = Path.Combine(localData, "data", "favourites.json"),

                // Supply the bundled favourites snapshot for first-run seeding.
                SeedJson = ReadEmbeddedSeed()
            },
            new MarketDataOptions(),
            trainingPlanOptions: new TrainingPlanOptions
            {
                FilePath = Path.Combine(localData, "data", "training-plans.json")
            },
            moneyMakingPreferenceOptions: new MoneyMakingPreferenceOptions
            {
                FilePath = Path.Combine(localData, "data", "money-making-preferences.json")
            },
            itemIconCacheOptions: new ItemIconCacheOptions
            {
                DirectoryPath = Path.Combine(localData, "data", "item-icons")
            },
            priceHistoryStoreOptions: new PriceHistoryStoreOptions(
                Path.Combine(localData, "data", "price-history")));

        // Create and register the settings used to persist the selected profile.
        builder.Services.AddSingleton(new ProfilePreferenceOptions
        {
            FilePath = Path.Combine(localData, "data", "profile.json"),
            DefaultRsn = "bottleo"
        });

        // Register profile persistence and the shared currently loaded profile context.
        builder.Services.AddSingleton<IProfilePreferenceStore, JsonProfilePreferenceStore>();
        builder.Services.AddSingleton<ICurrentProfileContext, CurrentProfileContext>();

        // Register page ViewModels so each is created when first resolved and then reused.
        builder.Services.AddSingleton<ProfileViewModel>();
        builder.Services.AddSingleton<DashboardViewModel>();
        builder.Services.AddSingleton<FavouritesViewModel>();

        // Share the selected money-making method between Money Makers and XP Planner.
        builder.Services.AddSingleton<MoneyMakerSelectionContext>();
        builder.Services.AddSingleton<MoneyMakersViewModel>();

        // Register the WPF services responsible for opening training-related dialogs.
        builder.Services.AddSingleton<
            ITrainingConfigurationDialogService,
            TrainingConfigurationDialogService>();
        builder.Services.AddSingleton<
            ITrainingPriceDialogService,
            TrainingPriceDialogService>();

        // Register the remaining page, navigation shell and main application window.
        builder.Services.AddSingleton<XpPlannerViewModel>();
        builder.Services.AddSingleton<ShellViewModel>();
        builder.Services.AddSingleton<MainWindow>();

        // Build the host and service provider from the completed registrations.
        return builder.Build();
    }

    // Read the favourites JSON bundled inside the application's assembly.
    private static string ReadEmbeddedSeed()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("SeedFavourites.json")
                           ?? throw new InvalidOperationException("The first-run favourites snapshot is missing.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // Warm favourites history while handling cancellation and failures locally.
    private static async Task WarmFavouritesAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var logger = services.GetRequiredService<ILogger<App>>();
        try
        {
            await services.GetRequiredService<IFavouriteHistoryWarmupService>().WarmAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Cancellation during application shutdown is expected.
        }
        catch (Exception exception)
        {
            // Log warmup failures without closing the application.
            logger.LogWarning(exception, "Favourite history warmup did not complete.");
        }
    }
}