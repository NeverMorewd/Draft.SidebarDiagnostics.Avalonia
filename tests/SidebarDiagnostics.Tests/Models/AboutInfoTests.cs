using SidebarDiagnostics.App.Models;
using Xunit;

namespace SidebarDiagnostics.Tests.Models;

public sealed class AboutInfoTests
{
    [Fact]
    public void CreateReportsProjectAndRuntimeMetadata()
    {
        var info = AboutInfo.Create();

        Assert.Equal("https://github.com/NeverMorewd/SidebarDiagnostics.Avalonia", info.RepositoryUrl);
        Assert.Equal($"{info.RepositoryUrl}/issues", info.IssuesUrl);
        Assert.Equal("GPL-3.0-only", info.License);
        Assert.Contains("NeverMorewd", info.Author, StringComparison.Ordinal);
        Assert.NotEmpty(info.ApplicationVersion);
        Assert.NotEmpty(info.AvaloniaVersion);
        Assert.StartsWith(".NET", info.DotNetVersion, StringComparison.Ordinal);
    }
}
