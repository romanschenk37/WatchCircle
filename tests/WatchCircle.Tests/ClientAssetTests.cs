using Jellyfin.Plugin.WatchCircle.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace WatchCircle.Tests;

public class ClientAssetTests
{
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
