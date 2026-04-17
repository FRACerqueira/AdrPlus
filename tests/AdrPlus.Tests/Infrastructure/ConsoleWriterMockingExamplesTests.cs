// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Core;
using AdrPlus.Domain;
using AdrPlus.Infrastructure.FileSystem;
using AdrPlus.Infrastructure.UI;
using static AdrPlus.Tests.Helpers.TestPathData;

namespace AdrPlus.Tests.Infrastructure;

/// <summary>
/// Examples of how to mock IConsoleWriter for unit testing.
/// These tests demonstrate various mocking patterns using NSubstitute.
/// </summary>
public class ConsoleWriterMockingExamplesTests
{
    [Fact]
    public void MockWriteSuccess_VerifiesMethodWasCalled()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var message = "Operation completed successfully";

        // Act
        mockConsole.WriteSuccess(message);

        // Assert
        mockConsole.Received(1).WriteSuccess(message);
    }

    [Fact]
    public void MockWriteError_VerifiesMethodWasCalled()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var errorMessage = "An error occurred";

        // Act
        mockConsole.WriteError(errorMessage);

        // Assert
        mockConsole.Received(1).WriteError(errorMessage);
    }

    [Fact]
    public void MockWriteInfo_VerifiesCallWithAnyString()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();

        // Act
        mockConsole.WriteInfo("Some info message");

        // Assert
        mockConsole.Received().WriteInfo(Arg.Any<string>());
    }

    [Fact]
    public void MockMultipleWriteCalls_VerifiesAllCalls()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();

        // Act
        mockConsole.WriteStartCommand("test command");
        mockConsole.WriteInfo("Processing...");
        mockConsole.WriteSuccess("Done!");
        mockConsole.WriteFinishedCommand("test command");

        // Assert
        mockConsole.Received(1).WriteStartCommand("test command");
        mockConsole.Received(1).WriteInfo("Processing...");
        mockConsole.Received(1).WriteSuccess("Done!");
        mockConsole.Received(1).WriteFinishedCommand("test command");
    }

    [Fact]
    public void MockPromptConfirm_ReturnsConfiguredResponse()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, ConfirmYes: true));

        // Act
        var (IsAborted, ConfirmYes) = mockConsole.PromptConfirm("Do you want to continue?", CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        ConfirmYes.Should().BeTrue();
    }

    [Fact]
    public void MockPromptConfirm_SimulatesUserAbort()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IsAborted: true, ConfirmYes: false));

        // Act
        var (IsAborted, _) = mockConsole.PromptConfirm("Are you sure?", CancellationToken.None);

        // Assert
        IsAborted.Should().BeTrue();
    }

    [Fact]
    public void MockPromptEditTitleAdr_ReturnsModifiedTitle()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var defaultTitle = "Original Title";
        var newTitle = "Updated Title";

        mockConsole.PromptEditTitleAdr(defaultTitle, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: newTitle));

        // Act
        var (IsAborted, Content) = mockConsole.PromptEditTitleAdr(defaultTitle, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(newTitle);
    }

    [Fact]
    public void MockPromptEditScopeAdr_ReturnsSelectedScope()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var repoConfig = new AdrPlusRepoConfig();
        var defaultScope = "backend";
        var selectedScope = "frontend";

        mockConsole.PromptEditScopeAdr(defaultScope, repoConfig, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: selectedScope));

        // Act
        var (IsAborted, Content) = mockConsole.PromptEditScopeAdr(defaultScope, repoConfig, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(selectedScope);
    }

    [Fact]
    public void MockPrompCalendar_ReturnsSelectedDate()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var config = new AdrPlusConfig { Language = "en-US" };
        var referenceDate = new DateTime(2024, 1, 1);
        var selectedDate = new DateTime(2024, 6, 15);

        mockConsole.PrompCalendar(Arg.Any<string>(), referenceDate, config, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: selectedDate));

        // Act
        var (IsAborted, Content) = mockConsole.PrompCalendar("Select a date", referenceDate, config, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(selectedDate);
    }

    [Fact]
    public void MockPromptEditFieldPrefix_ReturnsNewPrefix()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var fieldsJson = new FieldsJson { Name = AppConstants.FieldPrefix, Value = "ADR" };
        var newPrefix = "RFC";

        mockConsole.PromptEditFieldPrefix(fieldsJson, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: newPrefix));

        // Act
        var (IsAborted, Content) = mockConsole.PromptEditFieldPrefix(fieldsJson, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(newPrefix);
    }

    [Fact]
    public void MockPromptEditFieldLenSeq_ReturnsNewLength()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var fieldsJson = new FieldsJson { Name = AppConstants.FieldLenSeq, Value = "3" };
        var newLength = 5;

        mockConsole.PromptEditFieldLenSeq(fieldsJson, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: newLength));

        // Act
        var (IsAborted, Content) = mockConsole.PromptEditFieldLenSeq(fieldsJson, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(newLength);
    }

    [Fact]
    public void MockPromptEditFieldFolderByScope_ReturnsBoolean()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var fieldsJson = new FieldsJson { Name = AppConstants.FieldFolderByScope, Value = "false" };

        mockConsole.PromptEditFieldFolderByScope(fieldsJson, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: true));

        // Act
        var (IsAborted, Content) = mockConsole.PromptEditFieldFolderByScope(fieldsJson, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().BeTrue();
    }

    [Fact]
    public void MockPromptSelectMenu_ReturnsSelectedMenuItem()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var menuItem1 = new ItemMenuWizard { Id = "menu1", Title = "Option 1", Description = "First option" };
        var menuItem2 = new ItemMenuWizard { Id = "menu2", Title = "Option 2", Description = "Second option" };
        var menuItems = new[] { menuItem1, menuItem2 };

        mockConsole.PromptSelectMenu(true, menuItems, menuItem1, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: menuItem2));

        // Act
        var (IsAborted, Content) = mockConsole.PromptSelectMenu(true, menuItems, menuItem1, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(menuItem2);
    }

    [Fact]
    public void MockPromptSelectLogicalDrive_ReturnsSelectedDrive()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var selectedDrive = SelectedDrive;

        mockConsole.PromptSelectLogicalDrive(Arg.Any<string>(), mockFileSystem, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: selectedDrive));

        // Act
        var (IsAborted, Content) = mockConsole.PromptSelectLogicalDrive("Select drive", mockFileSystem, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(selectedDrive);
    }

    [Fact]
    public void MockGetCursorPosition_ReturnsPosition()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        mockConsole.GetCursorPosition().Returns((left: 10, top: 5));

        // Act
        var (left, top) = mockConsole.GetCursorPosition();

        // Assert
        left.Should().Be(10);
        top.Should().Be(5);
    }

    [Fact]
    public void MockPressAnyKeyToContinue_ReturnsAbortStatus()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        mockConsole.PressAnyKeyToContinue(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);

        // Act
        var wasAborted = mockConsole.PressAnyKeyToContinue("Press any key...", CancellationToken.None);

        // Assert
        wasAborted.Should().BeFalse();
    }

    [Fact]
    public void MockEnableEscToAbort_VerifiesCall()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();

        // Act
        mockConsole.EnabledEscToAbort(true);

        // Assert
        mockConsole.Received(1).EnabledEscToAbort(true);
    }

    [Fact]
    public void MockConfigurePrompt_VerifiesConfigurationCall()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var config = new AdrPlusConfig
        {
            Language = "en-US",
            YesValue = "Y",
            NoValue = "N"
        };

        // Act
        mockConsole.ConfigurePrompt(config);

        // Assert
        mockConsole.Received(1).ConfigurePrompt(config);
    }

    [Fact]
    public void MockPromptGetArrayDomainsAdr_ReturnsDomainsArray()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var mockFileSystem = Substitute.For<IFileSystemService>();
        var config = new AdrPlusConfig();
        var repoConfig = new AdrPlusRepoConfig();
        var domains = new[] { "Authentication", "Database", "API" };

        mockConsole.PromptGetArrayDomainsAdr(
            mockFileSystem, 
            Arg.Any<string>(), 
            config, 
            repoConfig, 
            Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, domains, Content: (Exception?)null));

        // Act
        var result = mockConsole.PromptGetArrayDomainsAdr(
            mockFileSystem, 
            "/path", 
            config, 
            repoConfig, 
            CancellationToken.None);

        // Assert
        result.IsAborted.Should().BeFalse();
        result.domains.Should().BeEquivalentTo(domains);
        result.Content.Should().BeNull();
    }

    [Fact]
    public void MockPromptConfigJsonAppSelect_ReturnsSelectedField()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var field1 = new FieldsJson { Name = "Language", Value = "en-US", IsEnabled = true };
        var field2 = new FieldsJson { Name = "DateFormat", Value = "yyyy-MM-dd", IsEnabled = true };
        var fields = new[] { field1, field2 };

        mockConsole.PromptConfigJsonAppSelect(field1, fields, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: field2));

        // Act
        var (IsAborted, Content) = mockConsole.PromptConfigJsonAppSelect(field1, fields, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(field2);
    }

    [Fact]
    public void MockPromptEditFieldYesNoChar_ValidatesNoConflict()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var yesField = new FieldsJson { Name = AppConstants.FieldYesValue, Value = "Y" };
        var noField = new FieldsJson { Name = AppConstants.FieldNoValue, Value = "N" };
        var fields = new[] { yesField, noField };

        mockConsole.PromptEditFieldYesNoChar(yesField, fields, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: "S"));

        // Act
        var (IsAborted, Content) = mockConsole.PromptEditFieldYesNoChar(yesField, fields, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be("S");
    }

    [Fact]
    public void MockPromptSelecLatesAdrs_ReturnsSelectedAdr()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var repoConfig = new AdrPlusRepoConfig();

        var adrFile1 = new AdrFileNameComponents
        {
            FileName = "ADR-001-test.md",
            IsValid = true,
            Header = new AdrHeader { StatusCreate = AdrStatus.Accepted }
        };

        var adrFiles = new[] { adrFile1 };
        static (bool, string?) validSelect(AdrFileNameComponents adr) => (true, null);

        mockConsole.PromptSelecLatesAdrs(adrFiles, repoConfig, validSelect, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, info: adrFile1));

        // Act
        var (IsAborted, info) = mockConsole.PromptSelecLatesAdrs(adrFiles, repoConfig, validSelect, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        info.Should().Be(adrFile1);
    }

    [Fact]
    public void MockPromptConfigTemplateAdrSelect_ReturnsTemplatePath()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var templatePath = "/templates/adr-template.md";

        mockConsole.PromptConfigTemplateAdrSelect(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, FilePathAdrTemplate: templatePath));

        // Act
        var (IsAborted, FilePathAdrTemplate) = mockConsole.PromptConfigTemplateAdrSelect("/templates", CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        FilePathAdrTemplate.Should().Be(templatePath);
    }

    [Fact]
    public void MockConsoleWriter_InCompleteWorkflow_SimulatesUserInteraction()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();

        // Setup the workflow: confirm -> edit title -> edit scope -> confirm
        mockConsole.PromptConfirm("Start new ADR?", Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, ConfirmYes: true));

        mockConsole.PromptEditTitleAdr(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: "New ADR Title"));

        var repoConfig = new AdrPlusRepoConfig();
        mockConsole.PromptEditScopeAdr(Arg.Any<string>(), repoConfig, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: "backend"));

        mockConsole.PromptConfirm("Confirm creation?", Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, ConfirmYes: true));

        // Act - Simulate a workflow
        var (_, ConfirmYes) = mockConsole.PromptConfirm("Start new ADR?", CancellationToken.None);
        mockConsole.WriteInfo("Creating new ADR...");

        var (_, Content) = mockConsole.PromptEditTitleAdr("", CancellationToken.None);
        var scope = mockConsole.PromptEditScopeAdr("", repoConfig, CancellationToken.None);

        mockConsole.WriteSuccess("ADR created successfully");
        var finalConfirm = mockConsole.PromptConfirm("Confirm creation?", CancellationToken.None);

        // Assert
        ConfirmYes.Should().BeTrue();
        Content.Should().Be("New ADR Title");
        scope.Content.Should().Be("backend");
        finalConfirm.ConfirmYes.Should().BeTrue();

        mockConsole.Received(1).WriteInfo("Creating new ADR...");
        mockConsole.Received(1).WriteSuccess("ADR created successfully");
    }

    [Fact]
    public void MockConsoleWriter_WithCancellation_HandlesGracefully()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        using var cts = new CancellationTokenSource();

        mockConsole.PromptConfirm(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((IsAborted: true, ConfirmYes: false));

        // Act
        cts.Cancel();
        var (IsAborted, ConfirmYes) = mockConsole.PromptConfirm("Continue?", cts.Token);

        // Assert
        IsAborted.Should().BeTrue();
    }

    [Theory]
    [InlineData("en-US")]
    [InlineData("pt-BR")]
    [InlineData("es-ES")]
    public void MockPromptEditFieldLanguage_ReturnsValidLanguage(string language)
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();
        var fieldsJson = new FieldsJson { Name = AppConstants.FieldLanguage, Value = "en-US" };

        mockConsole.PromptEditFieldLanguage(fieldsJson, Arg.Any<CancellationToken>())
            .Returns((IsAborted: false, Content: language));

        // Act
        var (IsAborted, Content) = mockConsole.PromptEditFieldLanguage(fieldsJson, CancellationToken.None);

        // Assert
        IsAborted.Should().BeFalse();
        Content.Should().Be(language);
    }

    [Fact]
    public void MockConsoleWriter_VerifyCallOrder()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();

        // Act
        mockConsole.WriteStartCommand("test");
        mockConsole.WriteInfo("Step 1");
        mockConsole.WriteInfo("Step 2");
        mockConsole.WriteSuccess("Complete");
        mockConsole.WriteFinishedCommand("test");

        // Assert - Using Received() in order
        Received.InOrder(() =>
        {
            mockConsole.WriteStartCommand("test");
            mockConsole.WriteInfo(Arg.Any<string>());
            mockConsole.WriteInfo(Arg.Any<string>());
            mockConsole.WriteSuccess(Arg.Any<string>());
            mockConsole.WriteFinishedCommand("test");
        });
    }

    [Fact]
    public void MockConsoleWriter_VerifyMethodNotCalled()
    {
        // Arrange
        var mockConsole = Substitute.For<IConsoleWriter>();

        // Act
        mockConsole.WriteInfo("Some info");

        // Assert
        mockConsole.DidNotReceive().WriteError(Arg.Any<string>());
    }
}
