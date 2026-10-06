using Xunit;

// FacadeTests drive the process-wide ScanDetector engines, and a few tests use an engine's real
// clock, so test classes must not run in parallel (xUnit's default).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
