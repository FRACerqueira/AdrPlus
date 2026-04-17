// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.Process;

namespace AdrPlus.Tests.Infrastructure.Process;

/// <summary>
/// Unit tests for classes that use <see cref="IProcessService"/>.
/// Tests use mocks of the interface rather than the real implementation.
/// </summary>
public class ProcessServiceTests
{
    #region Mock Setup Tests

    [Fact]
    public void Mock_WhenConfiguredWithSuccessResult_ReturnsEmpty()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.Create();

        // Act
        var result = mockProcessService.OpenFile("C:\\file.txt", "notepad");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Mock_WhenConfiguredWithErrorResult_ReturnsError()
    {
        // Arrange
        var errorMessage = "File not found";
        var mockProcessService = IProcessServiceMock.CreateWithError(errorMessage);

        // Act
        var result = mockProcessService.OpenFile("C:\\invalid.txt", "notepad");

        // Assert
        result.Should().Be(errorMessage);
    }

    #endregion

    #region Mock Configuration Tests

    [Fact]
    public void Mock_CanBeConfiguredWithAnyArguments_ReturnsSameResult()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.Create("success");

        // Act
        var result1 = mockProcessService.OpenFile("C:\\file1.txt", "notepad");
        var result2 = mockProcessService.OpenFile("C:\\file2.txt", "gedit");
        var result3 = mockProcessService.OpenFile("D:\\file3.txt", "start");

        // Assert
        result1.Should().Be("success");
        result2.Should().Be("success");
        result3.Should().Be("success");
    }

    [Fact]
    public void Mock_CanBeConfiguredForSpecificArguments()
    {
        // Arrange
        var mockProcessService = Substitute.For<IProcessService>();
        mockProcessService.OpenFile("C:\\important.txt", "notepad").Returns("opened");
        mockProcessService.OpenFile(Arg.Is<string>(x => x != "C:\\important.txt"), Arg.Any<string>()).Returns("default");

        // Act
        var specificResult = mockProcessService.OpenFile("C:\\important.txt", "notepad");
        var genericResult = mockProcessService.OpenFile("C:\\other.txt", "gedit");

        // Assert
        specificResult.Should().Be("opened");
        genericResult.Should().Be("default");
    }

    #endregion

    #region Mock Verification Tests

    [Fact]
    public void Mock_CanVerifyMethodWasCalled()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.CreateUnconfigured();

        // Act
        mockProcessService.OpenFile("C:\\test.txt", "notepad");

        // Assert
        mockProcessService.Received(1).OpenFile("C:\\test.txt", "notepad");
    }

    [Fact]
    public void Mock_CanVerifyMethodWasNotCalled()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.CreateUnconfigured();

        // Act
        // Don't call OpenFile

        // Assert
        mockProcessService.Received(0).OpenFile(Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public void Mock_CanVerifyMethodWasCalledWithSpecificArguments()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.CreateUnconfigured();
        var filepath = "C:\\document.txt";
        var command = "notepad";

        // Act
        mockProcessService.OpenFile(filepath, command);

        // Assert
        mockProcessService.Received(1).OpenFile(filepath, command);
    }

    [Fact]
    public void Mock_CanVerifyCallCount()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.CreateUnconfigured();

        // Act
        mockProcessService.OpenFile("C:\\file1.txt", "cmd");
        mockProcessService.OpenFile("C:\\file2.txt", "cmd");
        mockProcessService.OpenFile("C:\\file3.txt", "cmd");

        // Assert
        mockProcessService.Received(3).OpenFile(Arg.Any<string>(), Arg.Any<string>());
    }

    #endregion

    #region Different Result Scenarios

    [Theory]
    [InlineData("")]
    [InlineData("Error: Permission denied")]
    [InlineData("Error: File not found")]
    [InlineData("Error: Process failed")]
    public void Mock_WithVariousResults_ReturnsConfiguredValue(string expectedResult)
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.Create(expectedResult);

        // Act
        var result = mockProcessService.OpenFile("C:\\path\\to\\file.txt", "notepad");

        // Assert
        result.Should().Be(expectedResult);
    }

    [Theory]
    [InlineData("C:\\file1.txt", "notepad")]
    [InlineData("C:\\file2.docx", "start")]
    [InlineData("/home/user/file.txt", "gedit")]
    [InlineData("/usr/share/file.pdf", "xdg-open")]
    public void Mock_WithVariousPaths_AcceptsAnyArguments(string filepath, string command)
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.Create();

        // Act
        var result = mockProcessService.OpenFile(filepath, command);

        // Assert
        result.Should().BeEmpty();
    }

    #endregion

    #region Mock Substitution Tests

    [Fact]
    public void Mock_CanReplaceRealImplementation_InDependentClass()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.Create("mocked result");

        // Act
        var result = mockProcessService.OpenFile("C:\\file.txt", "notepad");

        // Assert
        // Verify that we got the mocked result, not the real one
        result.Should().Be("mocked result");
        result.Should().NotBeEmpty();  // Real implementation might return different result
    }

    [Fact]
    public void Mock_CanReturnSuccessForAllCalls()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.Create();  // Default to empty string (success)

        // Act
        var result1 = mockProcessService.OpenFile("C:\\file1.txt", "notepad");
        var result2 = mockProcessService.OpenFile("C:\\file2.txt", "gedit");
        var result3 = mockProcessService.OpenFile("C:\\file3.txt", "start");

        // Assert
        result1.Should().BeEmpty();
        result2.Should().BeEmpty();
        result3.Should().BeEmpty();
    }

    [Fact]
    public void Mock_CanReturnErrorForAllCalls()
    {
        // Arrange
        var errorMessage = "Service unavailable";
        var mockProcessService = IProcessServiceMock.CreateWithError(errorMessage);

        // Act
        var result1 = mockProcessService.OpenFile("C:\\file1.txt", "notepad");
        var result2 = mockProcessService.OpenFile("C:\\file2.txt", "gedit");
        var result3 = mockProcessService.OpenFile("C:\\file3.txt", "start");

        // Assert
        result1.Should().Be(errorMessage);
        result2.Should().Be(errorMessage);
        result3.Should().Be(errorMessage);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public void Mock_CanSimulateSuccessfulFileOpen()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.Create();  // Success scenario

        // Act
        var result = mockProcessService.OpenFile("C:\\Documents\\report.pdf", "start");

        // Assert
        result.Should().BeEmpty();  // Empty = success
    }

    [Fact]
    public void Mock_CanSimulateFailedFileOpen()
    {
        // Arrange
        var mockProcessService = IProcessServiceMock.CreateWithError("File not found");

        // Act
        var result = mockProcessService.OpenFile("C:\\nonexistent\\file.txt", "notepad");

        // Assert
        result.Should().NotBeEmpty();
        result.Should().Be("File not found");
    }

    [Fact]
    public void Mock_SupportsMultipleIndependentMocks()
    {
        // Arrange
        var successMock = IProcessServiceMock.Create();
        var errorMock = IProcessServiceMock.CreateWithError("Error occurred");

        // Act
        var successResult = successMock.OpenFile("C:\\file.txt", "notepad");
        var errorResult = errorMock.OpenFile("C:\\file.txt", "notepad");

        // Assert
        successResult.Should().BeEmpty();
        errorResult.Should().Be("Error occurred");
    }

    #endregion
}
