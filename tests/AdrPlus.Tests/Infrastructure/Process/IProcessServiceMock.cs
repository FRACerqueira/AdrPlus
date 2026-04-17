// ***************************************************************************************
// MIT LICENCE
// The maintenance and evolution is maintained by the AdrPlus project under MIT license
// ***************************************************************************************

using AdrPlus.Infrastructure.Process;

namespace AdrPlus.Tests.Infrastructure.Process;

/// <summary>
/// Mock implementation of <see cref="IProcessService"/> for testing.
/// </summary>
internal static class IProcessServiceMock
{
    /// <summary>
    /// Creates a mock <see cref="IProcessService"/> using NSubstitute.
    /// </summary>
    /// <param name="openFileResult">Optional result to return when OpenFile is called. Defaults to empty string (success).</param>
    /// <returns>A mocked instance of <see cref="IProcessService"/>.</returns>
    public static IProcessService Create(string openFileResult = "")
    {
        var mock = Substitute.For<IProcessService>();
        mock.OpenFile(Arg.Any<string>(), Arg.Any<string>()).Returns(openFileResult);
        return mock;
    }

    /// <summary>
    /// Creates a mock <see cref="IProcessService"/> that returns an error when OpenFile is called.
    /// </summary>
    /// <param name="errorMessage">The error message to return.</param>
    /// <returns>A mocked instance of <see cref="IProcessService"/> configured to return an error.</returns>
    public static IProcessService CreateWithError(string errorMessage)
    {
        return Create(errorMessage);
    }

    /// <summary>
    /// Creates a mock <see cref="IProcessService"/> that can be configured with specific behaviors.
    /// </summary>
    /// <returns>An unconfigured mocked instance of <see cref="IProcessService"/>.</returns>
    public static IProcessService CreateUnconfigured()
    {
        return Substitute.For<IProcessService>();
    }
}
