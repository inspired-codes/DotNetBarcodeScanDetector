# Implementation Plan: Barcode Scan Detector Refactoring & Hardening

> **Revision 2 (2026-10-06).** Revised after validating revision 1 against the code (see [Revision notes](#revision-notes)). Facts marked *(verified)* were reproduced with a probe project or build, not just read from the code.

## Overview & Objectives

Step-by-step remediation of the defects, architectural shortcomings and documentation discrepancies found in **BarcodeScanDetector**, split into releases by risk:

| Phase | Theme | Release | Breaking? |
|---|---|---|---|
| A | Logic, concurrency and registration fixes | 2.0.x | No |
| B | Deterministic time + test suite modernization | 2.0.x | No (public `TimestampTicks` stays wall-clock) |
| C | Namespace move, instantiable detectors, API surface | **3.0.0** | **Yes** |
| D | Packaging, documentation, BPMN | with A/B/C as noted | No |

Phases A and B can ship independently of C. Do not mix C into an A/B release.

## Verified Baseline

Facts the plan relies on:

* `DetectorConfig.NewLineRN` is `"\n\r"`; `CheckNoCrOrLf` allocates a string per character.
* After a fast `\r`, a fast `\n` moves the cooldown end by only the CR→LF gap (~8 ms measured), **not** to a doubled 600 ms *(verified)*. The BPMN annotation says "Extends on fast inputs during cooldown", which the code matches.
* A scan of exactly **4097** characters raises `BarcodeScanned` with an **empty string**; 4096 chars scan correctly and 4098 chars raise nothing *(verified)*. Cause: `HandleReturnInput` enqueues the previous char, the queue overflows and is cleared (cooldown set in `DetectorData.Enqueue`), and the event is still raised with the empty result.
* `GenericScanDetector.SimulateFastInput(object? sender, …)` ignores `sender`; handlers always receive `sender == null` *(verified)*.
* `Environment.TickCount64` does **not** exist on `netstandard2.0` or `netstandard2.1` (CS0117) *(verified)*. `Stopwatch.GetTimestamp()` does, but its tick unit is `Stopwatch.Frequency`, not 100 ns.
* `TextInputEventArgs(string text, long deltaToPreviousTicks)` takes a *delta*, but is called with a *timestamp* at `GenericScanDetector.cs:58` and `:125` and `DetectorData.cs:16`, and `GenericScanDetector.cs:78` passes `TimestampTicks` into `ReturnInputArgs`. It only works because `TimestampTicks` is set from `DateTime.Now` at construction. Nothing reads `ReturnInputArgs.DeltaToPreviousTicks`, so the `:78` bug has no observable effect.
* The `$(AssemlbyVersion)` typo in the core csproj is harmless: the SDK falls back and generates `FileVersion` `2.0.1.0` *(verified)*.
* The core csproj packs the **root** `README.md` (`..\README.md`), not `InspiredCodes.BarcodeScanDetector/README.md`.
* Only core library + core tests build and run on Linux/macOS. WPF, WinForms, their tests and the demos need Windows.

---

## Phase A: Logic, Concurrency & Registration Fixes (non-breaking)

### A1. Newline constants and character validation
* **File:** `InspiredCodes.BarcodeScanDetector/DetectorConfig.cs`
* **Actions:**
  - Change `NewLineRN` from `"\n\r"` to `"\r\n"`; keep recognising `"\n\r"` as a delimiter in `IsLineFeedOrCarriageReturn`.
  - Rewrite `CheckNoCrOrLf` to compare characters directly (`c == '\r' || c == '\n'`) with no per-character string allocation.

### A2. CRLF handling and cooldown ordering (BPMN-aligned) — DONE
* **File:** `InspiredCodes.BarcodeScanDetector/GenericScanDetector.cs`
* **Actions:**
  - In `HandleReturnInput`, set `CooldownEndTicks` **before** raising `OnBarcodeScannedEvent` so a handler that synchronously feeds input sees an active cooldown (previously set after the event).
  - During cooldown, swallow **only** the immediate complement of the newline that ended the scan (`\n` after `\r`, or `\r` after `\n`) when it arrives within the threshold, **without** extending the cooldown. Every other fast input during cooldown still extends it.
  - **Implementation:** the "scan ended with newline X" marker is a private `_newlineComplement` field on `GenericScanDetector`, not stored in `PreviousInput` (the cooldown branch overwrites that with an empty-text args). It is set when a scan completes, consumed and cleared by the very next `ProcessInput` call whether or not that call is in cooldown, and cleared by `Reset()`. A6 must put this field under the same lock as the rest of the state.
  - **Behaviour change to note:** the 300 ms cooldown is now measured from the moment the scan completes, not from when the `BarcodeScanned` handler returns, so a slow handler no longer pushes the cooldown end later.
  - The BPMN annotation was updated in the same change (D4, A2 part). The gateway diagram has no separate cooldown branch, so only the annotation text changed. Do not describe the old behaviour as "doubling the cooldown": the real effect was a few milliseconds of extra cooldown.
  - **Tests:** `CooldownNewlineTests` (wall-clock, threshold widened to 150 ms in the tests). Replace its sleeps with the injected clock in B1/B3.

### A3. Fix empty-scan event on buffer overflow
* **Files:** `GenericScanDetector.cs`, `DetectorData.cs`
* **Actions:**
  - In `HandleReturnInput`, do not raise `BarcodeScanned` when the assembled text is empty (covers the overflow-at-newline case). The cooldown already set by the overflow stays in force.
  - Add boundary regression tests (see B3): 4096 → one event; 4097 → no event + cooldown; 4098 → no event.

### A4. Correct timestamp/delta argument usage
* **Files:** `TextInputArgs.cs`, `GenericScanDetector.cs`, `DetectorData.cs`
* **Actions:**
  - Add a `TextInputEventArgs(string text, long timestampTicks, long deltaToPreviousTicks)` constructor so callers state both values explicitly and `DateTime.Now` is not re-read implicitly.
  - Update all four call sites listed in *Verified Baseline* (`GenericScanDetector.cs:58`, `:78`, `:125`, `DetectorData.cs:16`). At `:78` pass `DeltaToPreviousTicks`.
  - Treat this as cleanup, not a behaviour fix.

### A5. Externalize timing and buffer limits
* **Files:** `DetectorConfig.cs`, `DetectorData.cs`, `GenericScanDetector.cs`
* **Actions:**
  - `ThresholdTicks` already has a setter; add a **setter for `ThresholdMillisec`** (currently get-only).
  - Add `CooldownMillisec` (default 300) and `MaxBufferLength` (default 4096) as static properties alongside the threshold.
  - Replace the hard-coded `300 * TimeSpan.TicksPerMillisecond` (`GenericScanDetector.cs:54`, `:102`; `DetectorData.cs:36`) and `4096` (`DetectorData.cs:32`).
  - These stay **static for now**. Per-instance options arrive in C2; do not design the static API as if it were final.

### A6. Thread safety
* **Files:** `DetectorData.cs`, `GenericScanDetector.cs`
* **Actions:**
  - Synchronize `CooldownEndTicks` and `PreviousInput` (currently only the queue is locked). Prefer one lock owned by `GenericScanDetector` around the whole `ProcessInput`/`Reset` state transition rather than many fine-grained locks, and make sure `DetectorData`'s own lock use cannot invert lock order.
  - **Raise `BarcodeScanned` outside the lock** (capture the text under the lock, release, then invoke), so handlers that call back into the detector or block cannot deadlock.

### A7. Idempotent UI registration
* **Files:** `WpfScanDetectorExtensions.cs`, `WinFormsScanDetectorExtensions.cs`
* **Actions:**
  - In each `Register…` method, detach the static handler (`-=`) before attaching (`+=`). This is idempotent because both use the same static method group.
  - Document that WinForms parent forms need `KeyPreview = true` to see child-control keystrokes, and that neither adapter suppresses input.
  - Note the limitation honestly: the handlers forward to the **global** detector, so two registered windows share one state machine until C2 lands.

### A8. `sender` parameter decision
* **Files:** `GenericScanDetector.cs`, `ScanDetector.cs`
* **Actions:**
  - `SimulateFastInput`'s `sender` is dead today. In A: mark the `object sender` facade overloads `[Obsolete]` or document that the value is ignored. In C2 the instance detector raises events with itself as `sender`.

---

## Phase B: Deterministic Time & Test Modernization (non-breaking)

### B1. Monotonic, injectable clock
* **Files:** `GenericScanDetector.cs`, `DetectorData.cs`, `TextInputArgs.cs`
* **Actions:**
  - Measure deltas and cooldowns with `Stopwatch.GetTimestamp()`, converted to 100 ns ticks via `Stopwatch.Frequency`. **Do not use `Environment.TickCount64`** (not available on the targets).
  - Keep the **public** `TimestampTicks` on `BarcodeScannedEventArgs` and `TextInputEventArgs` as wall-clock (`DateTime.UtcNow`/`Now`) so consumers are not broken; the monotonic value is internal.
  - Route all time reads through an internal `Func<long>` clock so tests can drive time deterministically. Guard against negative deltas.
  - Before adding `InternalsVisibleTo` for the test project, check whether the assembly is actually strong-name signed (`SignAssembly` is `True`); a signed assembly requires the friend assembly's public key in the attribute.

### B2. Modernize test targets
* **File:** `InspiredCodes.BarcodeScanDetector.Tests/InspiredCodes.BarcodeScanDetector.Tests.csproj`
* **Actions:**
  - Replace `net6.0` (out of support) with `net8.0;net10.0`, or just `net10.0`. net8.0 reaches end of support in November 2026, so prefer `net10.0` alone unless net8.0 coverage is needed.
  - Bump `Microsoft.NET.Test.Sdk`, `MSTest.*` (3.0.1) and `coverlet.collector` and confirm they run against the new target.
  - Every extra target repeats the wall-clock tests (`SimulateFastInputTest` alone is ~12 s). Do the B1 clock work before multi-targeting so timing tests shrink instead of multiplying.
  - This is a modernization, not a typo fix. Local machines need the matching runtimes installed (only .NET 10 is present on the current dev box).

### B3. Expand the test suite
* **File:** `InspiredCodes.BarcodeScanDetector.Tests/ScanDetectorTests.cs` (plus a new internal-detector test class using the B1 clock)
* **Actions (using the injected clock, not `Thread.Sleep`):**
  - **CR-only scan** and **CRLF scan**: one event; the trailing `\n` does not extend the cooldown (A2); a different fast input during cooldown still extends it.
  - **Interleaved typing**: fast burst, pause > threshold, fast burst → no event, queue reset.
  - **Buffer boundaries**: 4096 → one event; 4097 → **no** event and an active cooldown (A3 regression); 4098 → no event. *(`BufferLimitTest` already covers 4100 chars; keep it.)*
  - **Handler re-entrancy**: a handler that feeds input during the event observes an active cooldown (A2), and does not deadlock (A6).
  - **Idempotent registration** (Windows only): `MockInputElement` (WPF tests) and a `Control` (WinForms tests) registered twice must forward each input **once**. Assert through the facade, not just "does not throw".
* **Note:** the facade's `SimulateFastInput` strips CR/LF and always sends a single `\n`; CR/CRLF tests must call `ProcessInput` directly.

---

## Phase C: Breaking Changes (3.0.0)

Ship only as a deliberate major release. Bump `AssemblyVersion`/`FileVersion`/`Version` in **every** csproj (they are duplicated by hand) and write migration notes.

### C1. Namespace alignment
* **Files:** all types in `InspiredCodes.BarcodeScanDetector`, the WinForms adapter, core tests, demos, READMEs.
* **Actions:**
  - Move the core types from `InspiredCodes.WPF.BarcodeScanDetector` to `InspiredCodes.BarcodeScanDetector`. (Revision 1 said "change `InspiredCodes.BarcodeScanDetector` to `InspiredCodes.BarcodeScanDetector`", which is a no-op.)
  - Remove `using InspiredCodes.WPF.BarcodeScanDetector;` from the WinForms adapter; the WPF adapter keeps its own namespace for the extension methods.
  - **Type forwarders cannot do this**: they redirect a type between assemblies and cannot alias a namespace. Either accept the break and document it (recommended), or ship `[Obsolete]` shim types in the old namespace for one release. Existing consumers with `using InspiredCodes.WPF.BarcodeScanDetector;` will lose `ScanDetector`/`BarcodeScannedEventArgs` resolution.

### C2. Instantiable detectors and options
* **Files:** `GenericScanDetector.cs`, `ScanDetector.cs`, `DetectorConfig.cs`, both adapters
* **Actions:**
  - Make the detector public (e.g. `BarcodeScanDetector`, optionally behind `IScanDetector`) so isolated instances can serve separate windows, controls or devices.
  - Move threshold, cooldown and buffer limit into a per-instance options object (defaulting from the A5 statics), so instances can differ. Static-only config contradicts instantiable detectors.
  - Change `public class ScanDetector` to `public static class ScanDetector`; it remains a convenience facade over default instances.
  - The instance raises events with itself as `sender` (resolves A8).
  - **Add adapter overloads that take a detector instance**, e.g. `RegisterTextInput(this IInputElement e, BarcodeScanDetector detector)`. Without this, instance detectors are unreachable from the extension methods, because the current handlers are static and bound to the global detector.

### C3. Public surface review
* `DetectorData`, `TextInputEventArgs`, `ReturnInputArgs` and `DetectorConfig` are public implementation details. Decide per type whether to make it `internal` (preferred) or keep it public; `DetectorConfig` is a class of only statics and should become `static class` if it stays.

---

## Phase D: Packaging, Documentation & BPMN

### D1. Project configuration
* **File:** `InspiredCodes.BarcodeScanDetector/InspiredCodes.BarcodeScanDetector.csproj`
* **Actions:**
  - Line 13: fix `$(AssemlbyVersion)` → `$(AssemblyVersion)`. (Cosmetic; the generated version is already correct.)
  - Rewrite `PackageReleaseNotes`; the current text ("removed netstandard, supports net6.0-windows and net48 as there is a dependency to WPF") is stale. The core targets `netstandard2.0;netstandard2.1` and has no WPF dependency.

### D2. README packaging
* **Decision needed:** the core csproj packs the root `README.md` as `PackageReadmeFile`. Either (a) keep the root README as the package readme and make it package-appropriate, or (b) pack `InspiredCodes.BarcodeScanDetector/README.md`. Whichever is chosen, D3's edits must land in the file that is actually packed.

### D3. Documentation cleanup
* **Files:** `README.md`, `InspiredCodes.BarcodeScanDetector/README.md`
* **Actions:**
  - Replace the absolute `file:///c:/Users/Peter/...` links in the root README with repository-relative links.
  - Core README: replace nonexistent `ScanDetector.Register(this)` with `this.RegisterTextInput()`; fix `BarcodeScannedArgs` → `BarcodeScannedEventArgs`; remove the stale "needs reference to PresentationCore" claim (the core no longer depends on it); remove the internal Steelcase feed/push/delete commands and the `@since … @steelcase.com` line.
  - State that `ScanDetector` observes input and does **not** suppress it from reaching focused controls.

### D4. BPMN alignment
* **File:** `Documentation/CharInputStateMachine.bpmn`
* **Actions:**
  - Update the cooldown annotation ("Extends on fast inputs during cooldown") to carve out the swallowed newline-complement (A2).
  - Add the "assembled text empty → no event" path to the return/queue branch (A3).
  - Ship this with A2/A3, not later; otherwise the BPMN no longer matches the code it is meant to specify.

### D5. WinForms package metadata
* **File:** `InspiredCodes.WinForms.BarcodeScanDetector/InspiredCodes.WinForms.BarcodeScanDetector.csproj`
* **Decision needed:** the root README presents this project as a package, but it has `GeneratePackageOnBuild=false` and no package metadata (description, license, authors, product). Either add the metadata and enable packing, or stop describing it as a NuGet package.

---

## Verification & Rollout

| Check | Where | Command / action |
|---|---|---|
| Core build | Any OS | `dotnet build InspiredCodes.BarcodeScanDetector/InspiredCodes.BarcodeScanDetector.csproj` |
| Core tests | Any OS (runtime for the chosen TFM installed) | `dotnet test InspiredCodes.BarcodeScanDetector.Tests`; locally on a newer-only runtime, `DOTNET_ROLL_FORWARD=Major` |
| WPF / WinForms adapter tests | **Windows only** | `dotnet test` on each test project |
| Demos | **Windows only** | Run `WpfDemo` and `WinFormsDemo`; trigger the UUIDv7 simulator and a real scanner if available |
| Packaging | Any OS | `dotnet build` already runs pack (`GeneratePackageOnBuild`); inspect the `.nupkg` for the readme, icon, version, and (3.0.0) migration notes |

Release sequence: **A + D4 (BPMN) + the A-related D3 items → B → C with the 3.0.0 bump.** A regression test accompanies every behavioural fix in A.

---

## Revision notes

Changes from revision 1, as found by validation against the code:

* **1.2:** removed the "doubling to 600 ms" claim (measured ~8 ms) and added the BPMN conflict; the BPMN update is now an explicit task (D4).
* **1.3:** reclassified from "critical" to cleanup; added the other call sites that pass a timestamp as a delta.
* **1.4:** replaced `Environment.TickCount64` (unavailable on netstandard) with `Stopwatch` + frequency conversion; kept public `TimestampTicks` wall-clock to avoid a silent breaking change; added the injectable clock.
* **3.3:** fixed the garbled namespace step, removed the unworkable "type forwarders" suggestion, moved it to the 3.0.0 breaking phase.
* **4.1:** marked the `FileVersion` typo as cosmetic.
* **4.2:** no longer called a "typo"; dropped EOL net6.0, noted runtime availability and per-TFM cost, sequenced after the clock work.
* **4.3:** acknowledged that `BufferLimitTest` exists; added boundary, re-entrancy and deterministic-clock tests; noted `SimulateFastInput` cannot produce CR/CRLF input.
* **New:** the 4097-char empty-event bug (A3); the ignored `sender` parameter (A8); adapters not reaching instance detectors (C2); README-packaging, WinForms-package and stale-README findings (D2, D3, D5); release phasing and a corrected verification matrix.
