using Microsoft.Extensions.DependencyInjection;
using RunescapeTools.Application.Favourites;
using RunescapeTools.Application.Market;
using RunescapeTools.Application.MoneyMaking;
using RunescapeTools.Application.Profiles;
using RunescapeTools.Application.Training;
using RunescapeTools.Core.Favourites;
using RunescapeTools.Core.Market;
using RunescapeTools.Core.MoneyMaking;
using RunescapeTools.Core.Profiles;
using RunescapeTools.Core.Training;
using RunescapeTools.Infrastructure.Configuration;
using RunescapeTools.Infrastructure.Market;
using RunescapeTools.Infrastructure.Persistence;
using RunescapeTools.Infrastructure.Profiles;
using RunescapeTools.Infrastructure.Training;

namespace RunescapeTools.Infrastructure.DependencyInjection;

// Groups registrations for the application's shared services.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddRunescapeToolsServices(
        this IServiceCollection services,
        OsrsWikiOptions wikiOptions,
        FavouriteStoreOptions favouriteOptions,
        MarketDataOptions? marketOptions = null,
        OsrsHiscoreOptions? hiscoreOptions = null,
        TrainingPlanOptions? trainingPlanOptions = null,
        MoneyMakingPreferenceOptions? moneyMakingPreferenceOptions = null,
        ItemIconCacheOptions? itemIconCacheOptions = null,
        PriceHistoryStoreOptions? priceHistoryStoreOptions = null)
    {
        hiscoreOptions ??= new OsrsHiscoreOptions { UserAgent = wikiOptions.UserAgent };
        services.AddSingleton(wikiOptions);
        services.AddSingleton(hiscoreOptions);
        services.AddSingleton(favouriteOptions);
        services.AddSingleton(marketOptions ?? new MarketDataOptions());
        services.AddSingleton(TimeProvider.System);

        // Add HttpClient for OsrsWikiPriceClient and OsrsHiscoreClient
        //these clients will be used to fetch data from the OSRS Wiki and Hiscores respectively
        // Configure typed HTTP clients using the supplied options.
        services.AddHttpClient<IOsrsPriceClient, OsrsWikiPriceClient>(client =>
        {
            client.BaseAddress = wikiOptions.BaseAddress;
            client.Timeout = wikiOptions.Timeout;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(wikiOptions.UserAgent);
        });

        services.AddHttpClient<IHiscoreClient, OsrsHiscoreClient>(client =>
        {
            client.BaseAddress = hiscoreOptions.BaseAddress;
            client.Timeout = hiscoreOptions.Timeout;
            client.DefaultRequestHeaders.Accept.ParseAdd("text/plain");
            client.DefaultRequestHeaders.UserAgent.ParseAdd(hiscoreOptions.UserAgent);
        });

        if (itemIconCacheOptions is not null)
        {
            services.AddSingleton(itemIconCacheOptions);
            services.AddHttpClient<IItemIconService, WikiItemIconService>(client =>
            {
                client.BaseAddress = itemIconCacheOptions.WikiBaseAddress;
                client.Timeout = wikiOptions.Timeout;
                client.DefaultRequestHeaders.Accept.ParseAdd("image/*");
                client.DefaultRequestHeaders.UserAgent.ParseAdd(wikiOptions.UserAgent);
            });
        }

        // Register shared stores, application services, catalogues and calculators.
        services.AddSingleton<IFavouriteStore, JsonFavouriteStore>();
        services.AddSingleton<IMarketDataService, MarketDataService>();
        if (priceHistoryStoreOptions is not null)
        {
            services.AddSingleton(priceHistoryStoreOptions);
            services.AddSingleton<IPriceHistoryStore, JsonPriceHistoryStore>();
            services.AddSingleton<IPlannerPricingService, PlannerPricingService>();
        }
        services.AddSingleton<IFavouriteHistoryWarmupService, FavouriteHistoryWarmupService>();
        services.AddSingleton<HiscoreParser>();
        services.AddSingleton<MoneyMakingCalculator>();
        services.AddSingleton<IEhpCatalogue, MainEhpCatalogue>();
        services.AddSingleton<TrainingPlanCalculator>();
        services.AddSingleton<TrainingMoneyMakingCalculator>();

        // Register optional JSON stores when their configuration is supplied.
        if (trainingPlanOptions is not null)
        {
            services.AddSingleton(trainingPlanOptions);
            services.AddSingleton<ITrainingPlanStore, JsonTrainingPlanStore>();
        }

        if (moneyMakingPreferenceOptions is not null)
        {
            services.AddSingleton(moneyMakingPreferenceOptions);
            services.AddSingleton<IMoneyMakingPreferenceStore, JsonMoneyMakingPreferenceStore>();
        }
        // Discover concrete money-making methods in the Core assembly and register each one.
        var methodTypes = typeof(IMoneyMakingMethod).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract
                           && !type.IsInterface
                           && typeof(IMoneyMakingMethod).IsAssignableFrom(type));

        foreach (var methodType in methodTypes)
            services.AddSingleton(typeof(IMoneyMakingMethod), methodType);

        return services;
    }
}
