#nullable enable
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Moq;
using NetInteractor.Mcp;
using NetInteractor.Test.TestWebApp;
using NetInteractor.WebAccessors;
using Xunit;

namespace NetInteractor.Mcp.Test
{
    public class NetInteractorToolTests : IClassFixture<TestWebApplicationFixture>
    {
        private readonly TestWebApplicationFactory _factory;
        private readonly string _baseUrl;
        private readonly NetInteractorTool _tool;
        private readonly Mock<McpServer> _mockServer;

        public NetInteractorToolTests(TestWebApplicationFixture fixture)
        {
            _factory = fixture.Factory;
            _baseUrl = fixture.Factory.ServerUrl;
            // Use HttpClientWebAccessor for tests since Playwright requires browser installation
            _tool = new NetInteractorTool(new HttpClientWebAccessor());
            _mockServer = new Mock<McpServer>();
        }

        private RequestContext<CallToolRequestParams> CreateRequestContext(IDictionary<string, JsonElement>? arguments = null)
        {
            var jsonRpcRequest = new JsonRpcRequest
            {
                Id = new RequestId("test-id"),
                Method = "tools/call",
                Params = null
            };

            var context = new RequestContext<CallToolRequestParams>(_mockServer.Object, jsonRpcRequest)
            {
                Params = new CallToolRequestParams
                {
                    Name = "netinteractor_execute_script",
                    Arguments = arguments
                }
            };

            return context;
        }

        private static IDictionary<string, JsonElement> CreateArguments(string script, IDictionary<string, string>? inputs = null, string? target = null)
        {
            var args = new Dictionary<string, JsonElement>
            {
                ["script"] = JsonSerializer.SerializeToElement(script)
            };

            if (inputs != null)
            {
                args["inputs"] = JsonSerializer.SerializeToElement(inputs);
            }

            if (target != null)
            {
                args["target"] = JsonSerializer.SerializeToElement(target);
            }

            return args;
        }

        [Fact]
        public async Task InvokeAsync_SimpleGetScript_ExtractsTitle()
        {
            // Arrange
            var script = $@"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='{_baseUrl}/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            var context = CreateRequestContext(CreateArguments(script));

            // Act
            var result = await _tool.InvokeAsync(context, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.NotNull(result.StructuredContent);
            Assert.Equal("Welcome to Test Shop", result.StructuredContent["title"]?.ToString());
        }

        [Fact]
        public async Task InvokeAsync_WithInputs_ExtractsTitle()
        {
            // Arrange
            var script = @"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='$(BaseUrl)/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            var inputs = new Dictionary<string, string> { { "BaseUrl", _baseUrl } };
            var context = CreateRequestContext(CreateArguments(script, inputs));

            // Act
            var result = await _tool.InvokeAsync(context, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.NotNull(result.StructuredContent);
            Assert.Equal("Welcome to Test Shop", result.StructuredContent["title"]?.ToString());
        }

        [Fact]
        public async Task InvokeAsync_WithSpecificTarget_ExecutesTarget()
        {
            // Arrange
            var script = $@"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='{_baseUrl}/'>
                        <output name='title' xpath='//h1' attr='text()' />
                    </get>
                </target>
                <target name='Products'>
                    <get url='{_baseUrl}/products'>
                        <output name='productTitle' xpath='//h1' attr='text()' />
                    </get>
                </target>
            </InteractConfig>";

            var context = CreateRequestContext(CreateArguments(script, target: "Products"));

            // Act
            var result = await _tool.InvokeAsync(context, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.NotNull(result.StructuredContent);
            Assert.Equal("Products", result.StructuredContent["productTitle"]?.ToString());
        }

        [Fact]
        public async Task InvokeAsync_InvalidScript_ReturnsError()
        {
            // Arrange
            var invalidScript = @"<Invalid></Script>";
            var context = CreateRequestContext(CreateArguments(invalidScript));

            // Act
            var result = await _tool.InvokeAsync(context, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsError);
            Assert.NotNull(result.Content);
            Assert.NotEmpty(result.Content);
        }

        [Fact]
        public async Task InvokeAsync_MissingScript_ReturnsError()
        {
            // Arrange - No script argument
            var context = CreateRequestContext(new Dictionary<string, JsonElement>());

            // Act
            var result = await _tool.InvokeAsync(context, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.IsError);
            Assert.NotNull(result.Content);
            Assert.NotEmpty(result.Content);
            var textContent = result.Content[0] as TextContentBlock;
            Assert.NotNull(textContent);
            Assert.Contains("script", textContent.Text.ToLower());
        }

        [Fact]
        public async Task InvokeAsync_MultipleOutputs_ExtractsAllValues()
        {
            // Arrange
            var script = $@"<InteractConfig defaultTarget='Main'>
                <target name='Main'>
                    <get url='{_baseUrl}/data'>
                        <output name='title' xpath='//h1' attr='text()' />
                        <output name='imageSrc' xpath='//img' attr='src' />
                    </get>
                </target>
            </InteractConfig>";

            var context = CreateRequestContext(CreateArguments(script));

            // Act
            var result = await _tool.InvokeAsync(context, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.False(result.IsError);
            Assert.NotNull(result.StructuredContent);
            Assert.Equal("Data Extraction Test", result.StructuredContent["title"]?.ToString());
            Assert.Equal("/images/test.png", result.StructuredContent["imageSrc"]?.ToString());
        }

        [Fact]
        public void ProtocolTool_ReturnsValidTool()
        {
            // Act
            var protocolTool = _tool.ProtocolTool;

            // Assert
            Assert.NotNull(protocolTool);
            Assert.Equal("netinteractor_execute_script", protocolTool.Name);
            Assert.NotNull(protocolTool.Description);
            Assert.NotNull(protocolTool.OutputSchema);
        }

        [Fact]
        public void Metadata_ReturnsEmptyList()
        {
            // Act
            var metadata = _tool.Metadata;

            // Assert - Metadata returns empty list, output schema is on ProtocolTool.OutputSchema
            Assert.NotNull(metadata);
            Assert.Empty(metadata);
        }
    }

    /// <summary>
    /// Shared test fixture that provides a single TestWebApplicationFactory instance for all tests.
    /// </summary>
    public class TestWebApplicationFixture : System.IDisposable
    {
        public TestWebApplicationFactory Factory { get; }

        public TestWebApplicationFixture()
        {
            Factory = new TestWebApplicationFactory(ServerMode.Kestrel);
        }

        public void Dispose()
        {
            Factory?.Dispose();
        }
    }
}
