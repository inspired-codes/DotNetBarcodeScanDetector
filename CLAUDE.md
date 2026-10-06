# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET library that tells barcode-scanner input (which emulates a fast burst of keystrokes) apart from human typing, and raises a `BarcodeScanned` event with the concatenated text. Solution file is `InspiredCodes.BarcodeScanDetector.slnx` (needs the .NET 10 SDK; `LangVersion` is 14.0).

## Commands

Only the core library and its tests build and run on Linux/macOS. Every other project targets `net48` / `net*-windows` (WPF, WinForms, their tests, the demos) and fails on non-Windows with `NETSDK1100` unless `-p:EnableWindowsTargeting=true` is passed. With that flag all four Windows target frameworks of the adapters, demos and their tests compile on Linux (use it to compile-check changes), but the tests can't be executed there.

```bash
dotnet build InspiredCodes.BarcodeScanDetector/InspiredCodes.BarcodeScanDetector.csproj

# core tests (xUnit, net48 + net10.0). On Linux/macOS run only net10.0: net48 compiles there but can't run
dotnet test InspiredCodes.BarcodeScanDetector.Tests -f net10.0

# single test
dotnet test InspiredCodes.BarcodeScanDetector.Tests -f net10.0 --filter "FullyQualifiedName~BufferLimitTest"

# Windows only: the WPF / WinForms extension tests (xunit)
dotnet test InspiredCodes.WPF.BarcodeScanDetector.Tests
dotnet test InspiredCodes.WinForms.BarcodeScanDetector.Tests
```

- All projects use the standard `Debug`/`Release` configurations with SDK defaults (no custom configuration groups). Assemblies are not strong-named.
- A whole-solution build works on Linux with `dotnet build InspiredCodes.BarcodeScanDetector.slnx -p:EnableWindowsTargeting=true`. It currently gives 10 warnings, all CS8618 in the WPF test mock and the WinForms demo. Use `--no-incremental` when comparing warning counts, because an incremental build only reports warnings for projects it recompiled.
- Core tests are slow by design: they use real `Thread.Sleep` / `Task.Delay` to wait out the 300 ms cooldown, so `SimulateFastInputTest` alone takes ~12 s, and timing-based flakiness on a loaded machine is a real possibility.
- `GeneratePackageOnBuild` is on for the core and WPF projects, so every build emits a `.nupkg`. Version (`AssemblyVersion`/`FileVersion`/`Version`) is duplicated by hand in each csproj; keep them in sync.
- `.editorconfig` enforces file-scoped namespaces and unused-using removal (IDE0005) as warnings.

## Architecture

Three layers; the detection logic lives only in the first.

1. **Core (`InspiredCodes.BarcodeScanDetector`, netstandard2.0/2.1)** — UI-agnostic.
   - `GenericScanDetector` (internal) is the state machine: `ProcessInput(string)` compares the time since the previous input to `DetectorConfig.ThresholdTicks` (32 ms). A slow input resets the buffer; a fast input is buffered. Input held in `PreviousInput` is only enqueued when the *next* fast input arrives, so a CR/LF arriving fast is what flushes the queue and fires `BarcodeScanned`. CR/LF are never enqueued. After a scan (or a >4096-char overflow) a 300 ms cooldown starts; fast input during the cooldown is discarded and *extends* it.
   - `DetectorData` holds the queue, `PreviousInput`, `CooldownEndTicks`, and the 4096-char limit. `DetectorConfig` holds the static threshold and newline helpers.
   - `ScanDetector` is the public **static-style facade** over two global `GenericScanDetector` instances: a "bubble" one (`BarcodeScanned`, `ProcessInput`, `SimulateBubbleFastInput`) and a "tunnel/preview" one (`PreviewBarcodeScanned`, `ProcessPreviewInput`, `SimulateTunnelFastInput`). State is process-wide, so tests must call `ScanDetector.Reset()` first and must unsubscribe every handler they add to `BarcodeScanned` (a leaked handler changes every later test). `ProcessInput` and `Reset` are serialized by one lock in `GenericScanDetector`; `BarcodeScanned` is raised after that lock is released. `SimulateFastInput` blocks the calling thread with `Task.Delay(...).Wait()` and pushes the characters plus a trailing `\n` through `ProcessInput`.
2. **UI adapters** — thin extension methods that forward UI text events into the static `ScanDetector`:
   - `InspiredCodes.WPF.BarcodeScanDetector`: `RegisterTextInput` / `RegisterPreviewTextInput` on `IInputElement` (and `UnRegister…`).
   - `InspiredCodes.WinForms.BarcodeScanDetector`: `RegisterKeyPress` on `Control` (the parent form needs `KeyPreview = true` to see child-control keystrokes). Neither adapter suppresses keystrokes; they only observe.
3. **Demos** (`…WpfDemo`, `…WinFormsDemo`, net10.0-windows) — wire up an adapter and use `SimulateBubbleFastInput(Guid.CreateVersion7().ToString())` as a fake scanner.

`Documentation/CharInputStateMachine.bpmn` is the intended spec of the state machine (open it at demo.bpmn.io or Camunda Modeler); keep `GenericScanDetector` consistent with it.

## Things that will trip you up

- **Namespace does not match the project.** The core library's types are in `InspiredCodes.WPF.BarcodeScanDetector` even though the core has no WPF dependency, and the WinForms adapter does `using InspiredCodes.WPF.BarcodeScanDetector;`. Core tests import that namespace too. Don't "fix" it in one project without updating the others.
- **`Documentation/IMPLEMENTATION_PLAN.md` is the single plan (v3.0, multi-platform), not a description of current code.** Phase 0 (hardening of the 2.x engine) and Phase 1 (xUnit, no signing, Debug/Release, csproj fixes) are done; Phases 2–6 (engine rewrite, WPF/WinForms, WinUI, Blazor WASM PWA, release) are not. Its "Behaviour Contract" (R1–R10) lists rules the current engine already implements and the rewrite must keep, and its "Open Decisions" table lists what must be decided before each phase. Notably, the planned class name `BarcodeScanDetector` does not compile in the adapter namespaces (CS0118). Don't use `Environment.TickCount64` (not available on netstandard2.x).
- **Docs are partly stale.** `InspiredCodes.BarcodeScanDetector/README.md` references a nonexistent `ScanDetector.Register(this)` and an internal Steelcase NuGet feed; the root `README.md` has absolute `file:///c:/Users/...` links. The core csproj packs the root `README.md`, not the one in its own folder.
- **All test projects use xUnit 2 (2.7.0).** The core tests have `Nullable` disabled, and `AssemblyInfo.cs` turns off xUnit's default parallel execution of test classes. Every class drives the same static detector and static config, so with parallelization on, 20 of the 78 tests fail. The WPF and WinForms tests (registration idempotency) have only been compiled, never run on Windows.
