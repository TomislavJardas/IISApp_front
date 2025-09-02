using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using IISApp.Services;
using Xunit;

namespace IISApp.Tests
{
    public class WeatherServiceClientTests
    {
        [Theory]
        [InlineData("en-US")]
        [InlineData("fr-FR")]
        public void ParseTemperatures_UsesInvariantCulture(string culture)
        {
            var xml = @"<?xml version=\"1.0\"?><methodResponse><params><param><value><array><data><value><struct>" +
                      @"<member><name>city</name><value><string>London</string></value></member>" +
                      @"<member><name>temperature</name><value><double>21.3</double></value></member>" +
                      @"</struct></value></data></array></value></param></params></methodResponse>";

            var client = new WeatherServiceClient("http://localhost");
            var method = typeof(WeatherServiceClient).GetMethod("ParseTemperatures", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                var result = (Dictionary<string, double>)method.Invoke(client, new object[] { xml })!;
                Assert.Equal(21.3, result["London"]);
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    }
}
