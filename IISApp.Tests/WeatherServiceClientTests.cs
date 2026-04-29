using IISApp.Services;
using Xunit;

namespace IISApp.Tests;

public class WeatherServiceClientTests
{
    [Fact]
    public void ParseTemperatures_ParsesRows()
    {
        var xml = "<?xml version=\"1.0\"?><methodResponse><params><param><value><array><data><value><string>London: 21.3</string></value><value><string>Paris: 18.5</string></value></data></array></value></param></params></methodResponse>";
        var result = WeatherServiceClient.ParseTemperatures(xml);
        Assert.Equal(2, result.Count);
        Assert.Equal("London", result[0].City);
        Assert.False(result[0].IsError);
    }

    [Fact]
    public void ParseTemperatures_ParsesFault()
    {
        var xml = "<?xml version=\"1.0\"?><methodResponse><fault><value><struct><member><name>faultString</name><value><string>Too many params.</string></value></member></struct></value></fault></methodResponse>";
        var result = WeatherServiceClient.ParseTemperatures(xml);
        Assert.Single(result);
        Assert.True(result[0].IsError);
    }
}
