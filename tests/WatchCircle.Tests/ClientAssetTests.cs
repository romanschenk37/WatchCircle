using Jellyfin.Plugin.WatchCircle.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace WatchCircle.Tests;

public class ClientAssetTests
{
    [Theory]
    [InlineData("components/cleanup/cleanup.js", "application/javascript", "window.WatchCircleCleanup")]
    [InlineData("components/cleanup/cleanup.css", "text/css", ".wc-clean-marker")]
    public void CleanupAssetsAreEmbeddedAndServed(string path, string type, string expected)
    {
        var controller = new WatchCircleClientController(null!, null!, null!, null!, null!, NullLogger<WatchCircleClientController>.Instance);
        var response = Assert.IsType<FileStreamResult>(controller.GetClientAsset(path));
        Assert.Equal(type, response.ContentType);
        using var reader = new StreamReader(response.FileStream);
        Assert.Contains(expected, reader.ReadToEnd());
    }

    [Fact]
    public void TranslationDependencyIsServedFromTheBuiltPlugin()
    {
        var controller = new WatchCircleClientController(null!, null!, null!, null!, null!, NullLogger<WatchCircleClientController>.Instance);
        var response = Assert.IsType<FileStreamResult>(controller.GetClientAsset("utils/i18n.js"));
        Assert.Equal("application/javascript", response.ContentType);
        using var reader = new StreamReader(response.FileStream);
        var script = reader.ReadToEnd();
        Assert.Contains("window.WatchCircleI18n", script);
        Assert.Contains("Begonnene Serien", script);
    }
}
