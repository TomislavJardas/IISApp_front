using System.Collections.Generic;
using System.Reflection;
using IISApp.Services;
using Xunit;

namespace IISApp.Tests
{
    public class WeatherServiceClientTests
    {
        [Fact]
        public void ParseTemperatures_ParsesMultipleCityRows()
        {
            var xml = "<?xml version=\"1.0\"?><methodResponse><params><param><value><array><data>" +
                      "<value><string>London: 21.3</string></value>" +
                      "<value><string>Paris: 18.5</string></value>" +
                      "</data></array></value></param></params></methodResponse>";

            var result = Parse(xml);

            Assert.Collection(result,
                r =>
                {
                    Assert.Equal("London", r.City);
                    Assert.Equal("21.3", r.Temperature);
                    Assert.False(r.IsError);
                },
                r =>
                {
                    Assert.Equal("Paris", r.City);
                    Assert.Equal("18.5", r.Temperature);
                    Assert.False(r.IsError);
                });
        }

        [Fact]
        public void ParseTemperatures_ParsesCityNotFoundMessage()
        {
            var xml = "<?xml version=\"1.0\"?><methodResponse><params><param><value><array><data>" +
                      "<value><string>City not found</string></value>" +
                      "</data></array></value></param></params></methodResponse>";

            var result = Parse(xml);

            Assert.Single(result);
            Assert.Equal("City not found", result[0].Message);
            Assert.False(result[0].IsError);
        }

        [Fact]
        public void ParseTemperatures_ParsesBackendErrorMessage()
        {
            var xml = "<?xml version=\"1.0\"?><methodResponse><params><param><value><array><data>" +
                      "<value><string>Error retrieving temperature: timeout</string></value>" +
                      "</data></array></value></param></params></methodResponse>";

            var result = Parse(xml);

            Assert.Single(result);
            Assert.True(result[0].IsError);
            Assert.Contains("Error retrieving temperature", result[0].Message);
        }

        [Fact]
        public void ParseTemperatures_ParsesXmlRpcFault()
        {
            var xml = "<?xml version=\"1.0\"?><methodResponse><fault><value><struct>" +
                      "<member><name>faultCode</name><value><int>4</int></value></member>" +
                      "<member><name>faultString</name><value><string>Too many params.</string></value></member>" +
                      "</struct></value></fault></methodResponse>";

            var result = Parse(xml);

            Assert.Single(result);
            Assert.True(result[0].IsError);
            Assert.Contains("Too many params", result[0].Message);
        }

        private static List<WeatherResult> Parse(string xml)
        {
            var client = new WeatherServiceClient("http://localhost");
            var method = typeof(WeatherServiceClient).GetMethod("ParseTemperatures", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return (List<WeatherResult>)method.Invoke(client, new object[] { xml })!;
        }
    }
}
