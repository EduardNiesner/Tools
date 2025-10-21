# Quick Start: Running CsprojChecker Tests

## Prerequisites
- .NET 9.0 SDK or later
- Git (for cloning the repository)

## Running All Tests

### Basic Test Run
```bash
cd /path/to/Tools
dotnet test
```

**Expected Output:**
```
Test Run Successful.
Total tests: 31
     Passed: 31
 Total time: ~2.3 Seconds
```

### Detailed Test Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Class
```bash
# Run only conversion tests
dotnet test --filter "FullyQualifiedName~ConversionTests"

# Run only framework operation tests
dotnet test --filter "FullyQualifiedName~FrameworkOperationsTests"
```

### Run Specific Test
```bash
dotnet test --filter "FullyQualifiedName~TestCase1_OldStyleConsoleApp_ConvertsToSdkStyle"
```

## Test Categories

### 1. Conversion Tests (20 tests)
Tests Old-style ↔ SDK-style conversions
```bash
dotnet test --filter "FullyQualifiedName~ConversionTests"
```

### 2. Framework Operations Tests (11 tests)
Tests Change and Append Target Framework operations
```bash
dotnet test --filter "FullyQualifiedName~FrameworkOperationsTests"
```

## Test Results Location

Test results and documentation:
- `CsprojChecker.Tests/TEST_RESULTS.md` - Detailed test coverage
- `CsprojChecker.Tests/EXECUTION_SUMMARY.md` - Execution summary with timing

## Common Commands

### Build Solution
```bash
dotnet build
```

### Clean and Rebuild
```bash
dotnet clean
dotnet build
dotnet test
```

### Run Tests with Coverage (if configured)
```bash
dotnet test --collect:"XPlat Code Coverage"
```

### Watch Mode (run tests on file change)
```bash
dotnet watch test
```

## Interpreting Results

### Success
```
Test Run Successful.
Total tests: 31
     Passed: 31
```
✅ All tests passed - conversion logic is working correctly

### Failure Example
```
Test Run Failed.
Total tests: 31
     Passed: 30
     Failed: 1
```
❌ One test failed - investigate the failed test output for details

## Troubleshooting

### Tests Fail to Run
```bash
# Restore dependencies
dotnet restore

# Clean build
dotnet clean
dotnet build
```

### Specific Test Fails
1. Check the test output for the failure reason
2. Review the test code in `CsprojChecker.Tests/`
3. Look at the temporary files in `/tmp/CsprojChecker*` if needed

### Permission Issues
Ensure the test runner has permissions to create temporary directories in `/tmp/`

## Test Files Structure

```
CsprojChecker.Tests/
├── CsprojChecker.Tests.csproj      # Test project file
├── ConversionTests.cs               # 20 conversion tests
├── FrameworkOperationsTests.cs      # 11 framework operation tests
├── TEST_RESULTS.md                  # Detailed results documentation
├── EXECUTION_SUMMARY.md             # Execution summary
└── QUICK_START.md                   # This file
```

## CI/CD Integration

### GitHub Actions Example
```yaml
name: Test

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - name: Restore dependencies
        run: dotnet restore
      - name: Build
        run: dotnet build --no-restore
      - name: Test
        run: dotnet test --no-build --verbosity normal
```

## Additional Resources

- [xUnit Documentation](https://xunit.net/)
- [.NET Testing Documentation](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [CsprojChecker README](../README.md)
- [Conversion Reference](../docs/csproj-conversion-reference.md)
- [Testing Guide](../docs/testing-guide.md)

## Quick Verification

Run this command to verify everything works:
```bash
dotnet test --logger "console;verbosity=normal" | grep "Test Run"
```

Expected output:
```
Test Run Successful.
Total tests: 31
     Passed: 31
```

✅ If you see this, all tests are passing!
