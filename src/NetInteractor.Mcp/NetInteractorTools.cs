using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using NetInteractor.WebAccessors;

namespace NetInteractor.Mcp
{
    /// <summary>
    /// MCP Server Tool for NetInteractor web automation.
    /// This tool enables AI agents to execute web interactions and automation scripts using InteractExecutor.
    /// 
    /// NetInteractor scripts are XML-based configurations that define web automation workflows.
    /// 
    /// ## Script Structure
    /// 
    /// A script consists of:
    /// - **InteractConfig**: The root element with an optional 'defaultTarget' attribute
    /// - **target**: Named workflow targets containing action sequences
    /// 
    /// ## Supported Actions
    /// 
    /// ### 1. get - HTTP GET Request
    /// Fetches a web page and optionally extracts data.
    /// ```xml
    /// &lt;get url="https://example.com"&gt;
    ///     &lt;output name="title" xpath="//h1" attr="text()" /&gt;
    ///     &lt;output name="link" xpath="//a[@class='main']" attr="href" /&gt;
    /// &lt;/get&gt;
    /// ```
    /// Attributes:
    /// - url: The URL to fetch (supports $(InputName) variable substitution)
    /// - expectedHttpStatusCodes: Comma-separated list of expected HTTP status codes
    /// 
    /// ### 2. post - HTTP POST Form Submission
    /// Submits a form on the current page.
    /// ```xml
    /// &lt;post formIndex="0"&gt;
    ///     &lt;formValue name="username" value="$(Username)" /&gt;
    ///     &lt;formValue name="password" value="$(Password)" /&gt;
    ///     &lt;output name="result" xpath="//div[@class='message']" attr="text()" /&gt;
    /// &lt;/post&gt;
    /// ```
    /// Attributes (use one to identify the form):
    /// - formIndex: Zero-based index of the form on the page
    /// - formName: The name attribute of the form
    /// - action: The action URL of the form
    /// - clientID: The id attribute of the form
    /// 
    /// ### 3. if - Conditional Execution
    /// Executes a child action only if a condition is met.
    /// ```xml
    /// &lt;if property="$(ShouldLogin)" value="true"&gt;
    ///     &lt;call target="Login" /&gt;
    /// &lt;/if&gt;
    /// ```
    /// Attributes:
    /// - property: The property/input to check
    /// - value: The expected value
    /// 
    /// ### 4. call - Call Another Target
    /// Executes another named target.
    /// ```xml
    /// &lt;call target="ExtractData" /&gt;
    /// ```
    /// Attributes:
    /// - target: Name of the target to call
    /// 
    /// ## Output Extraction
    /// 
    /// The output element extracts data from HTML responses:
    /// ```xml
    /// &lt;output name="outputName" xpath="//selector" attr="text()" /&gt;
    /// &lt;output name="price" xpath="//span[@class='price']" regex="\\$([\\d.]+)" /&gt;
    /// ```
    /// Attributes:
    /// - name: Name of the output variable
    /// - xpath: XPath expression to select element
    /// - attr: Attribute to extract ('text()' for inner text, or attribute name like 'href')
    /// - regex: Optional regex to extract from the selected content
    /// - isMultipleValue: If true, extracts all matching values
    /// - expectedValue: Validates the extracted value matches expected
    /// 
    /// ## Variable Substitution
    /// 
    /// Use $(VariableName) syntax to reference input values:
    /// ```xml
    /// &lt;get url="$(BaseUrl)/products" /&gt;
    /// ```
    /// 
    /// ## Example Complete Script
    /// 
    /// ```xml
    /// &lt;InteractConfig defaultTarget="Main"&gt;
    ///     &lt;target name="Main"&gt;
    ///         &lt;get url="$(BaseUrl)/"&gt;
    ///             &lt;output name="title" xpath="//h1" attr="text()" /&gt;
    ///         &lt;/get&gt;
    ///         &lt;if property="$(ShouldLogin)" value="true"&gt;
    ///             &lt;call target="Login" /&gt;
    ///         &lt;/if&gt;
    ///     &lt;/target&gt;
    ///     &lt;target name="Login"&gt;
    ///         &lt;get url="$(BaseUrl)/login" /&gt;
    ///         &lt;post formIndex="0"&gt;
    ///             &lt;formValue name="username" value="$(Username)" /&gt;
    ///             &lt;formValue name="password" value="$(Password)" /&gt;
    ///         &lt;/post&gt;
    ///     &lt;/target&gt;
    /// &lt;/InteractConfig&gt;
    /// ```
    /// </summary>
    public class NetInteractorTool : McpServerTool
    {
        private readonly IWebAccessor _webAccessor;
        private readonly Tool _protocolTool;

        /// <summary>
        /// Initializes a new instance of the NetInteractorTool class with the default PlaywrightWebAccessor.
        /// </summary>
        public NetInteractorTool()
            : this(new PlaywrightWebAccessor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the NetInteractorTool class with a custom web accessor.
        /// </summary>
        /// <param name="webAccessor">The web accessor to use for HTTP requests.</param>
        public NetInteractorTool(IWebAccessor webAccessor)
        {
            _webAccessor = webAccessor ?? throw new ArgumentNullException(nameof(webAccessor));

            // Define the input schema as JSON
            var inputSchemaJson = """
            {
                "type": "object",
                "properties": {
                    "script": {
                        "type": "string",
                        "description": "XML script defining the web automation workflow. Structure:\n<InteractConfig defaultTarget='TargetName'>\n    <target name='TargetName'>\n        <!-- Actions: get, post, if, call -->\n    </target>\n</InteractConfig>\n\nActions:\n- <get url='...'><output name='...' xpath='...' attr='text()'/></get>\n- <post formIndex='0'><formValue name='...' value='...'/></post>\n- <if property='$(Var)' value='...'><call target='...'/></if>\n- <call target='TargetName'/>\n\nVariables: Use $(InputName) syntax for input substitution."
                    },
                    "inputs": {
                        "type": "string",
                        "description": "Comma-separated key=value pairs for script variable substitution. Example: 'BaseUrl=https://example.com,Username=admin,Password=secret'. These values replace $(Key) placeholders in the script."
                    },
                    "target": {
                        "type": "string",
                        "description": "Name of the target to execute. If omitted, uses the defaultTarget specified in InteractConfig."
                    }
                },
                "required": ["script"]
            }
            """;

            // Define the protocol tool representation
            _protocolTool = new Tool
            {
                Name = "netinteractor_execute_script",
                Description = "Executes a NetInteractor XML script for web automation. Supports GET requests, form POST submissions, conditional logic (if), and calling other targets. Use XPath expressions to extract data from HTML. Variables use $(Name) syntax.",
                InputSchema = JsonDocument.Parse(inputSchemaJson).RootElement
            };
        }

        /// <summary>
        /// Gets the protocol tool representation.
        /// </summary>
        public override Tool ProtocolTool => _protocolTool;

        /// <summary>
        /// Gets the tool metadata.
        /// </summary>
        public override IReadOnlyList<object> Metadata => Array.Empty<object>();

        /// <summary>
        /// Invokes the tool with the given parameters.
        /// </summary>
        public override async ValueTask<CallToolResult> InvokeAsync(
            RequestContext<CallToolRequestParams> request,
            CancellationToken cancellationToken = default)
        {
            var arguments = request.Params?.Arguments;

            string? script = null;
            string? inputs = null;
            string? target = null;

            if (arguments != null)
            {
                if (arguments.TryGetValue("script", out var scriptValue) && scriptValue.ValueKind != JsonValueKind.Undefined && scriptValue.ValueKind != JsonValueKind.Null)
                    script = scriptValue.GetString();
                if (arguments.TryGetValue("inputs", out var inputsValue) && inputsValue.ValueKind != JsonValueKind.Undefined && inputsValue.ValueKind != JsonValueKind.Null)
                    inputs = inputsValue.GetString();
                if (arguments.TryGetValue("target", out var targetValue) && targetValue.ValueKind != JsonValueKind.Undefined && targetValue.ValueKind != JsonValueKind.Null)
                    target = targetValue.GetString();
            }

            if (string.IsNullOrEmpty(script))
            {
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = "Error: 'script' parameter is required." }
                    },
                    IsError = true
                };
            }

            var result = await ExecuteScriptAsync(script, inputs, target);

            var outputText = result.Ok
                ? $"Success: {result.Message ?? "Script executed successfully."}\nOutputs: {FormatOutputs(result.Outputs)}"
                : $"Failed: {result.Message}";

            return new CallToolResult
            {
                Content = new List<ContentBlock>
                {
                    new TextContentBlock { Text = outputText }
                },
                IsError = !result.Ok
            };
        }

        /// <summary>
        /// Executes a NetInteractor XML script for web automation.
        /// 
        /// The script defines a workflow with actions like:
        /// - get: Fetch a URL and extract data using XPath
        /// - post: Submit forms with field values
        /// - if: Conditional execution based on input values
        /// - call: Call another named target
        /// 
        /// See the class documentation for full script syntax and examples.
        /// </summary>
        /// <param name="script">The XML script defining the web interaction workflow.</param>
        /// <param name="inputs">Optional comma-separated key=value pairs for script inputs (e.g., "BaseUrl=https://example.com,Username=user").</param>
        /// <param name="target">Optional target name to execute. If not specified, the default target will be used.</param>
        /// <returns>The InteractionResult containing Ok status, Message, and extracted Outputs.</returns>
        public async Task<InteractionResult> ExecuteScriptAsync(
            string script,
            string? inputs = null,
            string? target = null)
        {
            try
            {
                var executor = new InterationExecutor(_webAccessor);
                var inputValues = ParseInputs(inputs);
                return await executor.ExecuteAsync(script, inputValues, target);
            }
            catch (Exception ex)
            {
                return new InteractionResult
                {
                    Ok = false,
                    Message = $"Script execution failed: {ex.Message}",
                    Exception = ex
                };
            }
        }

        /// <summary>
        /// Gets the input metadata for the ExecuteScript tool.
        /// </summary>
        /// <returns>A dictionary describing the input parameters.</returns>
        public static Dictionary<string, ToolParameterMetadata> GetInputMetadata()
        {
            return new Dictionary<string, ToolParameterMetadata>
            {
                ["script"] = new ToolParameterMetadata
                {
                    Name = "script",
                    Description = @"XML script defining the web automation workflow. Structure:
<InteractConfig defaultTarget='TargetName'>
    <target name='TargetName'>
        <!-- Actions: get, post, if, call -->
    </target>
</InteractConfig>

Actions:
- <get url='...'><output name='...' xpath='...' attr='text()'/></get>
- <post formIndex='0'><formValue name='...' value='...'/></post>
- <if property='$(Var)' value='...'><call target='...'/></if>
- <call target='TargetName'/>

Variables: Use $(InputName) syntax for input substitution.",
                    Type = "string",
                    Required = true
                },
                ["inputs"] = new ToolParameterMetadata
                {
                    Name = "inputs",
                    Description = "Comma-separated key=value pairs for script variable substitution. Example: 'BaseUrl=https://example.com,Username=admin,Password=secret'. These values replace $(Key) placeholders in the script.",
                    Type = "string",
                    Required = false
                },
                ["target"] = new ToolParameterMetadata
                {
                    Name = "target",
                    Description = "Name of the target to execute. If omitted, uses the defaultTarget specified in InteractConfig. Targets are named workflow steps that can call each other.",
                    Type = "string",
                    Required = false
                }
            };
        }

        /// <summary>
        /// Gets the output metadata for the ExecuteScript tool.
        /// </summary>
        /// <returns>A dictionary describing the output properties of InteractionResult.</returns>
        public static Dictionary<string, ToolParameterMetadata> GetOutputMetadata()
        {
            return new Dictionary<string, ToolParameterMetadata>
            {
                ["Ok"] = new ToolParameterMetadata
                {
                    Name = "Ok",
                    Description = "True if the script execution completed successfully, false if any action failed",
                    Type = "boolean",
                    Required = true
                },
                ["Message"] = new ToolParameterMetadata
                {
                    Name = "Message",
                    Description = "Descriptive message about the result, especially useful for errors",
                    Type = "string",
                    Required = false
                },
                ["Outputs"] = new ToolParameterMetadata
                {
                    Name = "Outputs",
                    Description = "NameValueCollection containing all extracted output values defined by <output> elements in the script",
                    Type = "NameValueCollection",
                    Required = false
                },
                ["Target"] = new ToolParameterMetadata
                {
                    Name = "Target",
                    Description = "The name of the next target to execute (used for workflow chaining)",
                    Type = "string",
                    Required = false
                },
                ["Exception"] = new ToolParameterMetadata
                {
                    Name = "Exception",
                    Description = "Exception details if an error occurred during execution",
                    Type = "Exception",
                    Required = false
                }
            };
        }

        private static NameValueCollection ParseInputs(string? inputs)
        {
            var result = new NameValueCollection();
            
            if (string.IsNullOrEmpty(inputs))
                return result;

            var pairs = inputs.Split(',');
            foreach (var pair in pairs)
            {
                var keyValue = pair.Split(['='], 2);
                if (keyValue.Length == 2)
                {
                    result[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }
            
            return result;
        }

        private static string FormatOutputs(NameValueCollection? outputs)
        {
            if (outputs == null || outputs.Count == 0)
                return "{}";

            var dict = new Dictionary<string, string>();
            foreach (string? key in outputs.AllKeys)
            {
                if (key != null)
                {
                    dict[key] = outputs[key] ?? string.Empty;
                }
            }
            return JsonSerializer.Serialize(dict);
        }
    }

    /// <summary>
    /// Metadata describing a tool parameter (input or output).
    /// </summary>
    public class ToolParameterMetadata
    {
        /// <summary>
        /// The name of the parameter.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A description of the parameter.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The type of the parameter (e.g., "string", "boolean", "object").
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the parameter is required.
        /// </summary>
        public bool Required { get; set; }
    }
}
