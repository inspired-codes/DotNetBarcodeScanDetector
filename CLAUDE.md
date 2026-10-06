# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A .NET library that tells barcode-scanner input (which emulates a fast burst of keystrokes) apart from human typing, and raises a `BarcodeScanned` event with the concatenated text. Solution file is `InspiredCodes.BarcodeScanDetector.slnx` (needs the .NET 10 SDK; `LangVersion` is 14.0).

## Commands

Only the core library and its tests build and run on Linux/macOS. Every other project targets .NET Framework or `net*-windows` (adapters `net472; net8.0-windows`, their tests `net48; net10.0-windows`, demos `net10.0-windows`) and fails on non-Windows with `NETSDK1100` unless `-p:EnableWindowsTargeting=true` is passed. With that flag every target compiles on Linux (use it to compile-check changes), but the WPF and WinForms tests can't be executed there.

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
- A whole-solution build works on Linux with `dotnet build InspiredCodes.BarcodeScanDetector.slnx -p:EnableWindowsTargeting=true`. It currently gives 2 warnings, both CS8618 in the WinForms demo. Use `--no-incremental` when comparing warning counts, because an incremental build only reports warnings for projects it recompiled.
- Core tests feed explicit timestamps through `ProcessInput(text, timestamp)` (see the `Recorder` helper), so they never sleep and assert cooldown boundaries exactly; the suite runs in about half a second. Prefer that over real time in new tests.
- `GeneratePackageOnBuild` is on for the core and WPF projects, so every build emits a `.nupkg`. Version (`AssemblyVersion`/`FileVersion`/`Version`) is duplicated by hand in each csproj; keep them in sync.
- `.editorconfig` enforces file-scoped namespaces and unused-using removal (IDE0005) as warnings.

## Architecture

Three layers; the detection logic lives only in the first.

1. **Core (`InspiredCodes.BarcodeScanDetector`, netstandard2.0/2.1)** — UI-agnostic, namespace `InspiredCodes.BarcodeScanDetector`.
   - `ScanDetectorEngine` is the state machine. An input that arrives within `InterKeyThreshold` (32 ms) of the previous one is fast; a slow one starts over. The last input is held back and only added to the buffer when the *next* fast input confirms it, so a fast terminator (Enter, or Tab if selected in `ScanTerminators`) is what completes a scan and raises `BarcodeScanned`. After a scan, or a scan longer than `MaxLength` (4096), a `Cooldown` (300 ms) discards input; fast input during it extends it, except the second half of a CR/LF pair. Settings come from `ScanDetectorOptions`, copied when the engine is created.
   - Time: `ProcessInput(text)` and `Simulate` use the engine's own `Stopwatch` clock; `ProcessInput(text, timestamp)` and `ProcessBatch` use the caller's. An engine fixes its time base on the first input and throws on a mix until `Reset()`. Cooldowns are computed from input timestamps, never from a second clock read.
   - Thread safety: one lock per engine covers every state change; `BarcodeScanned` (sender: the engine) is raised after the lock is released, on the thread that completed the scan.
   - `ScanDetector` is a static facade over two process-wide engines with default options, `Default` (`BarcodeScanned`, `ProcessInput`, `SimulateBubbleFastInput`) and `Preview` (`PreviewBarcodeScanned`, `ProcessPreviewInput`, `SimulateTunnelFastInput`). Their state is process-wide, so tests that use the facade must call `ScanDetector.Reset()` and unsubscribe every handler they add (a leaked handler changes every later test). Tests of the engine itself should create their own `ScanDetectorEngine`.
2. **UI adapters** — forward UI text input into an engine (an optional parameter; default `ScanDetector.Default`, or `ScanDetector.Preview` for the WPF preview event). They only observe, never suppress input, and feed the engine through its own clock. Registration is idempotent per element and engine, tracked in a `ConditionalWeakTable` (detaching by method-group equality would not work: each per-engine handler is a new closure).
   - `InspiredCodes.WPF.BarcodeScanDetector`: `RegisterTextInput` / `RegisterPreviewTextInput` (and `UnRegister…`) on `UIElement`, using `AddHandler(..., handledEventsToo: true)` so text a focused `TextBox` marks as handled is seen too.
   - `InspiredCodes.WinForms.BarcodeScanDetector`: `RegisterKeyPress` on `Control` (the parent form needs `KeyPreview = true` to see child-control keystrokes), or `BarcodeScanMessageFilter` (`IMessageFilter` on `WM_CHAR`, installed with `Application.AddMessageFilter`) to see every typed character regardless of focus. Don't use both for one engine.
3. **Demos** (`…WpfDemo`, `…WinFormsDemo`, net10.0-windows) — wire up an adapter and use `SimulateBubbleFastInput(Guid.CreateVersion7().ToString())` as a fake scanner.

`Documentation/CharInputStateMachine.bpmn` is the intended spec of the state machine (open it at demo.bpmn.io or Camunda Modeler); keep `ScanDetectorEngine` consistent with it.

## Things that will trip you up

- **Don't name a type `BarcodeScanDetector`.** Inside the adapter namespaces (`InspiredCodes.WPF.BarcodeScanDetector`, …) and any consumer namespace under `InspiredCodes.*`, that simple name resolves to the namespace, not the type (CS0118). That is why the engine is called `ScanDetectorEngine`.
- **`Documentation/IMPLEMENTATION_PLAN.md` is the single plan (v3.0, multi-platform), not a description of current code.** Phases 0–3 (2.x hardening; xUnit, no signing, Debug/Release; the v3 engine; WPF/WinForms adapters) are done, but the Windows-only tests have never been run; Phases 4–6 (WinUI, Blazor WASM PWA, release) are not. Its Status table says what still needs Windows. Its "Behaviour Contract" (R1–R10) lists the rules the engine implements, with the tests that cover them, and its "Open Decisions" table lists what must be decided before each phase. Don't use `Environment.TickCount64` (not available on netstandard2.x).
- **Docs are stale (rewritten in Phase 6).** Both READMEs still describe the 2.x API; `InspiredCodes.BarcodeScanDetector/README.md` also references a nonexistent `ScanDetector.Register(this)` and an internal Steelcase NuGet feed, and the root `README.md` has absolute `file:///c:/Users/...` links. The core csproj packs the root `README.md`, not the one in its own folder.
- **All test projects use xUnit 2 (2.7.0).** The core tests have `Nullable` disabled, and `AssemblyInfo.cs` turns off xUnit's default parallel execution of test classes. Keep it off: `FacadeTests` drive the process-wide engines, and a few tests use the engine's real clock (with parallelization on, the 2.x suite lost 20 of 78 tests). The WPF and WinForms tests run their bodies on a dedicated STA thread (`RunOnSta`), which throws on Linux; they have only been compiled, never run on Windows.
