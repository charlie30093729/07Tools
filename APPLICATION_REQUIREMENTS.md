# 07Tools — Application Requirements

| Field | Value |
| --- | --- |
| Document version | 1.8 |
| Application | 07Tools |
| Status | WPF MVP baseline |
| Date | 10 October 2026 |
| Target platform | Windows 10 2004+ x64; .NET 8 WPF desktop application |

## 1. Purpose

This document defines the functional, data, integration, quality, and acceptance requirements for **07Tools**, an unofficial Old School RuneScape companion.

The application provides a focused personal workspace for Old School RuneScape Grand Exchange information. It intentionally limits general market noise by displaying favourite items and items required by registered money-making methods. It also supplies a reusable calculation model for GP per hour and future experience-per-hour or cost-per-hour features.

## 2. Product goals

The application shall:

1. Let a user maintain a small list of favourite Grand Exchange items.
2. Show current prices and selectable one-day, three-day, seven-day, and one-month history without operating a continuous data collector.
3. Calculate GP per hour from explicit item inputs and outputs using current market prices.
4. Make money-making methods modular so that adding or removing a method does not require navigation or calculator rewrites.
5. Keep application, infrastructure, persistence, and calculation logic independent from the desktop presentation layer.
6. Calculate level-aware XP goals, active hours, and reviewed GP/XP economics from a dated EHP catalogue.

## 3. Stakeholders and users

### 3.1 Primary user

The primary user is an Old School RuneScape player running the application locally to monitor selected items and estimate the profitability of personal money-making methods.

### 3.2 Maintainer

The maintainer adds or adjusts item mappings, money-making definitions, calculation rules, UI features, and external API compatibility.

### 3.3 External service owner

The OSRS Wiki real-time price API is an external dependency. The application must identify itself appropriately and avoid unnecessary requests.

## 4. Scope

### 4.1 MVP scope

- Native WPF executable as the only front end.
- Generic Host-based dependency injection, configuration, and logging.
- Profile, Dashboard, Favourites, Money Makers, and XP Planner navigation areas.
- JSON-backed favourite persistence.
- OSRS Wiki item mapping, latest-price, and time-series integration.
- Price-history graphs with one-day, three-day, seven-day, and one-month windows.
- Shared item-flow GP-per-hour calculator.
- Automatic discovery of concrete money-making method classes.
- Vyrewatch Sentinels, efficient Rune Dragons, and off-task AFK Frost Dragons as registered combat methods.
- Startup warming and in-memory API caching.
- Graceful handling of temporary market-data failures.
- Self-contained, single-file `win-x64` distribution.

### 4.2 Out of scope for the MVP

- User accounts, authentication, or role management.
- Cloud-hosted favourite synchronization.
- Automated trading or interaction with a RuneScape account.
- A continuously running historical-price ingestion service.
- Price alerts or push notifications.
- Editing money-making method definitions through the UI.
- Formal investment, trading, or profit guarantees.
- Full economic coverage for every EHP training band.
- Mobile-native applications.

## 5. Definitions and business terms

| Term | Definition |
| --- | --- |
| Favourite | A Grand Exchange item explicitly selected by the user for display and history retrieval. |
| Instant buy | The latest high price reported by the Wiki price API. |
| Instant sell | The latest low price reported by the Wiki price API. |
| Midpoint | The average of the latest high and low prices. If only one side is available, that value is used. |
| Item flow | An item quantity consumed as an input or produced as an output. |
| Per action | A quantity multiplied by the method's configured actions per hour. |
| Per hour | A quantity already expressed as an hourly amount. |
| GE tax | The configured percentage deducted from taxable output value. |
| History window | A discrete graph range of one day, three days, seven days, or one month; seven days is the default. |
| EHP rate band | A dated skill XP/hour rate beginning at a configured XP threshold and ending at the next band. |
| Economic coverage | The portion of remaining XP whose resource inputs, outputs, and fixed costs have been reviewed. |

## 6. Functional requirements

### 6.1 Application shell and navigation

| ID | Requirement |
| --- | --- |
| FR-NAV-001 | The application shall provide navigation to Profile, Dashboard, Favourites, Money Makers, and XP Planner views. |
| FR-NAV-002 | The active navigation destination shall be visually identifiable. |
| FR-NAV-003 | The WPF layout shall remain usable at the 1100 × 720 minimum window size, using contained scrolling where necessary. |
| FR-NAV-004 | The application shall identify the market-data source in the interface. |

### 6.2 Dashboard

| ID | Requirement |
| --- | --- |
| FR-DASH-001 | The dashboard shall display the number of favourite items. |
| FR-DASH-002 | The dashboard shall display the number of registered money-making methods. |
| FR-DASH-003 | The dashboard shall display the seven-day history-window summary. |
| FR-DASH-004 | The dashboard shall show the current midpoint for each favourite when live prices are available. |
| FR-DASH-005 | The dashboard shall provide direct navigation to favourite history and money-making calculators. |
| FR-DASH-006 | A live-price failure shall not prevent the user's stored favourites from being shown as safe. |

### 6.3 Favourite management

| ID | Requirement |
| --- | --- |
| FR-FAV-001 | The user shall be able to search Grand Exchange item mappings by a partial, case-insensitive item name. |
| FR-FAV-002 | Search shall begin only after at least two non-whitespace characters are supplied. |
| FR-FAV-003 | Search results shall prioritize names that begin with the search term. |
| FR-FAV-004 | Items already present in the favourite list shall not be offered as add candidates. |
| FR-FAV-005 | The user shall be able to add a search result to favourites. |
| FR-FAV-006 | Duplicate favourite item IDs shall not be persisted. |
| FR-FAV-007 | The user shall be able to remove a favourite independently from selecting it. |
| FR-FAV-008 | The favourite list shall preserve its saved user-defined order rather than sort by name. New favourites shall append at the bottom; removing a favourite shall preserve the relative order of the remaining items. Existing JSON arrays shall load in their stored order without schema migration or overwriting user data. |
| FR-FAV-009 | Adding an item shall select it and load its current price and graph history. |
| FR-FAV-010 | Removing the selected item shall select the first remaining favourite when one exists. |
| FR-FAV-011 | Favourite selection and removal controls shall have distinct accessible names and keyboard actions. |
| FR-FAV-012 | Persisted favourites shall display their cached OSRS Wiki inventory icon in the watch list and selected-item header when available; an unresolved or failed icon shall retain the monogram and all textual item data. Search-only candidates shall not trigger persistent icon downloads. |
| FR-FAV-013 | WPF users shall be able to drag a watch-list row's handle to a new position, with an insertion indicator and edge scrolling, or reorder the selected row with Alt+Up/Down. Moves shall save asynchronously and atomically, preserve item identities, selection, chart zoom and cached icons, and trigger no price/history/icon requests. A failed or cancelled save shall leave the previous order intact. Refresh, add/remove and reorder operations shall serialize to prevent lost updates. The saved order shall survive restarts and be used by Dashboard quick glance. |

### 6.4 Market prices and history

| ID | Requirement |
| --- | --- |
| FR-MKT-001 | The application shall retrieve the OSRS Wiki item mapping for item search and display metadata. |
| FR-MKT-002 | The application shall retrieve the latest high, low, and timestamp values for priced items. |
| FR-MKT-003 | The application shall calculate a midpoint from available high and low values. |
| FR-MKT-004 | Requests for current prices shall return only the item IDs required by the calling view or method. |
| FR-HIST-001 | The application shall request one-hour time-series data for one-day, three-day, and seven-day favourite views. |
| FR-HIST-002 | The application shall request six-hour time-series data for the one-month favourite view. |
| FR-HIST-003 | The history view shall display the weekly percentage change when at least two valid midpoint values exist. |
| FR-HIST-004 | The history view shall display current midpoint, instant-buy price, instant-sell price, point count, and tracked volume. |
| FR-HIST-004A | Each favourite chart-point tooltip shall display its local timestamp, midpoint price, and rolling 24-hour tracked volume split into high-side and low-side observations. The rolling window shall remain consistent across hourly and six-hour graph resolutions. |
| FR-HIST-005 | The application shall render a graph only when at least two valid midpoint points exist. |
| FR-HIST-006 | Graph points shall expose local timestamp and GP value details. |
| FR-HIST-007 | The application shall warm latest prices and weekly history for persisted favourites during startup. |
| FR-HIST-008 | A selected favourite shall default to the seven-day graph window. |
| FR-HIST-009 | Scrolling up over the graph shall zoom through seven days, three days, and one day; scrolling down shall zoom through one day, three days, seven days, and one month. |
| FR-HIST-010 | Graph zoom shall stop at the one-day and one-month boundaries. |

### 6.5 Profile dashboard and shared profile state

| ID | Requirement |
| --- | --- |
| FR-PRO-001 | `Profile` shall be the first desktop navigation item without replacing Dashboard as the startup page. |
| FR-PRO-002 | The profile page shall accept a trimmed RSN through its input, load button, or Enter key. |
| FR-PRO-003 | The first profile visit shall persist and load `bottleo` when no selected RSN exists. |
| FR-PRO-004 | Only a successfully fetched and parsed RSN shall replace the selected profile or its persisted preference. |
| FR-PRO-005 | The current profile shall be exposed through an injectable application-level `ICurrentProfileContext`. |
| FR-PRO-006 | Profile changes and same-RSN refreshes shall publish a `ProfileChanged` notification. |
| FR-PRO-007 | The desktop profile context shall have one application-wide singleton lifetime. |
| FR-PRO-008 | The profile page shall show Overall rank, total level, total experience, retrieval time, and all current skills. |
| FR-PRO-009 | Each skill shall show its API-defined name, level, experience, and rank in the shared canonical order. |
| FR-PRO-010 | Loading shall be asynchronous and cancellable, and duplicate requests shall be disabled while one is active. |
| FR-PRO-011 | Account-not-found, timeout, network, and malformed-response failures shall be distinguishable and shall retain valid state. |

### 6.6 Money-making calculations

| ID | Requirement |
| --- | --- |
| FR-CALC-001 | Every money-making method shall define a slug, name, description, actions per hour, account count, tax rate, and item flows. |
| FR-CALC-002 | Each item flow shall identify the item ID, display name, quantity, direction, quantity basis, and tax applicability. |
| FR-CALC-003 | A per-action quantity shall be multiplied by the configured actions per hour. |
| FR-CALC-004 | A per-hour quantity shall be used without action-rate multiplication. |
| FR-CALC-005 | Input cost shall equal the sum of hourly input quantity multiplied by midpoint price. |
| FR-CALC-006 | Gross revenue shall equal the sum of hourly output quantity multiplied by midpoint price. |
| FR-CALC-007 | GE tax shall apply only to taxable output value. |
| FR-CALC-008 | Profit per account shall equal gross revenue minus output tax and input cost. |
| FR-CALC-009 | Total profit shall equal profit per account multiplied by the user-selected account count, initialized from the method default. |
| FR-CALC-010 | A missing item price shall be visibly reported and shall contribute zero GP to the estimate. |
| FR-CALC-011 | The calculation view shall display a line-by-line ledger for every input and output. |
| FR-CALC-012 | The calculation model shall support optional experience rewards without requiring a GP formula rewrite. |
| FR-CALC-013 | Money Makers shall provide account-quantity increase and decrease controls, retain the chosen quantity per method for the current session, and prevent quantities below one. |
| FR-CALC-014 | Every current and future money-making method shall expose an editable positive actions/hour value initialized from its method definition. |
| FR-CALC-015 | An actions/hour override shall immediately reprice per-action item flows, experience rewards, per-account profit, all-account profit, and the shared rate supplied to the XP Planner. Per-hour item flows shall remain unchanged. |
| FR-CALC-016 | The user shall be able to reset actions/hour to the method's current configured default, removing the persisted override. Vyrewatch shall use 102 with prayer regeneration potions and 88 without them when no override is active. |
| FR-CALC-017 | Rune Dragons shall default to 45 kills per hour and shall scale its reviewed consumable inputs and expected tradeable outputs when the action-rate override changes. |
| FR-CALC-018 | Frost Dragons shall default to 120 off-task kills per hour using the reviewed full-Inquisitor AFK melee assumptions. Frost dragon bones shall be collected and priced by default; a persisted checkbox shall remove only their expected output when disabled. |

### 6.7 XP Planner

| ID | Requirement |
| --- | --- |
| FR-XP-001 | The XP Planner shall show the 21 planned skills in canonical Hiscores order; Attack, Strength, and Hitpoints shall remain available to the Profile Dashboard but shall be omitted from the XP Planner as zero-time skills. |
| FR-XP-002 | Start XP shall default to the successfully loaded profile, and every new per-profile goal shall default to 200,000,000 XP. |
| FR-XP-003 | Active hours shall be summed across every counted EHP rate band intersecting the start-to-goal range; zero-time methods shall retain their economics while contributing no active hours. |
| FR-XP-004 | The user shall be able to edit start XP, goal XP, and personal XP/hour and reset starts from the loaded profile. |
| FR-XP-005 | A personal rate override shall scale all route-band rates proportionally without changing resource quantities per XP. |
| FR-XP-006 | Training inputs shall use instant-buy/high prices and outputs shall use instant-sell/low prices, with visible fallback when only one quote side exists. |
| FR-XP-007 | Unknown or unreviewed economics shall be labelled as unpriced and shall not be silently included as zero GP. |
| FR-XP-008 | Summary totals shall show remaining XP, active hours, priced net GP, and economic coverage. |
| FR-XP-009 | Construction shall include reviewed oak- and mahogany-plank quantities plus Demon Butler fees for the EHP bands beginning at level 33. |
| FR-XP-010 | Woodcutting shall price Forester's rations and whole enhanced crystal teleport seeds for crystal-felling-axe charges while treating dropped teak logs as having no output value. |
| FR-XP-011 | Fishing shall price the reviewed all-skills crystal-harpoon charge allocation using whole enhanced crystal teleport seeds while treating dropped fish as having no output value. |
| FR-XP-012 | Mining shall price infernal-pickaxe recharges at one dragon pickaxe per 960,000 Mining XP while treating dropped granite as having no output value. |
| FR-XP-013 | Shooting-alt Hunter shall value one black chinchompa per 315 Hunter XP at the live low price after GE tax and disclose that PK losses and alt ammunition are excluded. |
| FR-XP-014 | Solo mud Runecraft shall price level-appropriate essence, earth runes, binding necklaces, Magic Imbue runes, pouch-repair runes, and taxed mud-rune output; level 99 shall omit pouch-repair runes. |
| FR-XP-015 | Hours shall represent active player time; the first release shall not claim calendar completion time for time-gated methods. |
| FR-XP-016 | The method subtitle shall identify counted methods as `gp/hr` and zero-time methods as `gp/xp`; negative values represent cost and positive values represent profit. |
| FR-XP-017 | Every skill definition shall expose a stable default method ID and support additional named method routes without changing the calculator contract. |
| FR-XP-018 | The reviewed Gwenith Glide projection shall use a rosewood hull at 240,000 Sailing XP/hour, exclude multiskilling, and live-price the conversion of generated crystal shards into divine super combat potions after GE tax. |
| FR-XP-019 | Gem Knights shall price only expected Tokkul output, using the official 182/195 chance at an average five Tokkul, an expected 1.8x yield from four rogue pieces, and conversion to live-priced uncut onyx at 260,000 Tokkul each. Gems, Rocky, variable consumables, banking time, and unlock requirements shall remain explicitly excluded. |
| FR-XP-020 | The reviewed Hallowed Sepulchre projection shall apply 98,500 Agility XP/hour across the selected range, loot only the Floor 5 Grand Hallowed Coffin, price its expected tradeable loot and coin output, consume one stamina potion(4) per two coffins, and exclude Hallowed marks and elite clues from GP. |
| FR-XP-021 | Defence shall default to Black Chinchompas & Cannon - Defensive at 405,000 XP/hour and price 1,500 black chinchompas plus 6,000 cannonballs per active hour. It shall also offer Red Chinchompas & Cannon - Defensive at the calibrated 330,000 XP/hour, pricing 1,500 red chinchompas plus 6,000 cannonballs per active hour. |
| FR-XP-022 | Ranged shall default to Black Chinchompas & Cannon at 1,150,000 XP/hour for supply conversion and GP/XP, while contributing zero active hours; the projection shall price 1,866 black chinchompas plus 6,000 cannonballs per calculation hour. It shall also offer a zero-time Red Chinchompas & Cannon route at the calibrated 940,000 XP/hour, pricing 1,866 red chinchompas plus 6,000 cannonballs per calculation hour. |
| FR-XP-023 | Slayer shall use the reviewed 123,040 XP/hour task-list projection and generate pending Magic XP at the centralized ratio derived from 163,136,972 Magic XP across 6,578 tasks averaging 28,397 Slayer XP. No other Slayer secondary XP shall be applied in this release. |
| FR-XP-024 | Pending Slayer Magic XP shall reduce only the residual Magic training required by the current plan and shall never replace or mutate the successfully loaded profile XP. |
| FR-XP-025 | Magic shall price residual Ice Barrage supplies after pending Slayer credit using 1,085 casts per calculation hour, two blood and four death runes per cast, and the reviewed 15 percent Kodai rune-saving assumption; it shall contribute zero active hours. |
| FR-XP-026 | Slayer supplies and loot shall use an explicit reviewed break-even assumption of 0 GP/XP while retaining active hours and pending Magic XP. |
| FR-XP-027 | Farming tree-run bands from level 38 onward shall live-price the highest unlocked regular, fruit, hardwood, and special-tree saplings plus their protection payments and gardener clearing fees. |
| FR-XP-028 | Farming economics shall assume one six-tree and six-fruit-tree run per day, normalize four hardwood patches and the redwood patch by growth time, complete calquat and celastrus daily once unlocked, and exclude unharvested fruit, bark, and logs from output value. Quest XP shall remain visibly unpriced. |
| FR-XP-029 | The fourth XP Planner summary card shall display the successfully priced method currently selected in Money Makers and navigate to Money Makers when clicked. |
| FR-XP-030 | The selected money-maker card shall provide a Reset action that clears the shared selection and deselects the corresponding Money Makers row without clearing skill allocations. |
| FR-XP-031 | The XP Planner shall provide a horizontal clickable bar of its OSRS skill icons under the instruction `Select skills to money make during`; selected icons shall be visually distinct. |
| FR-XP-032 | Money-maker income shall equal the selected method's combined GP/hour across the chosen account quantity multiplied only by the active planner hours of selected skills and shall be added to Priced Net GP. Zero-time skills shall add no money-making hours. |
| FR-XP-033 | Per-skill money-making allocations shall persist per RSN with training goals, starts, and personal rates. The selected method and its per-method account quantities shall remain session state and shall not be silently restored after application restart. |
| FR-XP-034 | Opening Money Makers without a shared selection shall not automatically select a default method; only a successfully priced explicit selection shall become available to the XP Planner. |
| FR-XP-035 | Every XP Planner skill row shall provide an explicit item-information action that opens a readable modal dialog instead of an item-price hover tooltip. The dialog shall display the route's calculated GP/XP, aggregate required inputs and expected outputs across every calculated rate band from effective start XP to goal XP, suggest input purchases at the same high price used by the calculator, suggest output sales at the same low price used by the calculator, show quote timestamps, identify fallback or unavailable prices, and disclose that recent completed trades do not guarantee offer execution. |
| FR-XP-036 | Every XP Planner skill row shall provide a dropdown containing each registered training route for that skill; selecting a route shall recalculate its active rate bands, hours, economics, generated XP, and live-price requirements. |
| FR-XP-037 | The selected training-route ID shall persist per RSN in the training plan and shall fall back safely to the skill's registered default when a saved route is unavailable. |
| FR-XP-038 | A per-skill reset shall preserve the currently selected training route, restore start XP from the loaded profile, restore the 200m goal, restore the selected route's catalogue XP rate and configuration defaults at that start XP, and clear that skill's money-making allocation. |
| FR-XP-039 | Herblore potion economics shall conditionally include the user-selected Prescription goggles secondary saving, Alchemist's amulet extra-dose output, and the Amulets of chemistry consumed to recharge it for eligible methods. Ineligible routes shall opt out explicitly, and all finished potions shall be valued as four-dose sale items. |
| FR-XP-040 | XP Planner item-recommendation rows shall resolve official OSRS Wiki inventory icons by item ID, display each available icon beside its matching item, and preserve the complete textual row when an icon is missing or cannot be loaded. |
| FR-XP-040 | Herblore shall provide selectable Saradomin brew, Super restore, and 1-tick extended super antifire routes. Super restores shall support both equipment effects; extended super antifires shall support Prescription goggles but not the Alchemist's amulet. |
| FR-XP-041 | Farming shall provide selectable magic + dragonfruit and magic + palm tree-run routes. The palm route shall retain palms after dragonfruit trees unlock, buy palm saplings and papaya protection, and scale its active XP rates by the resulting daily XP without changing the reviewed run-time assumptions. |
| FR-XP-042 | Configurable skill rows shall show a cog beside the method selector and use one schema-driven WPF dialog rather than skill-specific view logic. |
| FR-XP-043 | Skill-local `Global.cs` files shall own stable configuration keys, defaults, validation, method applicability, and calculation effects; mutable user selections shall not be stored in static catalogue state. |
| FR-XP-044 | Prayer shall expose Superior dragon bones, dragon bones, and Frost dragon bones, default to Superior dragon bones at a Gilded Altar, and support Chaos Altar, Sinister Offering at a bank, and Sinister Offering during Prifddinas Agility. Offering shall consume three bones, one blood rune, and one wrath rune per full cast, grant three times burial Prayer XP, and project 180 Magic XP. Bank defaults shall use 600 casts/hour. Prif defaults shall use 24 bones per lap, an estimated 82 seconds including banking, 1,340.6 Agility XP, and 0.94 expected crystal shards per lap. Shards shall be valued once through divine super combat potion(4) conversion at 2.5 potions per shard, buying inputs high and selling outputs low after GE tax. Projected secondary XP shall reduce remaining goals without modifying the loaded profile. Superior bones shall use a dragon-bone fallback below 70 Prayer for offering-spell routes. Requirements (92 Magic, A Kingdom Divided; Song of the Elves and 75 Agility for Prif; 97 Herblore for conversion), the high-level lap assumption, and excluded rune saving, robes, run-energy supplies, conversion time and conversion Herblore XP shall be disclosed rather than silently treated as verified user capabilities. See `docs/PRAYER_OFFERING.md`. |
| FR-XP-045 | Firemaking shall default to the full Pyromancer outfit, allow the outfit to be disabled, and allow supported logs to switch between bow burning and manual Forester's Campfire rates. |
| FR-XP-046 | Fletching shall allow its active hours to be excluded without removing its processing-time resource and economic calculations. |
| FR-XP-047 | Smithing shall persist a Smiths' uniform preference and apply the saved anvil-speed rate only to adamant platebodies and rune 2h swords, without changing their material quantities or applying it to Blast Furnace gold. Construction shall optionally apply the full Carpenter's outfit XP multiplier; Farming shall expose an empty configuration entry point for future options. |
| FR-XP-048 | Smithing shall provide selectable Solo Blast Furnace gold, adamant platebody, and rune 2h sword routes, preserve the existing lower-level route before each unlock, and price bars as inputs and finished equipment as taxed outputs. |
| FR-XP-049 | Construction shall provide an oak dungeon door route from level 74 using 10 oak planks for 600 base XP and a 25-plank Demon butler fee cycle. |
| FR-XP-050 | Crafting shall provide an air battlestaff route from level 66 using one battlestaff and one air orb per 137.5 XP and valuing one finished air battlestaff output. |
| FR-XP-051 | Fletching shall provide an adamant dart route from level 67 using one adamant dart tip and one feather per 15 XP and valuing one finished adamant dart output; its active-hour configuration shall apply to this route. |
| FR-XP-052 | Dragon-bone Prayer shall use the reviewed manual-offering rates and effective bone consumption for the selected altar; excluded Chaos Altar risk, supply, and unnoting costs shall remain disclosed rather than silently priced as zero. |
| FR-XP-053 | Runecraft shall provide selectable solo mud, solo lava, and solo aether routes, preserving the default solo-mud path until a selected alternative reaches its real unlock and pouch breakpoint. |
| FR-XP-054 | Runecraft shall provide a per-RSN full-Raiments toggle that increases applicable rune output by up to 60%, using the game's per-craft integer rounding, without changing XP/hour, essence consumption, or active hours. Aether bonus output shall consume matching additional aether catalysts. |
| FR-XP-055 | Solo aether economics shall buy pure essence, soul runes, aether catalysts, binding necklaces, Magic Imbue and pouch-repair runes where applicable, and rings of dueling, then value taxed aether-rune output. Reusable and untradeable teleport equipment shall remain disclosed exclusions. |
| FR-XP-056 | Hunter shall provide a selectable Herbiboar route from level 80 with level-banded rates based on 66 catches per hour. Economics shall use the reviewed 99-Herblore magic-secateurs output distribution, consume 0.125 stamina potion(4) per catch, live-price herbs and numulite, and disclose excluded fossils, pet value, reusable equipment, and lower-Herblore variance. |
| FR-XP-057 | Runecraft shall provide a selectable level-91 double-nature-rune route at 69,120 XP/hour using 64 pure essence across 120 Achievement Diary cape laps per hour. It shall apply the saved Raiments setting to nature-rune output with per-craft rounding, price one NPC Contact every eight laps, preserve the lower-level solo-mud route, and disclose excluded reusable and untradeable equipment. |
| FR-XP-058 | Hunter shall provide a selectable red-chinchompa route from level 63 using 265 XP and one tradeable red chinchompa per successful catch. Its default tick-manipulation rates shall scale by Hunter level to the reviewed 210,000 XP/hour at level 99, value output at the selected pricing mode's low price after GE tax, and disclose excluded supplies. Non-3-tick rates and configuration are defined by FR-XP-083; no shooting-alt option applies to red chinchompas. |
| FR-XP-059 | Cooking shall provide a selectable one-tick karambwan route from level 30 that buys raw karambwan at the live high price and values successfully cooked karambwan at the live low price after GE tax. Its lower-level bands shall account for burns using the reviewed 5,000-attempt baseline, while level 99 shall use 980,000 XP/hour with no burns and optimized banking. |
| FR-XP-060 | Runecraft shall provide a selectable Arceuus blood-rune route from level 77 at 36,000 XP/hour. It shall include the 0.625 Dark Altar binding XP per dark essence fragment, apply the saved Raiments setting to live-priced blood-rune output, preserve the lower-level solo-mud route, and disclose excluded blood essence, diary bonuses, passive secondary XP, ring charges, and reusable equipment. |
| FR-XP-061 | Runecraft shall provide a selectable Arceuus soul-rune route from level 90 at 44,000 XP/hour. It shall include the 0.625 Dark Altar binding XP per dark essence fragment, apply the saved Raiments setting to live-priced soul-rune output, preserve the lower-level solo-mud route, and disclose excluded diary bonuses, passive secondary XP, ring charges, and reusable equipment. |
| FR-XP-062 | Firemaking shall provide a selectable redwood-log route from level 90 using 350 base XP per log. Normal burning shall consume 1,485 logs/hour, the existing bonfire configuration shall switch both supported log routes to low-effort automatically fed Forester's Campfires at 665 logs/hour without changing base XP per log, and the saved Pyromancer setting shall apply its 2.5% XP bonus to the rate and goal quantity. |
| FR-XP-063 | At 13,034,431 Runecraft XP, every pouch-based route shall automatically stop pricing NPC Contact pouch repairs because the equipped Runecraft cape prevents pouch degradation. Astral runes consumed independently by Magic Imbue shall remain priced. Plans crossing level 99 shall retain repair costs only for the pre-99 segment. |
| FR-XP-064 | Runecraft shall provide a per-RSN Daeyalt-essence toggle, disabled by default, and an optional non-negative whole-number stock field whose blank value means unlimited stock. Eligible pure-essence segments shall consume Daeyalt first, apply its 50% Runecraft XP bonus without changing rune output per essence, and automatically fall back to pure essence when finite stock is exhausted. Daeyalt shall be displayed as already-owned untradeable stock with no GE value; its mining time shall be excluded, and dark-essence blood and soul segments shall remain unaffected. |
| FR-XP-065 | An editable personal XP/hour value shall be accepted and persisted as the player's base rate. The XP/hour field shall then display the active configured result. Rate-affecting skill configurations shall apply their documented multipliers on top of the stored base for eligible bands only, including Daeyalt essence, Pyromancer's outfit, Carpenter's outfit, and Smiths' uniform. Finite configuration resources shall stop contributing their multiplier when exhausted, while method/action-rate choices and economics-only configurations shall not be double-applied. |
| FR-XP-066 | Runecraft shall provide a selectable Ourania Altar (ZMI) route with the documented level and pouch rate bands from level 1 through 99. It shall live-price pure or configured Daeyalt essence, Eniola mind-rune payments, Ourania Teleport and pre-99 pouch-repair runes, and every level-dependent rune output. Full Raiments and an enabled-by-default Ardougne medium diary setting shall change rune yield without changing XP/hour; the Runecraft cape shall remove pouch-repair costs at level 99, and finite Daeyalt stock shall fall back to pure essence. |
| FR-XP-067 | Runecraft shall provide an enabled-by-default per-RSN toggle for an Abyssal lantern lit with magic logs. When enabled, applicable routes shall add 10% of their base rune output without changing XP/hour, essence consumption, or active hours. The bonus shall stack additively with Raiments, apply to every priced rune output including ZMI and dark-essence routes, and consume matching additional catalysts for aether runes. |
| FR-XP-068 | Woodcutting shall provide selectable crystal-felling-axe redwood and ironwood routes at their level 90 and level 80 unlocks. Redwood shall scale from 77,000 to 82,500 XP/hour and ironwood from approximately 82,500 to 104,500 XP/hour by level 99. Both routes shall consume one Forester's ration per successful chop, apply 110% of base XP, value the 80% expected log yield after the felling effect, and price crystal charges only when a log is received. |
| FR-XP-069 | Fishing shall provide selectable three-tick and five-tick Barbarian Fishing routes from level 48, using calibrated level bands of 45,000-115,000 and 25,000-60,000 Fishing XP/hour respectively. Only XP earned in eligible Barbarian bands shall generate level-banded passive Agility XP credit. Fish shall be treated as dropped, Strength XP shall remain disclosed but excluded from the planner, and unreviewed bait and tick-manipulation supplies shall remain visibly unpriced. |
| FR-XP-070 | Every XP Planner method option shall retain a distinct permanent route name regardless of the active fallback rate band. A future route shall remain selectable for long-term planning, show its primary-skill unlock level while locked, and identify the fallback method currently supplying its XP/hour and economic calculation until that unlock is reached. |
| FR-XP-071 | Hunter shall provide a calibrated Aerial Fishing route at level 99 Hunter and Fishing while disclosing the activity's underlying level 35 Hunter and level 43 Fishing access requirements. The reviewed route shall earn 129,000 Hunter and 99,500 Fishing XP/hour, apply the Fishing XP as a proportional linked credit without duplicating active hours, and buy 2.5 super hunter potion(4), 2.5 super fishing potion(4), and 2 prayer potion(4) per hour. The route shall document Alry the Angler's whole-fish toggle, require initial bait or an eligible fish to bootstrap the session, and model no recurring bait cost because the cormorant can consume caught fish when no appropriate bait remains. The default Hunter route shall remain active below level 99 because lower-level boosted rates are unreviewed and shall generate no Fishing credit. Cooking XP, Molch pearls, golden tench, and untradeable rewards shall remain excluded. |
| FR-XP-072 | Prayer shall provide selectable Frost dragon bones using the shared offering-location configuration. A two-burner Gilded Altar shall award 350 XP per bone at 2,550 offerings and 892,500 XP/hour. The Chaos Altar shall award the same 350 XP per offering at 2,000 offerings and 700,000 XP/hour while applying its 50% save chance as 700 effective XP per consumed bone. Both routes shall buy item 31729 at the live high price; Chaos Altar deaths, supplies, and unnoting fees shall remain excluded. |
| FR-XP-073 | Right-clicking a skill icon in the XP Planner allocation bar shall vertically scroll the training table to that skill without changing its money-making allocation or horizontal scroll position. Left-click shall retain its existing allocation-toggle behavior, and the icon shall expose discoverable tooltip and accessibility guidance for both actions. |
| FR-XP-074 | Runecraft shall provide a selectable 138,000 XP/hour 1+1 dolo Aether route from level 90. Its tracked item quantities, GP/XP, output configuration, and pre-99 pouch-repair behavior shall match Solo Aether, while all additional runner-supplied essence and operating costs remain excluded. GP/hour shall therefore scale from Solo Aether solely through the higher XP/hour rate. Configured Daeyalt shall replace the main account's tracked essence and apply its 50% bonus only to the main account's share of the combined rate, producing approximately 177,881 XP/hour while active, with the existing finite-stock fallback behavior. |
| FR-XP-075 | Construction shall provide a selectable mahogany table route from level 52 at 940,000 XP/hour, consuming one mahogany plank per 140 XP and applying the 24-plank Demon butler fee cycle. |

| FR-XP-076 | The planner shall offer a saved global Live/30-day-average pricing selection that reprices all skill methods and the allocated money maker without changing XP, hours, quantities or personal rate/configuration settings. Historical buy and sell prices shall be independently volume-weighted over 120 completed UTC six-hour buckets, retaining decimal GP precision and requiring at least 116 distinct valid bucket timestamps. Missing prices or volumes shall not silently become zero, opposite-side averages, or live-price fallbacks. |
| FR-XP-077 | Historical data and the pricing preference shall persist under `%LocalAppData%\07Tools\data\price-history`, with deduplicated item discovery, at most four simultaneous history requests, atomic cache replacement and refresh-on-request at the next six-hour boundary. Fresh cached history shall survive restart. In-flight requests shall support cancellation; a failed or superseded refresh shall not replace the last valid snapshot. Historical item details shall be clearly labeled as estimates rather than current offers. |
| FR-XP-078 | Allocated money-maker income shall be recalculated from its effective item flows, actions/hour, account count, configuration and tax using the same pricing snapshot as the skill methods. The planner shall buy inputs high and sell outputs low in either mode, without changing the standalone Money Makers tab's existing live midpoint calculation. Incompletely priced money makers shall be excluded from planner income with a visible explanation. |

| FR-XP-079 | Sailing shall provide a stable-labelled Salvaging route with ordered wreck/equipment rate bands and a level-15 unlock disclosure. Its persisted, default-on crystal-extractor toggle shall add 250 XP per 63 seconds from level 73, also on personal base rates, without changing Gwenith Glide. The method shall disclose equipment assumptions, uncertain rate estimates, the retained pre-15 legacy fallback, and unpriced loot. Rate calculations and configuration transformations shall remain shared, with no new calculation logic in XAML or view models. |

### 6.8 Method modularity

XP Planner addition FR-XP-084: Thieving shall offer separately selectable Gem knights and Ardougne knights, keeping Gem knights as the default with the existing `main-ehp` ID and unchanged economics. Ardougne knights shall use the stable `ardougne-knights` ID and label, disclose a level-55 unlock, preserve the existing route below that threshold, and use ordered diary-assisted rate bands at levels 55/60/65/70/75/80/85/90/95. At 95+ the efficient preset shall be 252,900 XP/hour. Full Rogue equipment shall yield 100 fixed coins per 84.3 Thieving XP, independent of market pricing and without GE tax. Personal rates shall affect hours and hourly income but retain per-goal coins. Selection and personal rates shall use existing per-profile persistence and reset behavior. Medium-diary/full-outfit assumptions, excluded food and optional supplies, the theoretical high-level ceiling and the legacy pre-unlock route shall be documented. Method definitions and item data shall reside in their respective Thieving/Methods classes.

XP Planner addition FR-XP-083: Hunter shall use stable Red chinchompas and Black chinchompas labels with a saved, default-on 3-tick toggle and a default-on black-only shooting-alt toggle. Disabling 3-ticking shall immediately disable and clear the shooting-alt option; shared normalization shall enforce this dependency for saved state. Re-enabling 3-ticking shall not silently re-enable the alt. Existing method IDs, default enabled rates and other Hunter methods shall remain compatible. Level-99 red rates shall be 210k/170k with/without 3-ticking; black rates shall be 265k with 3-ticking/alt, 225k with 3-ticking solo and a user-approved 200k planning estimate without 3-ticking. Personal rates shall scale with configuration throughput ratios without changing catch XP, per-goal resources or GP. Per-skill reset shall restore enabled defaults. Rate assumptions, lower-level limitations and excluded supplies shall be documented.

XP Planner addition FR-XP-082: Agility shall provide separately selectable Prifddinas and Ardougne courses with stable method IDs/names and level-75/90 unlock disclosure. Prif shall use ordered level-75/80/85/90 rate bands of 54k/58k/62k/66k XP/hour and value expected shards through divine super combat potion conversion. Ardougne shall use 70k XP/hour and an explicitly no-elite-diary estimate of 18 marks/hour converted to amylase. Personal rates shall preserve per-XP Prif rewards and scale time-based Ardougne rewards with hours. Both methods shall use existing persistence, price modes, missing-price handling and item dialogs. Prayer-at-Prif rates, quantities, costs and generated XP shall remain unchanged; its existing Agility credit shall reduce only standalone remaining XP, hours and rewards. Completing the goal through credit shall leave no standalone hours/rewards. The default Sepulchre method and legacy pre-unlock fallback shall remain unchanged and disclosed, not presented as a verified low-level route.

XP Planner addition FR-XP-080: Mining shall offer a saved Calcified rocks/crystal-pickaxe route from level 71, preserving the default granite and pre-unlock routes. Non-tick-manipulation base rate bands shall use 34k/39k/44k/49k XP/hour at levels 71/80/90/99. A persisted default-on full Prospector option shall apply a 2.5% XP bonus to this route and personal base rates, with corresponding reductions in per-XP resources; it shall not modify the existing granite route. The route shall estimate crystal recharge consumption from main-resource successes, price enhanced teleport seeds using the current pricing mode, and show bone shards/deposits as untradeable outputs. It shall not award Prayer XP or assume deposit processing. Custom rates shall change hours without changing per-goal resource quantities. Setup and excluded costs/rewards shall be documented.

XP Planner addition FR-XP-081: Prayer shall offer Infernal ashes (25778) and Abyssal ashes (25775) exclusively through Demonic Offering at a bank or Prif agility. Explicit ash-method selection and reset shall default to Prif; a saved valid Bank choice shall survive refresh and restart. Ashes shall grant 330/255 Prayer XP per item and 175 Magic XP per three-item cast. Rates shall reuse the existing bank and Prif throughput assumptions, with Prif's Agility/shard projections. At the user's request, spell costs shall intentionally reuse one blood and one wrath rune per cast, clearly disclosed as an approximation rather than Demonic Offering's real recipe. Method-specific location validation shall be shared, preserve bone behavior, and reject/normalize altar choices for ashes.

| ID | Requirement |
| --- | --- |
| FR-MOD-001 | A money-making method shall implement the `IMoneyMakingMethod` contract in the Core project. |
| FR-MOD-002 | Concrete method implementations shall be discovered and registered automatically at application startup. |
| FR-MOD-003 | Adding or removing a method class shall automatically update the available method list after rebuild and restart. |
| FR-MOD-004 | Domain calculations shall not depend on WPF or infrastructure-specific API types. |

### 6.9 Persistence and error handling

| ID | Requirement |
| --- | --- |
| FR-DATA-001 | Favourites shall persist locally in a JSON file under the configured data directory. |
| FR-DATA-002 | Favourite writes shall replace the target file atomically through a temporary file. |
| FR-DATA-003 | Concurrent favourite operations in one application process shall be serialized. |
| FR-DATA-004 | The desktop host shall allow only one running application instance per Windows user session. |
| FR-DATA-005 | On first launch only, the desktop host shall seed favourites from its embedded snapshot when no desktop file exists. |
| FR-DATA-006 | The first-run seed shall never replace an existing desktop favourites file. |
| FR-DATA-007 | The desktop host shall read the current `07Tools` data directory directly without importing data from previous product directories. |
| FR-DATA-008 | The selected RSN shall persist atomically in `%LocalAppData%\07Tools\data\profile.json`. |
| FR-DATA-009 | XP goals, overrides, selected training-route IDs, normalized skill-configuration values, and money-making skill allocations shall persist atomically per normalized RSN in `%LocalAppData%\07Tools\data\training-plans.json`. Existing records without configuration values shall load the current skill defaults. |
| FR-DATA-010 | Positive money-maker actions/hour overrides and method-specific boolean options shall persist atomically by stable method slug in `%LocalAppData%\07Tools\data\money-making-preferences.json` and shall apply across application restarts. |
| FR-DATA-011 | Wiki item icons requested by a desktop presentation shall be downloaded lazily and persisted under `%LocalAppData%\07Tools\data\item-icons`; the cache key shall change when the mapped Wiki filename changes. |
| FR-DATA-012 | XP Planner saves shall capture the loaded profile's RSN and a defensive snapshot of its preferences when an edit is queued. Pending edits shall remain separate per RSN, survive profile changes, and be removed only after a successful serialized write. Returning to a profile shall restore its pending edits. Existing JSON schema and settings shall remain compatible. |
| FR-DATA-013 | Graceful main-window closing shall commit the active text edit and wait for pending XP Planner writes before allowing the window to close. If saving fails or exceeds five seconds, the application shall remain open, retain pending edits, and display a retry warning. Forced process termination and operating-system shutdown are not guaranteed flush paths. |
| FR-ERR-001 | Item search, latest-price, history, and calculator failures shall produce user-readable messages. |
| FR-ERR-002 | Temporary API failures shall not delete or overwrite stored favourites. |
| FR-ERR-003 | HTTP 429 and server-error responses shall be retried up to three attempts with a delay. |
| FR-ERR-004 | Application shutdown cancellation shall not be logged as a startup-warming failure. |

## 7. Business rules

| ID | Rule |
| --- | --- |
| BR-001 | Midpoint = `(high + low) / 2` when both values exist. |
| BR-002 | When only one market side exists, midpoint equals the available value. |
| BR-003 | When neither market side exists, the item is treated as missing a price. |
| BR-004 | Weekly change = `(last midpoint - first midpoint) / first midpoint × 100`; it is omitted when fewer than two values exist or the first value is zero. |
| BR-005 | Tracked volume is the sum of high-side and low-side volume across the latest seven-day hourly history, independent of the visible graph window. |
| BR-006 | The MVP Vyrewatch method defaults to 102 actions per hour with prayer regeneration potions or 88 without them, uses five accounts, and applies a 2% output tax; a positive persisted user override supersedes either action-rate default until reset. |
| BR-007 | The MVP prices calculations using the current high/low midpoint, so results are estimates rather than guaranteed realized profit. |
| BR-008 | Rune Dragons assume the efficient low-intensity Justiciar, dragon hunter lance, dragonfire shield, Piety, Protect from Magic, mounted Digsite pendant route, and Monkey Madness II drop access. The variable rare-drop-table value is excluded. |
| BR-009 | Frost Dragons assume an off-task full-Inquisitor AFK melee setup with dragon hunter lance on crush, Avernic defender, Protect from Melee, Piety, extended super antifire, 120 kills/hour, and no rare- or gem-drop-table value. Frost dragon bones are optional ledger output and default to collected. |

## 8. External interface requirements

### 8.1 OSRS Wiki real-time price API

The application depends on these logical operations under the OSRS endpoint:

| Operation | Purpose |
| --- | --- |
| `mapping` | Searchable item IDs and metadata. |
| `latest` | Current high, low, and update timestamps. |
| `timeseries?id={itemId}&timestep=1h` | Hourly price and volume history for one-day, three-day, and seven-day views. |
| `timeseries?id={itemId}&timestep=6h` | Six-hour price and volume history for the one-month view. |

Requirements:

- Requests shall accept JSON responses.
- Requests shall include an identifiable `User-Agent` configured through application settings.
- The HTTP client timeout shall be finite; the MVP target is 20 seconds.
- API schema changes shall be handled as integration failures rather than silently producing incorrect calculations.
- The application shall not require Wiki credentials or store third-party secrets.

### 8.2 Old School Hiscores API

- Normal accounts shall use `https://secure.runescape.com/m=hiscore_oldschool/index_lite.ws?player=X`.
- The RSN query value shall be URL-encoded.
- The response shall be parsed as headerless CSV with `rank,level,experience` for Overall and skill rows.
- The canonical shared mapping shall preserve the documented API order: Overall, Attack, Defence, Strength, Hitpoints, Ranged, Prayer, Magic, Cooking, Woodcutting, Fletching, Fishing, Firemaking, Crafting, Smithing, Mining, Herblore, Agility, Thieving, Slayer, Farming, Runecraft, Hunter, Construction, and Sailing.
- Activity rows following the complete skill block shall not be misinterpreted as skills.
- Missing, empty, incomplete, or malformed skill rows shall fail parsing rather than produce zero-level data.
- HTTP 404 shall be treated as account not found; other HTTP, timeout, and parsing failures shall remain integration errors.
- Requests shall use a finite timeout and the existing identifiable `07Tools/0.1 (contact: Discord bottleo)` User-Agent.

### 8.3 Desktop interface

- The active interface shall be a native WPF application targeting `net8.0-windows10.0.19041.0`.
- The default window shall be approximately 1280 × 800 with a 1100 × 720 minimum.
- Feature behavior shall reside in CommunityToolkit.Mvvm view-models; code-behind shall be limited to initialization and application lifetime.
- Favourite history shall be rendered with LiveCharts2 WPF, with discrete mouse-wheel windows and tooltip timestamps converted to local time.
- The solution shall contain Core, Application, Infrastructure, Wpf, and Tests; WPF shall be the sole application host.

## 9. Data requirements

### 9.1 Favourite item

| Field | Type | Constraint |
| --- | --- | --- |
| Item ID | Integer | Positive and unique within favourites. |
| Name | String | Required display name. |
| Added at | Date/time with offset | Stored in UTC when added by the application. |

### 9.2 Market data

- Item mappings contain item ID, name, examine text, membership flag, optional buy limit, and icon name.
- Latest prices contain optional high and low values and their optional update times.
- History points contain timestamp, optional average high and low, and high/low volumes.

### 9.3 Money-making method data

- Method slugs shall be stable and unique.
- Required item IDs shall be derived from distinct item flows.
- Monetary calculation values shall use decimal arithmetic after API integer prices are read.
- Experience rewards shall identify the skill and experience per action.

### 9.4 Local and generated data

- Mutable desktop state shall be stored under `%LocalAppData%\07Tools`.
- Desktop favourites shall be stored at `%LocalAppData%\07Tools\data\favourites.json`.
- Per-RSN training plans shall be stored at `%LocalAppData%\07Tools\data\training-plans.json`.
- The EHP catalogue shall identify its source snapshot and verification date; rate changes shall be reviewed code/data changes rather than runtime scraping.
- The desktop host shall read existing `%LocalAppData%\07Tools\data` directly. Automatic imports from previous product directories are no longer supported.
- First-run favourites seeding shall apply only when the current favourites file does not exist; existing saved data shall never be overwritten by seeding.
- The WPF assembly shall embed the versioned MVP favourites snapshot as first-run seed data, including the current Scythe favourite.
- Build outputs and IDE-specific state shall be excluded from Git.

## 10. Caching and request-efficiency requirements

| ID | Requirement |
| --- | --- |
| NFR-CACHE-001 | Latest-price data shall be cached for approximately one minute. |
| NFR-CACHE-002 | Item mapping data shall be cached for approximately twelve hours. |
| NFR-CACHE-003 | History shall be cached per item and time-series resolution for approximately fifteen minutes. |
| NFR-CACHE-004 | Concurrent cache refreshes of the same category shall be serialized within the application process. |
| NFR-CACHE-005 | Startup warming shall use bounded parallelism; the MVP maximum is three history requests at once. |
| NFR-CACHE-006 | Item-icon downloads shall use bounded concurrency, an identified HTTP client, atomic cache-file publication, and a maximum accepted response size. A cache or network failure shall remain cosmetic. |

## 11. Non-functional requirements

### 11.1 Performance

| ID | Requirement |
| --- | --- |
| NFR-PERF-001 | Desktop navigation using cached data should update without recreating the application host. |
| NFR-PERF-002 | Search shall return no more than a small bounded result set; the MVP limit is eight items. |
| NFR-PERF-003 | The UI shall remain responsive while API requests are in progress and shall display loading states where appropriate. |

### 11.2 Reliability

| ID | Requirement |
| --- | --- |
| NFR-REL-001 | Failure of the external price service shall not prevent application startup. |
| NFR-REL-002 | Failure of startup history warming shall be logged as a warning and shall not terminate the application. |
| NFR-REL-003 | Local favourite writes shall avoid leaving a partially written target file. |

### 11.3 Security and privacy

| ID | Requirement |
| --- | --- |
| NFR-SEC-001 | The application shall not collect RuneScape credentials. |
| NFR-SEC-002 | User-specific runtime data and credentials shall not be committed to source control. |
| NFR-SEC-003 | No API secrets shall be embedded in source code or documentation. |
| NFR-SEC-004 | User-controlled values shall be rendered through normal WPF data binding without being interpreted as markup. |
| NFR-SEC-005 | The application shall run locally without requiring an application account or a hosted backend. |

### 11.4 Accessibility and usability

| ID | Requirement |
| --- | --- |
| NFR-A11Y-001 | Primary navigation and interactive actions shall be keyboard operable. |
| NFR-A11Y-002 | Remove actions shall expose an item-specific accessible name. |
| NFR-A11Y-003 | The price graph shall expose point values through keyboard/mouse tooltips and accompanying textual quote metrics. |
| NFR-A11Y-004 | Positive, negative, input, and output meaning shall not rely solely on position. |
| NFR-A11Y-005 | Focus indicators shall be visible for favourite selection and removal controls. |

### 11.5 Maintainability and testability

| ID | Requirement |
| --- | --- |
| NFR-MAINT-001 | Domain models and calculations shall remain in the Core project. |
| NFR-MAINT-002 | Market behavior and use cases shall remain in the Application project; external HTTP and JSON persistence shall remain in Infrastructure. |
| NFR-MAINT-003 | WPF view-models shall consume shared services and domain results rather than reproduce calculation rules. |
| NFR-MAINT-004 | Infrastructure dependency registration shall configure HTTP, persistence, caches, calculator, and discovered methods for the desktop host. |
| NFR-MAINT-005 | Each XP Planner skill shall be maintained in its own Infrastructure folder. A skill catalogue shall compose its method definitions, method-specific data shall remain in separate files as the catalogue expands, and the main catalogue shall only compose skills in canonical order. |
| NFR-MAINT-006 | A training method shall own its market-item ID and display name together as a local `CatalogueItem`; application-wide training-item registries shall not be used. |
| NFR-MAINT-007 | Rules genuinely shared by multiple methods of one skill shall be centralized in that skill's `Global.cs`; method files shall consume those rules by composition rather than duplicate them or inherit UI behavior. |
| NFR-MAINT-008 | An item shared by methods within one skill may live in that skill's `Global.cs`. Items coincidentally reused by unrelated skills shall remain locally owned so new methods do not create cross-skill catalogue coupling. |
| NFR-MAINT-009 | The shared calculator shall consume normalized skill configuration through Core contracts; WPF shall render option metadata and shall not reproduce skill-specific game rules. |
| NFR-TEST-001 | The regression harness shall cover calculation rules, caching, history filtering, search ordering, retries, warmup, JSON persistence, and WPF view-model behavior. |
| NFR-TEST-002 | A release candidate shall build with zero compiler errors. |

### 11.6 Compatibility

| ID | Requirement |
| --- | --- |
| NFR-COMP-001 | Core, Application, and Infrastructure shall target .NET 8; WPF and its tests shall target `net8.0-windows10.0.19041.0`. |
| NFR-COMP-002 | The supported desktop platform shall initially be Windows 10 version 2004 or newer on x64. |
| NFR-COMP-003 | The layout shall avoid page-level horizontal overflow at supported desktop widths. |
| NFR-COMP-004 | Tables may use contained horizontal scrolling when their readable minimum width exceeds the available viewport. |
| NFR-COMP-005 | The release executable shall be self-contained and shall not require the .NET Desktop Runtime on the target machine. |
| NFR-COMP-006 | Product, project, and assembly names shall use `07Tools`. C# root namespaces shall remain `RunescapeTools` so identifiers remain valid. The desktop host shall enforce a single running instance through the `07Tools` mutex. |

## 12. Configuration requirements

| Setting | Purpose | Default behavior |
| --- | --- | --- |
| `OsrsWikiOptions.BaseAddress` | Selects the OSRS Wiki price API root. | `https://prices.runescape.wiki/api/v1/osrs/`. |
| `OsrsWikiOptions.UserAgent` | Identifies the application and maintainer to the Wiki API. | Uses the configured application value. |
| `OsrsWikiOptions.Timeout` | Limits an individual HTTP request. | 20 seconds. |
| `OsrsWikiOptions.MaxRetryAttempts` | Limits transient 429/server-error attempts. | Three attempts. |
| `FavouriteStoreOptions.FilePath` | Selects the favourites JSON file. | Uses the desktop LocalAppData path. |
| `FavouriteStoreOptions.SeedJson` | Supplies optional first-run seed content. | Embedded desktop snapshot, applied only when no favourites file exists. |
| `MarketDataOptions` | Controls latest, mapping, history, window, and warmup cache behavior. | Shared defaults. |
| `TrainingPlanOptions.FilePath` | Selects the per-RSN XP Planner JSON file. | WPF uses the LocalAppData data directory. |
| `MoneyMakingPreferenceOptions.FilePath` | Selects the JSON file for actions/hour overrides keyed by method slug. | WPF uses the LocalAppData data directory. |
| `ItemIconCacheOptions` | Selects the persistent Wiki item-icon directory and bounds icon response size and download concurrency. | WPF uses `%LocalAppData%\07Tools\data\item-icons`. |

### 12.1 Release packaging

- The WPF publish profile shall target `win-x64` with `SelfContained=true` and `PublishSingleFile=true`.
- Native libraries shall be included for self-extraction, symbols shall be embedded, and trimming shall remain disabled.
- The release artifact shall be named `07Tools.exe` and shall include the 07 monogram application icon.
- The supported release command is `dotnet publish src\07Tools.Wpf\07Tools.Wpf.csproj -c Release -r win-x64 -p:PublishProfile=win-x64`.

The maintainer shall verify the configured contact information before public distribution.

## 13. MVP acceptance criteria

The MVP is accepted when all of the following are true:

1. The solution builds successfully with no compiler errors.
2. The NUnit regression suite passes all discovered tests through `dotnet test 07Tools.sln`.
3. The dashboard loads persisted favourites and live midpoint prices when the Wiki service is available.
4. The user can search for an item, add it, select it, inspect one-day through one-month history, and remove it.
5. Favourite changes persist across application restarts.
6. Favourite history defaults to seven days, uses hourly data through seven days, uses six-hour data for one month, and follows the discrete mouse-wheel zoom sequence.
7. The Vyrewatch method displays live-priced inputs, outputs, tax, per-account profit, and total five-account profit.
8. Adding another `IMoneyMakingMethod` implementation makes it available without a manual navigation registration.
9. Market-service failures display understandable fallback messages without deleting favourites.
10. The WPF views remain usable at 1100 × 720 without uncontrolled page-level overflow.
11. A second desktop instance exits cleanly without opening a competing favourites store.
12. First launch creates favourites from the embedded snapshot only when no current favourites file exists; existing saved state is read directly without importing or replacing it.
13. The `win-x64` Release publish produces a self-contained single-file `07Tools.exe` with trimming disabled.
14. The complete solution contains only Core, Application, Infrastructure, Wpf, and Tests, with no dependency on a retired application host.
15. Build outputs, local executables, logs, and IDE-specific state remain ignored by Git.
16. XP Planner hours span every applicable level band and reproduce the reviewed Construction 0-to-200m benchmark within rounding tolerance.
17. Unpriced training segments remain visible through economic-coverage states, and per-RSN goals survive restart.
18. A money-maker actions/hour override reprices the method and XP Planner contribution, survives restart, remains independent per method, and returns to the active configured default when reset.
19. XP Planner skill configuration survives restart per RSN, recalculates immediately, and returns to documented defaults on per-skill reset.
20. Prayer defaults to Superior dragon bones at the Gilded Altar, Firemaking defaults to Pyromancer, and disabling Fletching hours leaves its economics intact.

### 13.1 Automated verification

- The Windows-targeted NUnit project shall organize tests by layer and feature, with individual skill folders for training methods and shared test-only fakes/builders.
- NUnit shall discover tests automatically; new tests shall not require a console-runner registration entry.
- Migration shall preserve every existing regression assertion and record old-to-new scenario mapping without treating test counts as coverage percentages.
- Prices and HTTP responses shall use controlled test data. Persistence tests shall own unique temporary directories and never alter the user's application data.
- WPF smoke checks shall execute serially on an STA thread with explicit application cleanup. CI shall run them in a separate test process with a two-minute hang limit.
- GitHub Actions shall build all five solution projects and run tests on Windows for pull requests and pushes to `main`, retaining TRX results for 14 days.
- The test command, fixture organization, and instructions for adding tests shall be maintained in the repository testing guide.

## 14. Risks and dependencies

| Risk or dependency | Impact | Mitigation |
| --- | --- | --- |
| Wiki API outage or throttling | Current prices, search, or history may be unavailable. | Cache responses, retry limited transient failures, and show fallback messages. |
| API schema or endpoint changes | Parsing can fail or data can become incomplete. | Keep integration isolated in `OsrsWikiPriceClient` and validate failures visibly. |
| Thinly traded items | Midpoint and history may be sparse or misleading. | Show unavailable states and require two valid points for a graph. |
| Current midpoint differs from realized trade price | GP/hour is an estimate. | Label the pricing rule and expose input/output line values. |
| JSON storage is local and single-instance | No cloud synchronization and limited multi-process coordination. | Enforce one WPF process per user session; introduce a database only when broader concurrency is required. |
| WPF, reflection-based discovery, LiveCharts2, and SkiaSharp are trimming-sensitive | A trimmed release may fail at runtime. | Keep `PublishTrimmed=false` until a separately tested trimming design exists. |
| Game mechanics or tax rules change | Method estimates become inaccurate. | Keep quantities and tax rates explicit in method definitions and regression tests. |

## 15. Future requirements candidates

- Additional reviewed GP/XP coverage for lower-level and alternative training methods.
- Secondary-XP session ownership and crediting without double-counting hours or GP.
- UI-driven method creation and parameter overrides.
- Price and profit alerts.
- Optional historical persistence beyond the Wiki API window.
- Import/export of favourites and method definitions.
- Database-backed local storage.
- Desktop release automation and update distribution.
- Broader integration-failure and accessibility coverage beyond the NUnit regression and WPF smoke suite.

## 16. Requirements governance

- Requirement IDs shall remain stable after publication.
- Changed behavior shall update this document and its affected acceptance criteria in the same pull request or commit.
- New features shall declare whether they extend MVP scope or a future release.
- Business-rule changes affecting profitability shall include regression coverage before release.
