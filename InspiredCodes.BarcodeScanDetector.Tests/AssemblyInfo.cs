using Xunit;

// Every test class drives the same static ScanDetector and the same static DetectorConfig,
// so test classes must not run in parallel (xUnit's default).
[assembly: CollectionBehavior(DisableTestParallelization = true)]
