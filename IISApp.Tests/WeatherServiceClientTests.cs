using System.Collections.Generic;
using System.Reflection;
using IISApp.Services;
using Xunit;

namespace IISApp.Tests
{
    public class WeatherServiceClientTests
    {
        [Fact]
        public void ParseTemperatures_ParsesCityAndTemperatureRows()
        {
            var xml = "<?xml version=\"1.0\"?><methodResponse><params><param><value><array><data>" +
                      "<value><string>London: 21.3</string></value>" +
                      "<value><string>Paris: 18.5</string></value>" +
                      "</data></array></value></param></params></methodResponse>";

            var client = new WeatherServiceClient("http://localhost");
            var method = typeof(WeatherServiceClient).GetMethod("ParseTemperatures", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var result = (List<WeatherResult>)method.Invoke(client, new object[] { xml })!;

            Assert.Collection(result,
                r =>
                {
                    Assert.Equal("London", r.City);
                    Assert.Equal("21.3", r.Temperature);
                },
                r =>
                {
                    Assert.Equal("Paris", r.City);
                    Assert.Equal("18.5", r.Temperature);
                });
        }

        [Fact]
        public void ParseTemperatures_ParsesCityNotFoundMessage()
        {
            var xml = "<?xml version=\"1.0\"?><methodResponse><params><param><value><array><data>" +
                      "<value><string>City not found</string></value>" +
                      "</data></array></value></param></params></methodResponse>";

            var client = new WeatherServiceClient("http://localhost");
            var method = typeof(WeatherServiceClient).GetMethod("ParseTemperatures", BindingFlags.NonPublic | BindingFlags.Instance)!;

            var result = (List<WeatherResult>)method.Invoke(client, new object[] { xml })!;

            Assert.Single(result);
            Assert.Equal("City not found", result[0].Message);
        }
    }
}
