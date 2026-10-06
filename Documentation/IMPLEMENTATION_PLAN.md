# Implementation Plan: Barcode Scan Detector Refactoring & Hardening

## Overview & Objectives

This implementation plan details the step-by-step remediation of all defects, architectural shortcomings, and documentation discrepancies identified during the deep code inspection of **BarcodeScanDetector**.

The goals are:
1. **Fix Critical State Machine & Logic Bugs:** Correct CRLF definitions, prevent cooldown doubling on CRLF scans, fix argument mismatches, and migrate to monotonic timing.
2. **Thread Safety & Concurrency:** Ensure thread-safe state access across the input pipeline and event dispatchers.
3. **Architecture & Decoupling:** Decouple core classes from the `WPF` namespace, expose an instantiable detector API alongside the static facade, and externalize magic numbers.
4. **Resilient UI Integration:** Make WPF and WinForms event registrations idempotent to prevent handler duplication.
5. **Modernize Test Suite & Build:** Fix csproj configuration typos, multi-target unit tests to run on modern .NET environments (.NET 6/8/10), and expand test coverage.
6. **Documentation & Spec Synchronization:** Clean up repository links, correct API usage in READMEs, and ensure 100% alignment with the BPMN state machine.

---

## Phase 1: Critical Logic & State Machine Fixes

### 1.1 Correct Newline Constants & Character Validation
* **File:** `InspiredCodes.BarcodeScanDetector/DetectorConfig.cs`
* **Actions:**
  - Change `NewLineRN` from `"\n\r"` to standard CRLF `"\r\n"`.
  - Also support both `"\r\n"` and `"\n\r"` as recognized delimiters for safety.
  - Refactor `CheckNoCrOrLf(string textInput)` to check characters directly (`c == '\r' || c == '\n'`) without allocating `textInput[i].ToString()` strings per character.

### 1.2 Fix CRLF Double-Cooldown Extension (BPMN Alignment)
* **File:** `InspiredCodes.BarcodeScanDetector/GenericScanDetector.cs`
* **Actions:**
  - When a newline character (e.g. `\r`) completes a scan and initiates the cooldown, inspect subsequent incoming input during cooldown:
    - If the incoming input is the second part of a newline sequence (e.g. `\n` following `\r`) arriving immediately, discard it **without** extending the cooldown.
    - Set the cooldown timestamp **before** firing `OnBarcodeScannedEvent` so nested or synchronous handler actions observe an active cooldown.

### 1.3 Fix Argument Mapping in `ReturnInputArgs` & `TextInputEventArgs`
* **Files:** `InspiredCodes.BarcodeScanDetector/TextInputArgs.cs`, `InspiredCodes.BarcodeScanDetector/GenericScanDetector.cs`
* **Actions:**
  - In `GenericScanDetector.cs` line 78, change:
    ```csharp
    // Incorrect: passing TimestampTicks as delta
    HandleReturnInput(new ReturnInputArgs(textInputArgs.Text, textInputArgs.TimestampTicks));
    ```
    to:
    ```csharp
    HandleReturnInput(new ReturnInputArgs(textInputArgs.Text, textInputArgs.DeltaToPreviousTicks));
    ```
  - In `TextInputEventArgs`, add an explicit constructor overload allowing caller-specified `timestampTicks` to avoid re-evaluating `DateTime.Now.Ticks` and accidentally passing timestamps into `deltaToPreviousTicks`.

### 1.4 Switch to Monotonic Time Measurement
* **Files:** `InspiredCodes.BarcodeScanDetector/GenericScanDetector.cs`, `InspiredCodes.BarcodeScanDetector/DetectorData.cs`, `InspiredCodes.BarcodeScanDetector/TextInputArgs.cs`
* **Actions:**
  - Replace `DateTime.Now.Ticks` with monotonic time (`Stopwatch.GetTimestamp()` or `Environment.TickCount64`) for all delta and cooldown measurements.
  - Guard against negative deltas resulting from system time anomalies.

---

## Phase 2: Configuration & Externalizing Constants

### 2.1 Centralize Timing and Buffer Limits
* **File:** `InspiredCodes.BarcodeScanDetector/DetectorConfig.cs`
* **Actions:**
  - Expose setter for `ThresholdMillisec` that updates `ThresholdTicks`.
  - Add configurable `CooldownMillisec` (default: 300 ms).
  - Add configurable `MaxBufferLength` (default: 4096 characters).
  - Replace hardcoded `300 * TimeSpan.TicksPerMillisecond` and `4096` in `DetectorData.cs` and `GenericScanDetector.cs` with references to these properties.

---

## Phase 3: Concurrency, Thread Safety & Architecture

### 3.1 Comprehensive Thread Safety
* **Files:** `InspiredCodes.BarcodeScanDetector/DetectorData.cs`, `InspiredCodes.BarcodeScanDetector/GenericScanDetector.cs`
* **Actions:**
  - Synchronize read/write access to `CooldownEndTicks` and `PreviousInput`.
  - Make `GenericScanDetector.ProcessInput` and `Reset` thread-safe with internal locking or thread-safe state synchronization.

### 3.2 Public Detector Instances & Facade Cleanup
* **Files:** `InspiredCodes.BarcodeScanDetector/GenericScanDetector.cs`, `InspiredCodes.BarcodeScanDetector/ScanDetector.cs`
* **Actions:**
  - Make `GenericScanDetector` public (or expose an interface `IScanDetector` / public class `BarcodeScanDetector`) so consumers can instantiate isolated detector instances for separate controls, windows, or devices.
  - Change `public class ScanDetector` to `public static class ScanDetector`.
  - Correct event `sender` parameter: pass the detector instance (or caller-supplied sender) rather than hardcoded `null`.

### 3.3 Namespace Alignment
* **Files:** All files in `InspiredCodes.BarcodeScanDetector`
* **Actions:**
  - Change root namespace of `InspiredCodes.BarcodeScanDetector` to `InspiredCodes.BarcodeScanDetector`.
  - Maintain type forwarders or namespace backward compatibility aliases so existing WPF consumers do not experience breaking compilation changes.

### 3.4 Idempotent UI Event Registration
* **Files:**
  - `InspiredCodes.WPF.BarcodeScanDetector/WpfScanDetectorExtensions.cs`
  - `InspiredCodes.WinForms.BarcodeScanDetector/WinFormsScanDetectorExtensions.cs`
* **Actions:**
  - Ensure `RegisterTextInput` and `RegisterKeyPress` detach any previously attached detector delegate before attaching, ensuring idempotency even if called multiple times.
  - Document the requirement of `KeyPreview = true` for WinForms parent forms.

---

## Phase 4: Build, Packaging & Test Modernization

### 4.1 Fix Project Configuration Typos
* **File:** `InspiredCodes.BarcodeScanDetector/InspiredCodes.BarcodeScanDetector.csproj`
* **Actions:**
  - Line 13: Fix typo `<FileVersion>$(AssemlbyVersion)</FileVersion>` -> `<FileVersion>$(AssemblyVersion)</FileVersion>`.
  - Update `PackageReleaseNotes` to accurately describe the package.

### 4.2 Modernize Test Framework Targets
* **File:** `InspiredCodes.BarcodeScanDetector.Tests/InspiredCodes.BarcodeScanDetector.Tests.csproj`
* **Actions:**
  - Multi-target test project: `<TargetFrameworks>net6.0;net8.0;net10.0</TargetFrameworks>` (or add `net10.0` so tests run directly on current .NET 10 SDKs on Linux/macOS/Windows).

### 4.3 Expand Test Suite
* **File:** `InspiredCodes.BarcodeScanDetector.Tests/ScanDetectorTests.cs`
* **Actions:**
  - Add test for **CRLF (`\r\n`) scans**: verify scan emits without extending cooldown to 600ms.
  - Add test for **CR-only (`\r`) scans**.
  - Add test for **interleaved typing**: fast typing followed by pause (> 32ms) followed by fast typing.
  - Add test for **buffer overflow (4096 characters)**: verify cooldown activation and queue clearing.
  - Add test for **idempotent registration**: verify double registration does not duplicate characters.

---

## Phase 5: Documentation & BPMN Alignment

### 5.1 Documentation Cleanup
* **Files:** `README.md`, `InspiredCodes.BarcodeScanDetector/README.md`
* **Actions:**
  - Replace absolute `file:///c:/...` links with repository-relative Markdown links.
  - Fix outdated instructions in `InspiredCodes.BarcodeScanDetector/README.md` (remove nonexistent `ScanDetector.Register(this)` and replace with `this.RegisterTextInput()`).
  - Clarify that `ScanDetector` does not intercept/suppress keystrokes from reaching focused input controls.

---

## Verification & Rollout Plan

1. **Unit Testing:** Execute `dotnet test` across all supported runtime targets (`net6.0`, `net8.0`, `net10.0`).
2. **Demo Verification:** Run WinFormsDemo and WpfDemo to verify real-time barcode simulation with UUIDv7.
3. **Packaging:** Run `dotnet pack` on `InspiredCodes.BarcodeScanDetector` to ensure clean NuGet generation with valid metadata and versioning.
