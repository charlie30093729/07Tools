# Testing 07Tools

This is a Windows-targeted NUnit 4 project, not a console program. NUnit's adapter discovers
`[Test]` and `[TestCase]` methods automatically in Visual Studio Test Explorer and `dotnet test`.
No manual registration list or production-code change is required to add a test.

## Run tests

From the repository root, on Windows with the .NET 8 SDK:

```powershell
dotnet build 07Tools.sln
dotnet test 07Tools.sln --no-build --blame-hang-timeout 2m
```

Run only a feature, or omit real WPF views:

```powershell
dotnet test tests/07Tools.Tests --filter "FullyQualifiedName~Herblore"
dotnet test tests/07Tools.Tests --filter "TestCategory=Persistence"
dotnet test tests/07Tools.Tests --filter "TestCategory!=Wpf"
dotnet test tests/07Tools.Tests --filter "TestCategory=Wpf" --blame-hang-timeout 2m
```

The whole suite remains Windows-only because it references the WPF project. Excluding the
`Wpf` category does not make the project runnable on Linux. View-model checks belong to `Unit`;
`Wpf` specifically identifies real control/window smoke checks. `Persistence` includes icon-cache
and JSON-store checks that exercise the filesystem.

To save results:

```powershell
dotnet test 07Tools.sln --logger trx --results-directory TestResults --blame-hang-timeout 2m
```

`TestResults`, `bin`, and `obj` are ignored by Git. The normal `dotnet test 07Tools.sln`
command also works; the longer command above adds protection against a hung test process.

## Organization

| Folder | Responsibility |
| --- | --- |
| `Core` | Domain calculations, money-making methods, market/profile models |
| `Application` | Caching/search, favourite warmup, current-profile state |
| `Infrastructure/Http` and `Profiles` | HTTP protocol handling and hiscore parsing |
| `Infrastructure/Persistence` and `Icons` | JSON stores and persistent item-icon caching |
| `Infrastructure/Training/Catalogue` | Cross-catalogue invariants and method selection |
| `Infrastructure/Training/Skills/<Skill>` | Reviewed method rates, unlocks, resources, configuration and full-route economics |
| `Wpf/ViewModels` and `Dialogs` | Presentation state, navigation, selections and dialog data |
| `Wpf/Views` | Real XAML/control construction and binding regressions |
| `TestSupport/Fakes` | Fresh, test-owned API/service/store implementations |
| `TestSupport/Builders` | Small input factories and WPF event setup; never a second calculator |

Keep each fixture focused on a method or closely related behaviour. A new Herblore method
normally gets a file in `Infrastructure/Training/Skills/Herblore`, alongside the other methods.
Do not add another cross-skill "phase" fixture or a universal test base class.

## Add a test

Use a public fixture class and a public test method. Name the method after the behaviour being
checked; arrange inputs, call the subject, then assert an independently calculated expectation.

```csharp
namespace RunescapeTools.Tests.Core.Training;

[TestFixture]
[Category("Unit")]
public sealed class MoneyMakingAllocationExampleTests
{
    [Test]
    public void TenSelectedHours_EarnTenHoursOfProfit()
    {
        var calculator = new TrainingMoneyMakingCalculator();

        var result = calculator.Calculate(2_400_000m, [10m]);

        Assert.That(result.NetGp, Is.EqualTo(24_000_000m));
    }
}
```

Use `[TestCase]` for genuinely identical logic with different inputs. Keep separate cases for
different mechanics. Retain exact decimal expectations unless rounding requires an explicit
`.Within(...)` tolerance. Do not use the production calculator to manufacture its own expected answer.

Before adding a test, name its observable contract, a credible regression, and the independent
reason for its expected result. Prefer a precise, fast check over repeating generic plumbing
for every method. Preserve distinct rates, unlocks, failure paths, and real interactions; do not
test incidental inventories or reproduce the implementation in a fake. Temporary development
scaffolding is not automatically worth retaining. Never remove or weaken a failing test just
to make the suite green.

Use `async Task` tests and await operations. For exceptions where subtypes are valid, use
`await Assert.ThatAsync((Func<Task>)(() => operation()), Throws.InstanceOf<ExpectedException>())`;
an exact-type assertion is deliberately stricter. Never add fixed sleeps to wait for background
work: await a completion signal with a bounded timeout, as in the favourites search test.

Use `using var temporary = new TemporaryDirectory()` for file tests. Its `Path` is unique and
cleanup is restricted to that test-owned directory. Do not point a test at LocalAppData or the
repository's real seed/state files. Prefer the existing handwritten fakes over a new mocking
framework unless a concrete case needs one.

## Isolation and WPF

The assembly runs non-parallel with an explicit `en-GB` culture, preserving the migrated
numeric-label expectations. Test instances and their fakes are not shared. Do not enable broad
parallelism without checking filesystem, culture, event, and dispatcher lifetimes.

`WpfViewSmokeTests` is explicitly non-parallel and STA. It creates one application, loads its
resources without starting the production host, and shuts it down in `finally`. This preserves
the existing binding, image-fallback, dialog, and right-click navigation regressions. It is not
a substitute for manual visual/accessibility checks or a full application end-to-end test.

WPF allows only one Application lifetime in the test process: keep this as one scenario for now.
Do not add independent tests that each construct a new Application. Rerunning the smoke test
requires a fresh test process, as the CI command provides. The runner's hang limit is intentional:
NUnit's obsolete `Timeout` attribute cannot safely stop a blocked WPF thread on modern .NET.

## CI and migration baseline

`.github/workflows/tests.yml` restores packages, builds all five solution projects in Release,
runs Unit/Persistence checks, then runs WPF smoke checks in a fresh process. It requires no
application credentials or live market-price responses. TRX files are uploaded even after test
failure. Requiring this check before merge is a separate GitHub branch-protection setting.

See [MIGRATION.md](MIGRATION.md) for the historical 69-scenario migration and its original
assertion-preservation audit, not a current test inventory. `LegacyScenario` attributes are historical traceability,
not a registration mechanism; new tests do not need that attribute.

The [suite-pruning audit](../../docs/TEST_SUITE_PRUNING.md) records the later behavior-focused
review, retained protection, deliberate-fault checks, and comparable coverage measurements.
Existing fake clients often complete immediately; passing them does not prove all real-world
cancellation races safe. Independent captured hiscore fixtures and actual image-decoding
failures remain useful follow-up work.
