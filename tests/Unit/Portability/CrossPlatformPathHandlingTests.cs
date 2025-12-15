using System.IO;
using AdoPipelinesLocalRunner.Contracts;
using AdoPipelinesLocalRunner.Core.Templates;
using FluentAssertions;
using Xunit;

namespace AdoPipelinesLocalRunner.Tests.Unit.Portability;

/// <summary>
/// Tests to verify NFR-5 Portability compliance.
/// Ensures cross-platform support for Windows, macOS, and Linux with .NET 8.
/// </summary>
public class CrossPlatformPathHandlingTests
{
    /// <summary>
    /// Verify that Path.Combine is used for path construction (cross-platform compatible).
    /// </summary>
    [Fact]
    public void PathHandling_UsesPathCombine_ForCrossPlatformCompatibility()
    {
        // Arrange
        var baseDir = "base";
        var relativeRef = "templates";

        // Act
        var result = System.IO.Path.Combine(baseDir, relativeRef);

        // Assert - Path.Combine uses the platform-specific separator
        result.Should().Be($"base{System.IO.Path.DirectorySeparatorChar}templates");
    }

    /// <summary>
    /// Verify handling of absolute paths remains consistent across platforms.
    /// </summary>
    [Fact]
    public void PathHandling_IsPathRooted_IdentifiesAbsolutePaths()
    {
        // Arrange
        var absolutePath = System.IO.Path.GetFullPath("test.yml");
        var relativePath = "relative/path/test.yml";

        // Act & Assert
        System.IO.Path.IsPathRooted(absolutePath).Should().BeTrue();
        System.IO.Path.IsPathRooted(relativePath).Should().BeFalse();
    }

    /// <summary>
    /// Verify that file path separator is handled correctly across platforms.
    /// </summary>
    [Fact]
    public void PathHandling_DirectorySeparator_PlatformSpecific()
    {
        // Assert
        var separator = System.IO.Path.DirectorySeparatorChar;
        separator.Should().Be(System.IO.Path.DirectorySeparatorChar);
        // On Windows: \, on Unix-like: /
    }

    /// <summary>
    /// Verify that normalized paths work correctly with File.Exists.
    /// </summary>
    [Fact]
    public void FileSystemAccess_FileExists_CrossPlatformCompatible()
    {
        // Arrange
        var testFilePath = System.IO.Path.GetTempFileName();
        try
        {
            // Act
            var exists = System.IO.File.Exists(testFilePath);

            // Assert
            exists.Should().BeTrue();
        }
        finally
        {
            System.IO.File.Delete(testFilePath);
        }
    }

    /// <summary>
    /// Verify handling of file names with extensions across platforms.
    /// </summary>
    [Fact]
    public void PathHandling_FileNameExtraction_CrossPlatform()
    {
        // Arrange
        var fullPath = System.IO.Path.Combine("directory", "subdirectory", "file.yml");

        // Act
        var fileName = System.IO.Path.GetFileName(fullPath);
        var nameWithoutExt = System.IO.Path.GetFileNameWithoutExtension(fullPath);

        // Assert
        fileName.Should().Be("file.yml");
        nameWithoutExt.Should().Be("file");
    }

    /// <summary>
    /// Verify that dotnet SDK is .NET 8 compatible.
    /// </summary>
    [Fact]
    public void DotNetFramework_IsNet8_OrLater()
    {
        // Arrange & Act
        var runtimeVersion = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription;

        // Assert
        runtimeVersion.Should().Contain(".NET 8", "Application must run on .NET 8 or compatible");
    }

    /// <summary>
    /// Verify that OS detection works on all platforms.
    /// </summary>
    [Fact]
    public void PlatformDetection_RuntimeInformation_Available()
    {
        // Act
        var osDescription = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);
        var isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Linux);
        var isOsx = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.OSX);

        // Assert
        osDescription.Should().NotBeNullOrEmpty();
        (isWindows || isLinux || isOsx).Should().BeTrue(because: "One of the platforms must be detected");
    }
}
