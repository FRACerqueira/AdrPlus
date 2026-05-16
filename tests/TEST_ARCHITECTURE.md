# Test Architecture Guide

## Overview

This document describes the testing patterns and best practices used in the AdrPlus project. It serves as a reference for developers writing new tests or maintaining existing test suites.

---

## Table of Contents

1. [Required Imports](#required-imports)
2. [General Principles](#general-principles)
3. [Mock Configuration Patterns](#mock-configuration-patterns)
4. [Helper Selection Guide](#helper-selection-guide)
5. [Command Handler Test Architecture](#command-handler-test-architecture)
6. [Domain-Specific Mock Helpers](#domain-specific-mock-helpers)
7. [Test Organization](#test-organization)
8. [Best Practices](#best-practices)

---

## Required Imports

### For CommandHandler Tests

Every CommandHandler test file must include:

```csharp
// Testing frameworks
using Xunit;
using NSubstitute;
using FluentAssertions;

// Core domain and command types
using AdrPlus.Commands;
using AdrPlus.Core;
using AdrPlus.Domain;

// Infrastructure interfaces
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;

// Test helpers and utilities
using AdrPlus.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Optional: when using static test data
using static AdrPlus.Tests.Helpers.TestPathData;

// Optional: for specific command handler
using AdrPlus.Commands.[YourCommand];
```

### For Service/Core Tests

```csharp
using Xunit;
using NSubstitute;
using FluentAssertions;
using AdrPlus.Core;
using AdrPlus.Tests.Helpers;
```

### For Domain Tests

```csharp
using Xunit;
using FluentAssertions;
using AdrPlus.Domain;
```

### For Infrastructure Tests

```csharp
using Xunit;
using NSubstitute;
using FluentAssertions;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Tests.Helpers;
```

---

## General Principles

### Rule 1: No Real Implementations in Tests
Never use real implementations in unit tests - only use mocked dependencies. This is non-negotiable.

```csharp
// ❌ WRONG - Using real file system
var handler = new MyCommandHandler(new RealFileSystemService(), ...);

// ✓ CORRECT - Using mock
var mockFileSystem = Substitute.For<IFileSystemService>();
var handler = new MyCommandHandler(mockFileSystem, ...);
```

### Rule 2: All External Dependencies Must Be Mocked
Configure mocks with appropriate return values/behaviors, even if they appear to have no external dependencies.

```csharp
var mockLogger = Substitute.For<ILogger<MyCommandHandler>>();
var mockFileSystem = Substitute.For<IFileSystemService>();
var mockConsole = Substitute.For<IPromptConsole>();
var mockValidateConfig = Substitute.For<IValidateJsonConfig>();
var mockAdrServices = Substitute.For<IAdrServices>();

// Configure each mock with expected behavior
mockAdrServices.ParseArgs(Arg.Any<string[]>(), Arg.Any<Arguments[]>())
	.Returns(parsedArgs);
```

### Rule 3: Cross-Platform Compatibility
All tests must pass on Windows and Linux. No platform-specific code or dependencies.

- Use `Path` class methods instead of hardcoded path separators
- Avoid Windows-specific APIs
- Test on multiple .NET versions (.NET 8, 9, 10)

---

## Mock Configuration Patterns

### Pattern 1: Generic Command Mock Helper

For command handlers with standard file system and configuration operations:

```csharp
// Use CommandHandlerMockHelper.SetupBasicCommandMocks()
CommandHandlerMockHelper.SetupBasicCommandMocks(
	mockAdrServices,
	mockFileSystem,
	mockValidateConfig,
	parsedArgs,
	jsonConfig);
```

**What it configures**:
- `ParseArgs`: Returns provided parsed arguments
- `HasTemplateRepoFile`: Returns true
- `FileExists`: Returns true for .md and .adrplus files
- `ReadAllTextAsync`: Returns JSON configuration
- `ValidateRepoStructure`: Returns validation result
- `GetFileRootRepositoryPath`: Returns config file path
- `GetFullNameFile`: Returns normalized full paths
- `GetFullNameDirectoryByFile`: Returns directory from file path

### Pattern 2: Domain-Specific Mock Helper

When a command handler has specific business logic requirements that the generic helper doesn't support:

Create a specialized helper that:
1. Calls the generic helper as a base
2. Overrides specific mocks with domain-aware logic
3. Documents the business semantics

**Example**: `SupersedeCommandHandlerMockHelper.SetupSupersedeCommandMocks()`

```csharp
public static void SetupSupersedeCommandMocks(
	IAdrServices mockAdrServices,
	IFileSystemService mockFileSystem,
	IValidateJsonConfig mockValidateConfig,
	Dictionary<Arguments, string> parsedArgs,
	string jsonConfig)
{
	// Start with generic setup
	CommandHandlerMockHelper.SetupBasicCommandMocks(
		mockAdrServices,
		mockFileSystem,
		mockValidateConfig,
		parsedArgs,
		jsonConfig);

	// Override with domain-specific behavior
	mockFileSystem.FileExists(Arg.Any<string>())
		.Returns(callInfo =>
		{
			var filePath = callInfo.Arg<string>();
			// Domain logic: input files exist, output files don't
			if (IsInputFile(filePath, parsedArgs))
				return true;
			return false;
		});
}
```

---

## Command Handler Test Architecture

### Test Suite Structure

```csharp
public class MyCommandHandlerTests
{
	// Setup: Mock dependencies
	private readonly ILogger<MyCommandHandler> _mockLogger;
	private readonly IFileSystemService _mockFileSystem;
	private readonly IPromptConsole _mockConsole;
	private readonly IValidateJsonConfig _mockValidateConfig;
	private readonly IAdrServices _mockAdrServices;
	private readonly MyCommandHandler _handler;

	// Constructor: Initialize all mocks
	public MyCommandHandlerTests()
	{
		_mockLogger = Substitute.For<ILogger<MyCommandHandler>>();
		_mockFileSystem = Substitute.For<IFileSystemService>();
		_mockConsole = Substitute.For<IPromptConsole>();
		_mockValidateConfig = Substitute.For<IValidateJsonConfig>();
		_mockAdrServices = Substitute.For<IAdrServices>();

		_handler = new MyCommandHandler(
			_mockLogger,
			Options.Create(new AdrPlusConfig { Language = "en-US" }),
			_mockFileSystem,
			_mockValidateConfig,
			_mockConsole,
			_mockAdrServices);
	}

	#region Constructor Tests
	[Fact]
	public void Constructor_WithValidParameters_CreatesInstance()
	{
		// Arrange & Act
		var handler = new MyCommandHandler(...);

		// Assert
		handler.Should().NotBeNull();
	}
	#endregion

	#region ExecuteAsync - Help Tests
	[Fact]
	public async Task ExecuteAsync_WithHelpArgument_WritesHelpToConsole()
	{
		// Arrange
		var args = new[] { "--help" };
		var parsedArgs = new Dictionary<Arguments, string> { 
			{ Arguments.Help, string.Empty } 
		};
		_mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>())
			.Returns(parsedArgs);

		// Act
		await _handler.ExecuteAsync(args, CancellationToken.None);

		// Assert
		_mockConsole.Received(1).PromptWriteHelp(Arg.Any<string>());
	}
	#endregion

	#region Helper Methods
	private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig)
	{
		CommandHandlerMockHelper.SetupBasicCommandMocks(
			_mockAdrServices,
			_mockFileSystem,
			_mockValidateConfig,
			parsedArgs,
			jsonConfig);
	}
	#endregion
}
```

### Test Organization with Regions

Group tests into logical regions for clarity:

```csharp
#region Constructor Tests
// Tests for constructor validation and initialization

#region ExecuteAsync - Help Tests
// Tests for --help argument handling

#region ExecuteAsync - Validation Tests
// Tests for input validation and error handling

#region ExecuteAsync - Core Logic Tests
// Tests for main handler functionality

#region ExecuteAsync - Wizard Mode Tests
// Tests for interactive wizard flows

#region ExecuteAsync - Cancellation Tests
// Tests for cancellation token handling

#region Exception Handling Tests
// Tests for error scenarios and exception propagation

#region Helper Methods
// Shared helper methods for test setup
```

---

## Helper Selection Guide

Use this decision flowchart to determine which mock helper pattern to use for your tests:

### Quick Reference

| Scenario | Solution | Example |
|----------|----------|---------|
| **CommandHandler with standard file ops** | Use `CommandHandlerMockHelper` | NewAdrCommandHandler, ApproveCommandHandler |
| **CommandHandler with domain-specific logic** | Create domain-specific helper | SupersedeCommandHandler (file existence), ExplorerCommandHandler (reports) |
| **Complex test data/scenarios** | Create Fixture class | `ExplorerCommandHandlerFixture` |
| **Service/Infrastructure tests** | Direct NSubstitute setup | AdrServiceTests |
| **Path operations** | Use `PathHelper` | Cross-platform path normalization |

### Decision Tree

```
                    Starting new test?
                            |
                   _________v__________
                  /                    \
            Is it CommandHandler?      No → Direct NSubstitute mocking
                   |                       (See "Best Practices")
                  Yes
                   |
            _______v_______
           /               \
      Need standard        Domain-specific
      file system ops?     business logic?
         |                 |
        Yes               No
        |                 |
        v                 v
    Use:              Create:
    CommandHandler    [CommandName]CommandHandler
    MockHelper        MockHelper

    Examples:         Examples:
    - NewAdrCommand   - SupersedeCommand
    - ApproveCommand  - ExplorerCommand
    - VersionCommand
           |                 |
           |_________v_________|
                    |
            ________v________
           /                 \
      Test data complex?      No → Done!
      Multiple scenarios?
         |
        Yes
        |
        v
    Create Fixture class
    Example: ExplorerCommandHandlerFixture
```

### Step-by-Step Guide

#### 1. Generic Helper (CommandHandlerMockHelper)

**Use when**: Your CommandHandler has standard file system operations.

```csharp
// In your test class
private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig)
{
    CommandHandlerMockHelper.SetupBasicCommandMocks(
        _mockAdrServices,
        _mockFileSystem,
        _mockValidateConfig,
        parsedArgs,
        jsonConfig);
}

// In your test
[Fact]
public async Task ExecuteAsync_WithValidArgs_Succeeds()
{
    var parsedArgs = new Dictionary<Arguments, string> { ... };
    SetupBasicMocks(parsedArgs, validJsonConfig);

    // Test proceeds with pre-configured mocks
}
```

#### 2. Domain-Specific Helper

**Use when**: Your CommandHandler has unique business logic for file operations or path resolution.

**Example: Supersede Handler**
- Input files (being superseded) must exist
- Output files (being created) must NOT exist initially
- Paths can be with or without `.md` extension

```csharp
// Create: SupersedeCommandHandlerMockHelper.cs
internal static class SupersedeCommandHandlerMockHelper
{
    public static void SetupSupersedeCommandMocks(
        IAdrServices mockAdrServices,
        IFileSystemService mockFileSystem,
        IValidateJsonConfig mockValidateConfig,
        Dictionary<Arguments, string> parsedArgs,
        string jsonConfig)
    {
        // 1. Start with base setup
        CommandHandlerMockHelper.SetupBasicCommandMocks(
            mockAdrServices,
            mockFileSystem,
            mockValidateConfig,
            parsedArgs,
            jsonConfig);

        // 2. Override with domain-specific logic
        mockFileSystem.FileExists(Arg.Any<string>())
            .Returns(callInfo =>
            {
                var filePath = callInfo.Arg<string>();
                // Input files exist, output files don't
                return IsInputFile(filePath, parsedArgs);
            });
    }
}

// Use in tests
private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig)
{
    SupersedeCommandHandlerMockHelper.SetupSupersedeCommandMocks(
        _mockAdrServices,
        _mockFileSystem,
        _mockValidateConfig,
        parsedArgs,
        jsonConfig);
}
```

#### 3. Test Fixture Class

**Use when**: Multiple related test scenarios need shared complex setup or test data.

```csharp
// Create: ExplorerCommandHandlerFixture.cs
internal class ExplorerCommandHandlerFixture
{
    private readonly Dictionary<string, object> _scenarios;

    public void AddScenario(string name, object data) => _scenarios[name] = data;
    public object GetScenario(string name) => _scenarios[name];
}

// Use in tests
public class ExplorerCommandHandlerTests
{
    private readonly ExplorerCommandHandlerFixture _fixture;

    public ExplorerCommandHandlerTests()
    {
        _fixture = new ExplorerCommandHandlerFixture();
    }

    [Fact]
    public async Task ExecuteAsync_WithComplexScenario_GeneratesReport()
    {
        var scenario = _fixture.GetScenario("ComplexReport");
        // Use scenario data in test
    }
}
```

### Quick Comparison Table

| Aspect | Generic Helper | Domain Helper | Fixture |
|--------|---|---|---|
| **Purpose** | Common mock setup | Business-specific mocks | Complex test data |
| **Reusability** | Across commands | Single command | Single test class |
| **Complexity** | Simple | Medium | High |
| **When to Create** | Already exists | New domain logic | Complex scenarios |
| **Example** | CommandHandlerMockHelper | SupersedeCommandHandlerMockHelper | ExplorerCommandHandlerFixture |
| **Lines of Code** | 100+ | 50-100 | 50-150 |

---

## Domain-Specific Mock Helpers

### When to Create a Domain-Specific Helper

Create a specialized mock helper when:

1. **Business Logic Semantics**: The handler has specific business logic not handled by generic mocks
2. **File Path Complexity**: Complex file path handling with conditional logic
3. **Multiple Test Failures**: 5+ tests fail due to mock configuration issues
4. **Reuse Across Tests**: Multiple tests need the same specialized mock setup

### Example: SupersedeCommandHandlerMockHelper

**Why it was created**:
- Supersede handler checks if newly-created files already exist (before writing)
- Generic helper doesn't distinguish between input files (exist) and output files (don't exist)
- Input files can be specified with or without `.md` extension
- 39 tests were failing due to mock configuration mismatches

**Solution Structure**:

```csharp
public static class SupersedeCommandHandlerMockHelper
{
	/// Setup method with domain-aware logic
	public static void SetupSupersedeCommandMocks(
		IAdrServices mockAdrServices,
		IFileSystemService mockFileSystem,
		IValidateJsonConfig mockValidateConfig,
		Dictionary<Arguments, string> parsedArgs,
		string jsonConfig)
	{
		// Base setup
		CommandHandlerMockHelper.SetupBasicCommandMocks(...);

		// Domain-specific overrides
		mockFileSystem.FileExists(Arg.Any<string>())
			.Returns(callInfo =>
			{
				var filePath = callInfo.Arg<string>();

				// Config files should exist
				if (filePath.EndsWith(".adrplus"))
					return true;

				// Input files (being superseded) should exist
				if (filePath.EndsWith(".md") && IsInputFile(filePath, parsedArgs))
					return true;

				// New files being created should NOT exist
				return false;
			});

		// Override other domain-specific behaviors
		mockFileSystem.GetFullNameFile(Arg.Any<string>())
			.Returns(callInfo => callInfo.Arg<string>());
	}

	private static bool IsInputFile(string filePath, Dictionary<Arguments, string> parsedArgs)
	{
		// Logic to identify the input file from parsed arguments
		// Handles both with/without .md extension
	}
}
```

### Creating Your Own Domain-Specific Helper

Template for new domain-specific helpers:

```csharp
namespace AdrPlus.Tests.Helpers;

/// <summary>
/// Provides [CommandName]-specific mock setup for CommandHandler tests.
/// Handles [specific business logic] with proper domain semantics.
/// </summary>
internal static class YourCommandHandlerMockHelper
{
	/// <summary>
	/// Sets up mocks for YourCommand handler with domain-aware behavior.
	/// [Explain the business logic being mocked]
	/// </summary>
	public static void SetupYourCommandMocks(
		IAdrServices mockAdrServices,
		IFileSystemService mockFileSystem,
		IValidateJsonConfig mockValidateConfig,
		Dictionary<Arguments, string> parsedArgs,
		string jsonConfig)
	{
		// 1. Start with base setup
		CommandHandlerMockHelper.SetupBasicCommandMocks(
			mockAdrServices,
			mockFileSystem,
			mockValidateConfig,
			parsedArgs,
			jsonConfig);

		// 2. Override with domain-specific behavior
		mockFileSystem.YourSpecificMethod(Arg.Any<string>())
			.Returns(callInfo =>
			{
				var param = callInfo.Arg<string>();
				// Domain-aware logic here
				return ProcessWithDomainLogic(param);
			});
	}
}
```

---

## Test Organization

### File Structure

```
tests/
├── AdrPlus.Tests/
│   ├── Commands/
│   │   ├── NewAdr/
│   │   │   └── NewAdrCommandHandlerTests.cs
│   │   ├── Supersede/
│   │   │   ├── SupersedeCommandHandlerTests.cs
│   │   │   ├── REFACTORING_SUMMARY.md      (Documentation)
│   │   │   └── IMPLEMENTATION_NOTES.md     (Implementation details)
│   │   └── Version/
│   │       └── VersionCommandHandlerTests.cs
│   ├── Core/
│   │   └── [Service Tests]
│   ├── Domain/
│   │   └── [Entity Tests]
│   ├── Infrastructure/
│   │   └── [Infrastructure Tests]
│   └── Helpers/
│       ├── CommandHandlerMockHelper.cs     (Generic helper)
│       ├── SupersedeCommandHandlerMockHelper.cs (Domain-specific)
│       ├── TestPathData.cs                 (Test data)
│       └── PathHelper.cs                   (Path utilities)
```

### Naming Conventions

- **Test Classes**: `[ClassUnderTest]Tests` (e.g., `SupersedeCommandHandlerTests`)
- **Test Methods**: `[Method]_[Scenario]_[ExpectedResult]` 
  - Example: `ExecuteAsync_WithValidFile_SupersedesAdr`
  - Example: `ExecuteAsync_WhenFileNotFound_ThrowsFileNotFoundException`
- **Mock Helpers**: `[Context]MockHelper` (e.g., `SupersedeCommandHandlerMockHelper`)
- **Fixture Classes**: `[Context]Fixture` (e.g., `ExplorerCommandHandlerFixture`)

---

## Best Practices

### ✓ DO

1. **Mock All Dependencies**
   ```csharp
   var mockService = Substitute.For<IService>();
   var handler = new Handler(mockService, ...);
   ```

2. **Use Clear Test Names**
   ```csharp
   // Clear: Describes scenario and expectation
   public async Task ExecuteAsync_WithValidFile_SupersedesAdr()
   ```

3. **Arrange-Act-Assert Pattern**
   ```csharp
   // Arrange: Setup test data and mocks
   var args = new[] { "--file", validPath };

   // Act: Execute the code
   await handler.ExecuteAsync(args, CancellationToken.None);

   // Assert: Verify the result
   _mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
   ```

4. **Test One Thing Per Test**
   ```csharp
   // Good: Single assertion focus
   public void Constructor_WithValidParameters_CreatesInstance()
   {
	   handler.Should().NotBeNull();
   }
   ```

5. **Use Fluent Assertions**
   ```csharp
   handler.Should().NotBeNull();
   result.Should().Be(expected);
   await handler.Invoking(h => h.ExecuteAsync(...))
	   .Should().ThrowAsync<InvalidDataException>();
   ```

6. **Consolidate Setup Logic**
   ```csharp
   private void SetupBasicMocks(Dictionary<Arguments, string> parsedArgs, string jsonConfig)
   {
	   // Reuse across multiple tests
   }
   ```

7. **Document Complex Mocks**
   ```csharp
   /// <summary>
   /// Setup handles file path resolution with correct semantics:
   /// - Input files (being superseded) return true for FileExists
   /// - Output files (being created) return false for FileExists
   /// </summary>
   private void SetupSupersedeCommandMocks(...)
   ```

### ✗ DON'T

1. **Don't Use Real Implementations**
   ```csharp
   // ❌ NO
   var handler = new Handler(new RealFileSystemService(), ...);
   ```

2. **Don't Test Implementation Details**
   ```csharp
   // ❌ NO - Testing internal method
   var result = handler.PrivateMethod();
   ```

3. **Don't Create Interdependent Tests**
   ```csharp
   // ❌ NO - Tests should be independent
   public void Test1() { ... }
   public void Test2() { /* depends on Test1 */ }
   ```

4. **Don't Use Hardcoded Paths**
   ```csharp
   // ❌ NO
   var path = "C:\\repo\\adr\\adr-0001.md";

   // ✓ YES
   var path = PathHelper.GetAdrFilePath("adr-0001.md");
   ```

5. **Don't Skip Test Documentation**
   ```csharp
   // ❌ NO - Unclear what's being tested
   [Fact]
   public void Test1() { }

   // ✓ YES - Clear intention
   [Fact]
   public void ExecuteAsync_WithValidFile_SupersedesAdr() { }
   ```

6. **Don't Mix Concerns in Setup**
   ```csharp
   // ❌ NO - Setup is doing multiple things
   private void Setup() {
	   ConfigureLogging();
	   SetupDatabase();
	   InitializeCache();
	   CreateTestData();
   }

   // ✓ YES - Focused setup
   private void SetupBasicMocks(...) { }
   private void SetupDatabaseMocks(...) { }
   ```

---

## Testing Different Scenarios

### Testing Success Scenarios

```csharp
[Fact]
public async Task ExecuteAsync_WithValidFile_SupersedesAdr()
{
	// Arrange
	var args = new[] { "--file", ValidAdrFilePath };
	var parsedArgs = new Dictionary<Arguments, string> { 
		{ Arguments.FileAdr, ValidAdrFilePath } 
	};
	SetupBasicMocks(parsedArgs, jsonConfig);

	// Act
	await _handler.ExecuteAsync(args, CancellationToken.None);

	// Assert
	_mockConsole.Received(1).PromptWriteSuccess(Arg.Any<string>());
}
```

### Testing Error Scenarios

```csharp
[Fact]
public async Task ExecuteAsync_WhenFileNotFound_ThrowsFileNotFoundException()
{
	// Arrange
	var args = new[] { "--file", MissingAdrFilePath };
	_mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>())
		.Returns(parsedArgs);
	_mockValidateConfig.HasTemplateRepoFile().Returns(true);
	_mockFileSystem.FileExists(Arg.Any<string>()).Returns(false);

	// Act & Assert
	await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
		.Should().ThrowAsync<FileNotFoundException>();
}
```

### Testing Cancellation

```csharp
[Fact]
public async Task ExecuteAsync_WhenCancelled_ThrowsOperationCanceledException()
{
	// Arrange
	var args = new[] { "--file", ValidAdrFilePath };
	var cts = new CancellationTokenSource();
	cts.Cancel();

	// Act & Assert
	await _handler.Invoking(h => h.ExecuteAsync(args, cts.Token))
		.Should().ThrowAsync<OperationCanceledException>();
}
```

### Testing Wizard Flows

```csharp
[Fact]
public async Task ExecuteAsync_WithWizardModeFileSelectionAborted_ThrowsOperationCanceledException()
{
	// Arrange
	var args = new[] { "--wizard" };
	var parsedArgs = new Dictionary<Arguments, string> { 
		{ Arguments.WizardSupersede, string.Empty } 
	};
	_mockAdrServices.ParseArgs(args, Arg.Any<Arguments[]>())
		.Returns(parsedArgs);
	_mockConsole.PromptSelectFolderPath(Arg.Any<string>(), ...)
		.Returns((true, string.Empty)); // User cancelled

	// Act & Assert
	await _handler.Invoking(h => h.ExecuteAsync(args, CancellationToken.None))
		.Should().ThrowAsync<OperationCanceledException>();
}
```

---

## Performance Considerations

1. **Test Execution Time**: Keep individual tests fast (< 100ms target)
2. **Mock Efficiency**: Reuse setup logic to reduce redundancy
3. **Parallel Execution**: Tests should be independent for parallel runs
4. **Memory**: Mock objects should be lightweight and garbage-collectible

---

## Continuous Integration

All tests must pass in CI/CD pipelines:

- **Platform**: Windows, Linux
- **.NET Versions**: 8, 9, 10
- **Timeout**: 30 seconds per test
- **Failure Rate**: 0% expected

CI Configuration (GitHub Actions):
```yaml
test:
  runs-on: ${{ matrix.os }}
  strategy:
	matrix:
	  os: [ubuntu-latest, windows-latest]
	  dotnet-version: ['8.0', '9.0', '10.0']
```

---

## References

- **Testing Framework**: xUnit.net
- **Mocking Framework**: NSubstitute
- **Assertions**: FluentAssertions
- **Documentation**: XML Comments on test classes and helper methods

---

## Related Documents

- [REFACTORING_SUMMARY.md](tests/AdrPlus.Tests/Commands/Supersede/REFACTORING_SUMMARY.md) - Supersede test refactoring case study
- [IMPLEMENTATION_NOTES.md](tests/AdrPlus.Tests/Commands/Supersede/IMPLEMENTATION_NOTES.md) - Implementation details and lessons learned
- [CONTRIBUTING.md](CONTRIBUTING.md) - Project contribution guidelines
