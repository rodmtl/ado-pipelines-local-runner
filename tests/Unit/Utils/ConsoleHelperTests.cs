using AdoPipelinesLocalRunner.Utils;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Utils;

/// <summary>
/// Tests for ConsoleHelper utility class.
/// </summary>
public class ConsoleHelperTests
{
    [Fact]
    public void WriteHeader_DoesNotThrowException()
    {
        // Act - just ensure no exception is thrown
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteHeader("Test App", "1.0.0"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteSuccess_DoesNotThrowException()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteSuccess("Test success message"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteError_DoesNotThrowException()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteError("Test error message"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteWarning_DoesNotThrowException()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteWarning("Test warning message"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteInfo_DoesNotThrowException()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteInfo("Test info message"));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteColored_DoesNotThrowException()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteColored("Colored text", ConsoleColor.Red));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteLineColored_DoesNotThrowException()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteLineColored("Colored line", ConsoleColor.Green));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteColored_WithAllColors_DoesNotThrow()
    {
        // Arrange
        var colors = new[]
        {
            ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.Cyan,
            ConsoleColor.DarkBlue, ConsoleColor.DarkCyan, ConsoleColor.DarkGray,
            ConsoleColor.DarkGreen, ConsoleColor.DarkMagenta, ConsoleColor.DarkRed,
            ConsoleColor.DarkYellow, ConsoleColor.Gray, ConsoleColor.Green,
            ConsoleColor.Magenta, ConsoleColor.Red, ConsoleColor.White,
            ConsoleColor.Yellow
        };

        // Act & Assert
        foreach (var color in colors)
        {
            var exception = Record.Exception(() =>
                ConsoleHelper.WriteColored($"Test {color}", color));
            Assert.Null(exception);
        }
    }

    [Fact]
    public void WriteHeader_WithEmptyValues_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteHeader("", ""));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void WriteHeader_WithLongAppName_DoesNotThrow()
    {
        // Act
        var exception = Record.Exception(() =>
            ConsoleHelper.WriteHeader("Very Long Application Name That Exceeds Normal Length", "1.0.0"));

        // Assert
        Assert.Null(exception);
    }
}
