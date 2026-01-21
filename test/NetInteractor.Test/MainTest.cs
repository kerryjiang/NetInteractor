using System;
using System.Collections.Specialized;
using System.IO;
using NetInteractor;
using NetInteractor.Config;
using NetInteractor.WebAccessors;
using Xunit;

namespace NetInteractor.Test
{
    public class MainTest
    {
        
        //[Fact]
        public async void TestShop()
        {
            var config = ConfigFactory.DeserializeXml<InteractConfig>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Scripts", "Shop.config")));

            var executor = new InterationExecutor(new HttpClientWebAccessor());

            var inputs = new NameValueCollection();
            var result = await executor.ExecuteAsync(config, inputs);

            Assert.True(result.Ok, result.Message);
        }
    }
}
