// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

namespace AdrPlus.Tests.Infrastructure.Process;

/// <summary>
/// Example tests showing how to use the <see cref="IProcessServiceMock"/>.
/// Remove this file after understanding the usage patterns.
/// </summary>
public class IProcessServiceMockExamples
{
    [Fact]
    public void GivenSuccessfulFileOpen_WhenOpenFileCalled_ThenReturnsEmptyString()
    {
        // Arrange
        var processService = IProcessServiceMock.Create();

        // Act
        var result = processService.OpenFile("C:\\path\\to\\file.txt", "notepad");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GivenFileOpenError_WhenOpenFileCalled_ThenReturnsErrorMessage()
    {
        // Arrange
        var errorMessage = "File not found";
        var processService = IProcessServiceMock.CreateWithError(errorMessage);

        // Act
        var result = processService.OpenFile("C:\\invalid\\path.txt", "notepad");

        // Assert
        result.Should().Be(errorMessage);
    }

    [Fact]
    public void GivenCustomMock_WhenOpenFileCalledWithSpecificArguments_ThenCanVerifyCall()
    {
        // Arrange
        var processService = IProcessServiceMock.CreateUnconfigured();
        processService.OpenFile("C:\\file.txt", "cmd").Returns("success");

        // Act
        var result = processService.OpenFile("C:\\file.txt", "cmd");

        // Assert
        result.Should().Be("success");
        processService.Received(1).OpenFile("C:\\file.txt", "cmd");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Error: Permission denied")]
    [InlineData("Error: File not found")]
    public void GivenVariousResults_WhenOpenFileCalled_ThenReturnsCorrectResult(string expectedResult)
    {
        // Arrange
        var processService = IProcessServiceMock.Create(expectedResult);

        // Act
        var result = processService.OpenFile("C:\\path\\to\\file.txt", "notepad");

        // Assert
        result.Should().Be(expectedResult);
    }
}
