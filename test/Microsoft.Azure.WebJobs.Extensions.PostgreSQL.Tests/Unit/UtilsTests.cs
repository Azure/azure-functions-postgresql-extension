// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Common;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Microsoft.Azure.WebJobs.Extensions.PostgreSql.Tests.Unit
{
    /// <summary>
    /// Unit tests for utility methods.
    /// </summary>
    public class UtilsTests
    {
        private const string TestEnvVar = "AzureFunctionsPostgreSqlBindingsTestEnvVar";
        private const string TestConfigSetting = "AzureFunctionsPostgreSqlBindingsTestConfigSetting";

        /// <summary>
        /// Tests whether environment variables can be correctly converted to Boolean values.
        /// </summary>
        /// <param name="value">The value of the environment variable to be tested.</param>
        /// <param name="expectedValue">The expected Boolean result of the conversion.</param>
        /// <param name="defaultValue">The default Boolean value to be used if the environment variable is not set or cannot be converted.</param>
        [Theory]
        [InlineData(null, false)] // Doesn't exist, get default value
        [InlineData(null, true, true)] // Doesn't exist, get default value (set explicitly)
        [InlineData("1", true)]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("yes", true)]
        [InlineData("YES", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("FALSE", false)]
        [InlineData("no", false)]
        [InlineData("NO", false)]
        [InlineData("2", false)]
        [InlineData("SomeOtherValue", false)]
        public void GetEnvironmentVariableAsBool(string value, bool expectedValue, bool defaultValue = false)
        {
            Environment.SetEnvironmentVariable(TestEnvVar, value?.ToString());
            bool actualValue = Utils.GetEnvironmentVariableAsBool(TestEnvVar, defaultValue);
            Assert.Equal(expectedValue, actualValue);
        }

        /// <summary>
        /// Tests whether configuration settings can be correctly converted to Boolean values.
        /// </summary>
        /// <param name="value">The value of the configuration setting to be tested.</param>
        /// <param name="expectedValue">The expected Boolean result of the conversion.</param>
        /// <param name="defaultValue">The default Boolean value to be used if the configuration setting is not set or cannot be converted.</param>
        [Theory]
        [InlineData(null, false)] // Doesn't exist, get default value
        [InlineData(null, true, true)] // Doesn't exist, get default value (set explicitly)
        [InlineData("1", true)]
        [InlineData("true", true)]
        [InlineData("TRUE", true)]
        [InlineData("yes", true)]
        [InlineData("YES", true)]
        [InlineData("0", false)]
        [InlineData("false", false)]
        [InlineData("FALSE", false)]
        [InlineData("no", false)]
        [InlineData("NO", false)]
        [InlineData("2", false)]
        [InlineData("SomeOtherValue", false)]
        public void GetConfigSettingAsBool(string value, bool expectedValue, bool defaultValue = false)
        {
            var config = new TestConfiguration();
            IConfigurationSection configSection = new TestConfigurationSection();
            configSection.Value = value;
            config.AddSection(TestConfigSetting, configSection);
            bool actualValue = Utils.GetConfigSettingAsBool(TestConfigSetting, config, defaultValue);
            Assert.Equal(expectedValue, actualValue);
        }
    }
}
