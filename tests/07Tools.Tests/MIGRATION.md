# NUnit migration map

Historical migration record, not a requirement to retain every original test or assertion.
The later [behavior-focused pruning audit](../../docs/TEST_SUITE_PRUNING.md) deliberately
removes redundant checks and scaffolding. Counts below describe the original migration.

Baseline: `61b0dc8` (`main` after Construction PR #62), formerly `Program.cs`.

All 69 registered legacy scenarios are represented by 101 discovered NUnit tests. Syntax-level comparison verified preservation of all 871 assertion call sites, including expected/actual expressions, messages, decimal tolerances, and derived-exception matching. These are call-site counts, not runtime assertion counts or a code-coverage percentage.

The initial one-to-one extraction passed 69/69 before the cross-skill scenarios were split. `LegacyScenario` properties provide traceability only; NUnit discovers tests through `[Test]`, not this document or a registration list.

| Original scenario | Assertion sites | NUnit destinations |
| --- | ---: | --- |
| `GenericFlowCalculation` | 7 | [GenericFlowCalculation](Core/MoneyMaking/MoneyMakingCalculatorTests.cs) |
| `TrainingResourceRequirements` | 3 | [TrainingResourceRequirements](Core/Training/TrainingPlanCalculatorTests.cs) |
| `VyrewatchMatchesLegacyFormula` | 5 | [VyrewatchMatchesLegacyFormula](Core/MoneyMaking/VyrewatchMethodTests.cs) |
| `VyrewatchNoRegenConfiguration` | 4 | [RegenConfigurationIncludesPotionsAndHigherKillRate](Core/MoneyMaking/VyrewatchMethodTests.cs) |
| `VyrewatchItemIdsAreDistinct` | 2 | [VyrewatchItemIdsAreDistinct](Core/MoneyMaking/VyrewatchMethodTests.cs) |
| `RuneDragonMethodDefinition` | 14 | [RuneDragonMethodDefinition](Core/MoneyMaking/RuneDragonMethodTests.cs) |
| `FrostDragonMethodDefinition` | 14 | [FrostDragonMethodDefinition](Core/MoneyMaking/FrostDragonMethodTests.cs) |
| `MidPriceFallback` | 2 | [MidPriceFallback](Core/Market/ItemPriceTests.cs) |
| `LatestPricesAreCached` | 4 | [LatestPricesAreCached](Application/Market/MarketDataServiceTests.cs) |
| `HistoryWindowsAreFilteredAndCached` | 6 | [HistoryWindowsAreFilteredAndCached](Application/Market/MarketDataServiceTests.cs) |
| `SearchOrdering` | 4 | [SearchOrdering](Application/Market/MarketDataServiceTests.cs) |
| `ItemIconCachePersistence` | 8 | [ItemIconCachePersistence](Infrastructure/Icons/ItemIconServiceTests.cs) |
| `FavouriteWarmup` | 2 | [FavouriteWarmup](Application/Favourites/FavouriteHistoryWarmupTests.cs) |
| `WikiClientRetries` | 2 | [WikiClientRetries](Infrastructure/Http/OsrsWikiPriceClientTests.cs) |
| `JsonStoreSeedsSortsAndDeduplicates` | 5 | [JsonStoreSeedsSortsAndDeduplicates](Infrastructure/Persistence/JsonFavouriteStoreTests.cs) |
| `JsonStoreDoesNotOverwrite` | 2 | Now covered by [ExistingArrayOrderLoadsWithoutSchemaMigrationOrOverwrite](Infrastructure/Persistence/JsonFavouriteStoreTests.cs) |
| `HiscoreParserMapsSkills` | 10 | [HiscoreParserMapsSkills](Infrastructure/Profiles/HiscoreParserTests.cs) |
| `ProfileSkillIconMapping` | 4 | [ProfileSkillIconMapping](Core/Profiles/ProfileSkillIconTests.cs) |
| `HiscoreParserRejectsInvalidResponses` | 2 | [HiscoreParserRejectsInvalidResponses](Infrastructure/Profiles/HiscoreParserTests.cs) |
| `HiscoreClientProtocol` | 2 | [HiscoreClientProtocol](Infrastructure/Http/HiscoreClientTests.cs) |
| `ProfilePreferencePersistence` | 4 | [ProfilePreferencePersistence](Infrastructure/Persistence/JsonProfilePreferenceStoreTests.cs) |
| `ProfileContextStateFlow` | 11 | [ProfileContextStateFlow](Application/Profiles/CurrentProfileContextTests.cs) |
| `DashboardViewModelStates` | 4 | [DashboardViewModelStates](Wpf/ViewModels/DashboardViewModelTests.cs) |
| `FavouritesViewModelFlow` | 5 | [FavouritesViewModelFlow](Wpf/ViewModels/FavouritesViewModelTests.cs) |
| `FavouritesItemIcons` | 4 | [FavouritesItemIcons](Wpf/ViewModels/FavouritesViewModelTests.cs) |
| `FavouritesChartZoomFlow` | 11 | [FavouritesChartZoomFlow](Wpf/ViewModels/FavouritesViewModelTests.cs) |
| `FavouritesChartVolume` | 6 | [FavouritesChartVolume](Wpf/ViewModels/FavouritesViewModelTests.cs) |
| `MoneyMakerViewModelFlow` | 42 | [MoneyMakerViewModelFlow](Wpf/ViewModels/MoneyMakersViewModelTests.cs) |
| `FrostDragonViewModelConfiguration` | 8 | [FrostDragonViewModelConfiguration](Wpf/ViewModels/MoneyMakersViewModelTests.cs) |
| `MoneyMakingPreferencePersistence` | 7 | [MoneyMakingPreferencePersistence](Infrastructure/Persistence/JsonMoneyMakingPreferenceStoreTests.cs) |
| `ProfileViewModelFlow` | 7 | [ProfileViewModelFlow](Wpf/ViewModels/ProfileViewModelTests.cs) |
| `EhpCatalogueCoverage` | 9 | [EhpCatalogueCoverage](Infrastructure/Training/Catalogue/CatalogueTests.cs) |
| `CatalogueMarketItemIntegrity` | 5 | [CatalogueMarketItemIntegrity](Infrastructure/Training/Catalogue/CatalogueTests.cs) |
| `TrainingMethodSelection` | 4 | [TrainingMethodSelection](Infrastructure/Training/Catalogue/CatalogueTests.cs) |
| `HerbiboarMethodCatalogue` | 17 | [HerbiboarMethodCatalogue](Infrastructure/Training/Skills/Hunter/HerbiboarTests.cs) |
| `AerialFishingMethodCatalogue` | 19 | [AerialFishingMethodCatalogue](Infrastructure/Training/Skills/Hunter/AerialFishingTests.cs) |
| `RedChinsAndKarambwans` | 18 | [ReviewedRatesAndEconomics](Infrastructure/Training/Skills/Cooking/OneTickKarambwansTests.cs)<br>[ReviewedRatesAndOutput](Infrastructure/Training/Skills/Hunter/RedChinchompasTests.cs) |
| `WoodcuttingAlternativeMethods` | 22 | [WoodcuttingAlternativeMethods](Infrastructure/Training/Skills/Woodcutting/WoodcuttingMethodTests.cs) |
| `FishingBarbarianMethods` | 10 | [FishingBarbarianMethods](Infrastructure/Training/Skills/Fishing/BarbarianFishingTests.cs) |
| `TrainingMethodAvailabilityLabels` | 12 | [TrainingMethodAvailabilityLabels](Wpf/ViewModels/XpPlannerRowViewModelTests.cs) |
| `XpPlannerRowMethodSelection` | 14 | [XpPlannerRowMethodSelection](Wpf/ViewModels/XpPlannerRowViewModelTests.cs) |
| `TrainingSkillConfiguration` | 26 | [PrayerMaterialLabelIsIndependentOfAltar](Wpf/ViewModels/XpPlannerRowViewModelTests.cs)<br>[ExpectedSkillsExposeConfigurators](Infrastructure/Training/Catalogue/CatalogueConfigurationTests.cs)<br>[CarpenterOutfitAdjustsRateAndMaterials](Infrastructure/Training/Skills/Construction/ConstructionConfigurationTests.cs)<br>[PyromancerAndBonfiresAdjustRatesAndConsumption](Infrastructure/Training/Skills/Firemaking/FiremakingConfigurationTests.cs)<br>[HoursCanBeExcluded](Infrastructure/Training/Skills/Fletching/FletchingConfigurationTests.cs)<br>[DisablingEquipmentRestoresBaseConsumption](Infrastructure/Training/Skills/Herblore/HerbloreConfigurationTests.cs)<br>[AltarChangesBoneConsumption](Infrastructure/Training/Skills/Prayer/PrayerConfigurationTests.cs) |
| `XpPlannerRowConfiguration` | 6 | [XpPlannerRowConfiguration](Wpf/ViewModels/XpPlannerRowViewModelTests.cs) |
| `DeterministicMethodCatalogue` | 30 | [BuyableMethodsHaveCompleteEconomicModels](Infrastructure/Training/Catalogue/CatalogueTests.cs)<br>[ReviewedRateAndItemFlows](Infrastructure/Training/Skills/Cooking/SummerPiesTests.cs)<br>[ReviewedRateAndItemFlows](Infrastructure/Training/Skills/Crafting/BlackDragonhideBodiesTests.cs)<br>[ReviewedRateAndItemFlows](Infrastructure/Training/Skills/Firemaking/RosewoodLogsTests.cs)<br>[ReviewedRateAndItemFlows](Infrastructure/Training/Skills/Fletching/AmethystDartsTests.cs)<br>[ReviewedRateAndItemFlows](Infrastructure/Training/Skills/Herblore/SaradominBrewsTests.cs)<br>[ReviewedRateAndItemFlows](Infrastructure/Training/Skills/Prayer/SuperiorDragonBonesTests.cs)<br>[ReviewedRateAndItemFlows](Infrastructure/Training/Skills/Smithing/BlastFurnaceGoldTests.cs) |
| `HerbloreEquipmentEconomics` | 3 | [HerbloreEquipmentEconomics](Infrastructure/Training/Skills/Herblore/HerbloreMethodTests.cs) |
| `HerbloreAlternativeMethods` | 20 | [HerbloreAlternativeMethods](Infrastructure/Training/Skills/Herblore/HerbloreMethodTests.cs) |
| `PracticalBuyableMethods` | 41 | [ReviewedUnlocksRatesAndEconomics](Infrastructure/Training/Skills/Construction/OakDungeonDoorsTests.cs)<br>[ReviewedUnlocksRatesAndEconomics](Infrastructure/Training/Skills/Crafting/AirBattlestavesTests.cs)<br>[ReviewedUnlocksRatesAndEconomics](Infrastructure/Training/Skills/Fletching/AdamantDartsTests.cs)<br>[ReviewedUnlocksRatesAndEconomics](Infrastructure/Training/Skills/Prayer/DragonBoneMethodsTests.cs)<br>[ReviewedUnlocksRatesAndEconomics](Infrastructure/Training/Skills/Smithing/AnvilMethodsTests.cs) |
| `RunecraftAlternativeMethods` | 65 | [RunecraftAlternativeMethods](Infrastructure/Training/Skills/Runecraft/RunecraftMethodTests.cs) |
| `OuraniaAltarZmiMethod` | 20 | [OuraniaAltarZmiMethod](Infrastructure/Training/Skills/Runecraft/OuraniaAltarTests.cs) |
| `RunecraftDaeyaltConfiguration` | 39 | [RunecraftDaeyaltConfiguration](Infrastructure/Training/Skills/Runecraft/DaeyaltConfigurationTests.cs) |
| `PhaseTwoMethodCatalogue` | 35 | [GatheringMethodsHaveCompleteEconomicModels](Infrastructure/Training/Catalogue/CatalogueTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Fishing/TwoTickSwordfishTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Hunter/BlackChinchompasTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Mining/GraniteTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Runecraft/SoloMudRunesTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Woodcutting/TeakTreesTests.cs) |
| `PhaseTwoTrainingCalculations` | 16 | [FullRoutePricingRetainsUnpricedEarlyLevels](Infrastructure/Training/Skills/Fishing/TwoTickSwordfishTests.cs)<br>[FullRoutePricingRetainsUnpricedEarlyLevels](Infrastructure/Training/Skills/Hunter/BlackChinchompasTests.cs)<br>[FullRoutePricingRetainsUnpricedEarlyLevels](Infrastructure/Training/Skills/Mining/GraniteTests.cs)<br>[FullRoutePricingRetainsUnpricedEarlyLevels](Infrastructure/Training/Skills/Runecraft/SoloMudRunesTests.cs)<br>[FullRoutePricingRetainsUnpricedEarlyLevels](Infrastructure/Training/Skills/Woodcutting/TeakTreesTests.cs) |
| `PhaseThreeMethodCatalogue` | 34 | [ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Agility/HallowedSepulchreTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Farming/TreeRunTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Sailing/GwenithGlideTests.cs)<br>[ReviewedRatesAndItemFlows](Infrastructure/Training/Skills/Thieving/GemKnightsTests.cs) |
| `PhaseThreeTrainingCalculations` | 14 | [FullRouteHoursAndGrandCoffinEconomics](Infrastructure/Training/Skills/Agility/HallowedSepulchreTests.cs)<br>[FullRouteAndPersonalRateEconomics](Infrastructure/Training/Skills/Sailing/GwenithGlideTests.cs)<br>[FullRouteHoursAndTokkulEconomics](Infrastructure/Training/Skills/Thieving/GemKnightsTests.cs) |
| `FarmingTrainingCalculations` | 17 | [FarmingTrainingCalculations](Infrastructure/Training/Skills/Farming/TreeRunTests.cs) |
| `CombatMethodCatalogue` | 32 | [CombatMethodCatalogue](Infrastructure/Training/Skills/Combat/CombatMethodTests.cs) |
| `CombatDependencyCalculations` | 18 | [CombatDependencyCalculations](Infrastructure/Training/Skills/Combat/CombatMethodTests.cs) |
| `ConstructionTrainingCalculation` | 13 | [ConstructionTrainingCalculation](Infrastructure/Training/Skills/Construction/ConstructionMethodTests.cs) |
| `TrainingRateOverride` | 2 | [TrainingRateOverride](Core/Training/TrainingPlanCalculatorTests.cs) |
| `ConfiguredTrainingRateOverrides` | 8 | [ConfiguredTrainingRateOverrides](Core/Training/TrainingPlanCalculatorTests.cs) |
| `HourlyTrainingEconomics` | 4 | [HourlyTrainingEconomics](Core/Training/TrainingPlanCalculatorTests.cs) |
| `TrainingMoneyMakerAllocation` | 4 | [TrainingMoneyMakerAllocation](Core/Training/TrainingMoneyMakingCalculatorTests.cs) |
| `TrainingPlanPersistence` | 6 | [TrainingPlanPersistence](Infrastructure/Persistence/JsonTrainingPlanStoreTests.cs) |
| `XpPlannerPriceDialog` | 23 | [XpPlannerPriceDialog](Wpf/Dialogs/TrainingPriceDialogViewModelTests.cs) |
| `XpPlannerPriceDialogIcons` | 3 | [XpPlannerPriceDialogIcons](Wpf/Dialogs/TrainingPriceDialogViewModelTests.cs) |
| `XpPlannerViewModelFlow` | 28 | [XpPlannerViewModelFlow](Wpf/ViewModels/XpPlannerViewModelTests.cs) |
| `XpPlannerPriceFailure` | 3 | [XpPlannerPriceFailure](Wpf/ViewModels/XpPlannerViewModelTests.cs) |
| `ShellNavigation` | 6 | [ShellNavigation](Wpf/ViewModels/ShellViewModelTests.cs) |
| `WpfViewsConstruct` | 14 | [WpfViewsConstruct](Wpf/Views/WpfViewSmokeTests.cs) |
