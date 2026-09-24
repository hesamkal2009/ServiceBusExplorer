using System;
using FluentAssertions;
using ServiceBusExplorer.UIHelpers;
using Xunit;

namespace ServiceBusExplorer.Tests.UIHelpers
{
    public class ThemeManagerTests
    {
        [Theory]
        [InlineData("Light", "Light")]
        [InlineData("Dark", "Dark")]
        public void ParseConfiguredMode_ReturnsExpectedMode(string configuredMode, string expectedMode)
        {
            ThemeManager.ParseConfiguredMode(configuredMode).Should().Be((ThemeMode)Enum.Parse(typeof(ThemeMode), expectedMode));
        }
    }
}