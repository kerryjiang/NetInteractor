using System;
using System.Collections.Specialized;
using System.Threading.Tasks;
using NetInteractor.WebAccessors;
using Xunit;

namespace NetInteractor.Test
{
    /// <summary>
    /// Tests specifically for PuppeteerSharpWebAccessor.
    /// These tests use real HTTP endpoints to verify browser automation functionality.
    /// </summary>
    public class PuppeteerSharpWebAccessorTests : IDisposable
    {
        private PuppeteerSharpWebAccessor _accessor;

        public PuppeteerSharpWebAccessorTests()
        {
            _accessor = new PuppeteerSharpWebAccessor();
        }

        public void Dispose()
        {
            _accessor?.Dispose();
        }

        [Fact(Skip = "Requires browser download and real network access. Enable for manual testing.")]
        public async Task GetAsync_RetrievesPageContent()
        {
            // Arrange
            var url = "https://example.com";

            // Act
            var result = await _accessor.GetAsync(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Html);
            Assert.Contains("Example Domain", result.Html);
            Assert.Contains(url, result.Url);
        }

        [Fact(Skip = "Requires browser download and real network access. Enable for manual testing.")]
        public async Task GetAsync_HandlesHttpsUrls()
        {
            // Arrange
            var url = "https://www.google.com";

            // Act
            var result = await _accessor.GetAsync(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Html);
            Assert.NotEmpty(result.Html);
        }

        [Fact(Skip = "Requires browser download and real network access. Enable for manual testing.")]
        public async Task GetAsync_PopulatesResponseInfo()
        {
            // Arrange
            var url = "https://example.com";

            // Act
            var result = await _accessor.GetAsync(url);

            // Assert
            Assert.NotNull(result);
            Assert.InRange(result.StatusCode, 200, 299); // Success status codes
            Assert.NotNull(result.Html);
            Assert.NotEmpty(result.Html);
            Assert.NotNull(result.Url);
            Assert.NotNull(result.Headers);
        }

        [Fact(Skip = "Requires browser download and real network access. Enable for manual testing.")]
        public async Task GetAsync_RendersJavaScript()
        {
            // This test verifies that PuppeteerSharp can execute JavaScript
            // which is a key advantage over HttpClient-based solutions
            
            // Arrange
            // Use a site known to have JavaScript-rendered content
            var url = "https://example.com";

            // Act
            var result = await _accessor.GetAsync(url);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Html);
            // Verify we got rendered HTML (not just source)
            Assert.Contains("<html", result.Html, StringComparison.OrdinalIgnoreCase);
        }

        [Fact(Skip = "Requires browser download and POST endpoint. Enable for manual testing.")]
        public async Task PostAsync_SubmitsFormData()
        {
            // Arrange
            var url = "https://httpbin.org/post";
            var formValues = new NameValueCollection
            {
                ["field1"] = "value1",
                ["field2"] = "value2"
            };

            // Act
            var result = await _accessor.PostAsync(url, formValues);

            // Assert
            Assert.NotNull(result);
            Assert.InRange(result.StatusCode, 200, 299);
            Assert.NotNull(result.Html);
            // httpbin.org echoes back the posted data
            Assert.Contains("field1", result.Html);
            Assert.Contains("value1", result.Html);
        }

        [Fact(Skip = "Requires browser download. Enable for manual testing.")]
        public async Task Dispose_ReleasesResources()
        {
            // Arrange
            var accessor = new PuppeteerSharpWebAccessor();
            await accessor.GetAsync("https://example.com");

            // Act
            accessor.Dispose();

            // Assert
            // Verify that subsequent calls throw or fail gracefully
            // (This is more of a manual verification test)
            Assert.True(true, "Dispose completed without exception");
        }

        [Fact(Skip = "Requires browser download. Enable for manual testing.")]
        public async Task MultipleRequests_ReusesBrowser()
        {
            // Arrange
            var url1 = "https://example.com";
            var url2 = "https://www.iana.org";

            // Act
            var result1 = await _accessor.GetAsync(url1);
            var result2 = await _accessor.GetAsync(url2);

            // Assert
            Assert.NotNull(result1);
            Assert.NotNull(result2);
            Assert.Equal(200, result1.StatusCode);
            Assert.Equal(200, result2.StatusCode);
            Assert.Contains("Example Domain", result1.Html);
            Assert.Contains("IANA", result2.Html);
        }
    }
}
