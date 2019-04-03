using System;
using System.IO;
using Xunit;
using NetInteractor.Core;
using NetInteractor.Core.Config;

namespace NetInteractor.Test
{
    public class ConfigLoadTest
    {
        private InteractConfig GetConfigFromFile(string fileName)
        {
            return ConfigFactory.DeserializeXml<InteractConfig>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scripts", fileName)));
        }

        [Fact]
        public void TestTargetsCount()
        {
            var config = GetConfigFromFile("TargetsCountTest.config");
            Assert.NotEmpty(config.Targets);
            Assert.Equal(3, config.Targets.Length);
        }

        [Fact]
        public void TestTargetsAttributes()
        {
            var config = GetConfigFromFile("TargetsAttributesTest.config");
            Assert.NotEmpty(config.Targets);
            Assert.Equal(3, config.Targets.Length);

            Assert.Equal("target3", config.DefaultTarget);
            Assert.Equal("target1", config.Targets[0].Name);
            Assert.Equal("target2", config.Targets[1].Name);
            Assert.Equal("target3", config.Targets[2].Name);
        }

        [Fact]
        public void TestTargetActions()
        {
            var config = GetConfigFromFile("TargetActionsTest.config");
            Assert.NotEmpty(config.Targets);
            var target = config.Targets[0];
            Assert.NotEmpty(target.Actions);
            Assert.Equal(4, target.Actions.Count);
            Assert.Equal("GetConfig", target.Actions[0].GetType().Name);
            Assert.Equal("CallConfig", target.Actions[1].GetType().Name);
            Assert.Equal("PostConfig", target.Actions[2].GetType().Name);
            Assert.Equal("IfConfig", target.Actions[3].GetType().Name);
        }
    }
}
