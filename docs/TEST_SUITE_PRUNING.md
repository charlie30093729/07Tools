# Behavior-focused test-suite pruning

Audited the complete suite from `e2ccc56`, including each candidate's production owner and
the actual inputs/assertions of its proposed replacement. Application/persistence, catalogue,
and WPF areas were reviewed separately and reconciled before final verification. Core tests
and test-support consumers were also reviewed. No production code or public API changed.

## Result and consequential decisions

Removed 26 of 266 discovered NUnit cases (9.8%); 240 remain. This includes parameterized cases,
not just test methods. The audit investigated the whole suite rather than stopping at easy
deletions. Reaching 20% would require discarding distinct reviewed rate/unlock data or useful
failure/interaction checks; no deletions were manufactured to reach that target.

Grouped removals:

- WPF: 11 repeated label/selection/reset/persistence cases and a shallow default-label probe.
  Generic row behavior, the all-catalogue availability regression, real level-band rate refresh,
  and catalogue unlock tests retain the underlying contracts. Additive and multiplicative
  configuration round trips remain separate. The useful untradeable-price assertion was moved
  to a retained dialog check that explicitly requires the reward row, avoiding an empty-loop pass.
- Catalogue: 12 cases covering an empty configurator placeholder, an exact configurator inventory,
  redundant Rosewood/completeness checks, equivalent normalized Hunter settings, and repeated
  offering-rate/invalid-location plumbing. Independent rates, item IDs, unlocks, resource flows,
  and distinct configuration mechanics remain. Removed brittle method-count/prose/display-flag
  assertions and self-derived hours sums inside retained tests as well.
- Persistence: 3 duplicated generic serialization/non-overwrite cases. A retained non-overwrite
  check now uses a conflicting nonempty seed and verifies unchanged existing JSON and order.
  The real generic training-plan round trip still protects method IDs and configuration values.

Deleted 358 lines of test source and 31 lines of test support; added 22 test lines to preserve
or strengthen actual-result checks. Net test/support reduction: 367 lines. Production lines
deleted: **0**. Documentation and the ignored local agent guide are not included in these counts.

The orphaned `TotalResourceQuantity` helper reproduced the training calculator's band traversal.
Its seven assertions now inspect the real calculator's `ResourceRequirements` against the same
independently reviewed totals. No other test helper became orphaned. The public
`UseStableDisplayName` model property remains untouched despite having no runtime readers;
removing public API is outside this audit.

## Protection deliberately retained

- Profile-bound save snapshots, overlapping writes, cancellation/failure retention, stale reads,
  profile switching, and graceful-close completion protect user data, not implementation details.
- Independent market expectations cover weighted trade sides, missing prices, tax, accounts,
  finite Daeyalt stock, hourly versus per-XP costs, and secondary-XP rounding/convergence.
- Reviewed skill bands are not redundant just because they execute the same calculator: a wrong
  rate, item ID, or unlock in one method is a different data regression. Missing-price checks
  also retain method-specific resource-discovery and untradeable-reward contracts.
- The broad buyable-completeness check remains: some of those methods otherwise have only
  data-level checks, so removing it would leave their priced availability unprotected.
- HTTP encoding/status/parsing, cache expiry/resolution, real JSON round trips, icon caching,
  favourites ordering, and the unchanged real-WPF smoke scenario retain distinct protection.

## Verification and deliberate faults

Baseline: Release build succeeded with zero warnings/errors; 265 unit/persistence tests and
one real-WPF smoke scenario passed. No pre-existing failures or skipped tests were observed.

Two temporary production faults were rejected by retained tests:

- Skipping availability updates produced missing unlock suffixes in the retained label regression.
- Allowing a nonempty seed to replace an existing favourites file returned seed item 9 instead
  of existing item 3 in the strengthened non-overwrite check.

Both faults were fully restored. The final production diff is empty. Final Release build has
zero warnings/errors; all 239 retained unit/persistence tests and the one real-WPF smoke scenario
pass. The ViewModel/dialog subset was also rerun after restoring the label fault. `git diff --check`
passes. There is no separate application E2E suite in this repository; the real-XAML smoke test
is retained unchanged, not presented as a complete E2E substitute.

## Comparable coverage

Used the existing installed Microsoft Code Coverage collector; no dependency/project/CI changes:

```powershell
dotnet test tests/07Tools.Tests/07Tools.Tests.csproj -c Release --no-build --filter "TestCategory!=Wpf" --collect "Code Coverage;Format=Cobertura" --results-directory TestResults/pruning-baseline --blame-hang-timeout 2m
dotnet test tests/07Tools.Tests/07Tools.Tests.csproj -c Release --no-build --filter "TestCategory=Wpf" --collect "Code Coverage;Format=Cobertura" --results-directory TestResults/pruning-baseline --blame-hang-timeout 2m
```

Repeated after pruning with identical settings and `TestResults/pruning-after`. Each category runs
in a fresh process. Figures below union covered source-line identities from both runs within the
four production assemblies, deduplicating repeated class/report entries and excluding the test
assembly and third-party libraries. They are line coverage, not branch or mutation scores.

| Production assembly | Before | After | Percentage-point change |
| --- | ---: | ---: | ---: |
| Core | 93.88% | 93.75% | -0.13 |
| Application | 91.99% | 94.55% | +2.56 |
| Infrastructure | 93.26% | 94.01% | +0.75 |
| WPF (`07Tools`) | 82.85% | 83.06% | +0.21 |
| Combined | 89.63% (6,230/6,951) | 90.17% (6,268/6,951) | +0.55 |

No assembly dropped two percentage points. The only previously covered line lost was the unused
`UseStableDisplayName` record-property getter in `TrainingModels.cs`; its implementation-flag
assertions were intentionally removed. Other hit increases can arise from asynchronous completion
and instrumentation; they are not evidence of improved behavioral protection. Stable coverage
likewise does not establish equivalent protection—the source/assertion audit and targeted faults
provide separate evidence. Raw Cobertura/TRX artifacts remain in ignored `TestResults` folders.

## Remaining uncertainty

No consequential deletion depends on an unverified reciprocal-redundancy claim. This was not an
exhaustive mutation campaign. Immediate-completion fakes still cannot establish every real-world
network race; the existing smoke scenario is not comprehensive visual/accessibility or E2E coverage.
These limitations were retained and reported rather than addressed by a production refactor.
