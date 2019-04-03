using System;
using System.Collections.Specialized;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using NetInteractor.Core;
using NetInteractor.Core.Config;
using NetInteractor.Core.WebAccessors;
using Xunit;

namespace NetInteractor.Test
{
    public class MainTest
    {
        
        [Fact]
        public async void TestShop()
        {
            var services = new ServiceCollection();
            services.AddTransient<IWebAccessor, HttpWebAccessor>();

            var config = ConfigFactory.DeserializeXml<InteractConfig>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Config", "Shop.config")));

            var executor = new InterationExecutor(services.BuildServiceProvider());

            var inputs = new NameValueCollection();
            var result = await executor.ExecuteAsync(config, inputs);

            Assert.True(result.Ok, result.Message);
        }
    }
}
