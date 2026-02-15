using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

            // Define the protocol tool representation using GetInputMetadata and GetOutputMetadata
            _protocolTool = new Tool
            {
                Name = "netinteractor_execute_script",
                Description = "Executes a NetInteractor XML script for web automation. Supports GET requests, form POST submissions, conditional logic (if), and calling other targets. Use XPath expressions to extract data from HTML. Variables use $(Name) syntax.",
                InputSchema = GetInputMetadata(),
                OutputSchema = GetOutputMetadata()
            };
        }

        /// <summary>
        /// Gets the protocol tool representation.
        /// </summary>
        public override Tool ProtocolTool => _protocolTool;

        /// <summary>
        /// Gets the tool metadata - returns empty list as output metadata is returned via OutputSchema.
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
            NameValueCollection? inputs = null;
            string? target = null;

            if (arguments != null)
            {
                if (arguments.TryGetValue("script", out var scriptValue) && scriptValue.ValueKind != JsonValueKind.Undefined && scriptValue.ValueKind != JsonValueKind.Null)
                    script = scriptValue.GetString();
                if (arguments.TryGetValue("inputs", out var inputsValue) && inputsValue.ValueKind == JsonValueKind.Object)
                {
                    inputs = ParseInputsFromJsonElement(inputsValue);
                }
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

            var result = await ExecuteScriptInternalAsync(script, inputs, target);

            if (result.Ok)
            {
                // Return the outputs as structured content for the AI agent
                var outputsObject = ConvertOutputsToJsonNode(result.Outputs);
                return new CallToolResult
                {
                    StructuredContent = outputsObject,
                    IsError = false
                };
            }
            else
            {
                return new CallToolResult
                {
                    Content = new List<ContentBlock>
                    {
                        new TextContentBlock { Text = $"Error: {result.Message}" }
                    },
                    IsError = true
                };
            }
        }

        /// <summary>
        /// Internal method for testing - executes a script and returns the result.
        /// </summary>
        internal async Task<InteractionResult> ExecuteScriptInternalAsync(
            string script,
            NameValueCollection? inputs = null,
            string? target = null)
        {
            try
            {
                var executor = new InterationExecutor(_webAccessor);
                var inputValues = inputs ?? new NameValueCollection();
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
        /// Gets the input metadata schema as JsonElement.
        /// </summary>
        private static JsonElement GetInputMetadata()
        {
            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["script"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = BuildScriptDescription()
                    },
                    ["inputs"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = new JsonObject
                        {
                            ["type"] = "string"
                        },
                        ["description"] = "Object with key-value string pairs for script variable substitution. Example: {\"BaseUrl\": \"https://example.com\", \"Username\": \"admin\", \"Password\": \"secret\"}. These values replace $(Key) placeholders in the script."
                    },
                    ["target"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Name of the target to execute. If omitted, uses the defaultTarget specified in InteractConfig."
                    }
                },
                ["required"] = new JsonArray { "script" }
            };

            return JsonDocument.Parse(schema.ToJsonString()).RootElement;
        }

        /// <summary>
        /// Gets the output metadata schema as JsonElement.
        /// </summary>
        private static JsonElement GetOutputMetadata()
        {
            var schema = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["Ok"] = new JsonObject
                    {
                        ["type"] = "boolean",
                        ["description"] = "True if the script execution completed successfully, false if any action failed"
                    },
                    ["Message"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "Descriptive message about the result, especially useful for errors"
                    },
                    ["Outputs"] = new JsonObject
                    {
                        ["type"] = "object",
                        ["additionalProperties"] = new JsonObject
                        {
                            ["type"] = "string"
                        },
                        ["description"] = "Object containing all extracted output values defined by <output> elements in the script. Keys are output names, values are extracted strings."
                    },
                    ["Target"] = new JsonObject
                    {
                        ["type"] = "string",
                        ["description"] = "The name of the next target to execute (used for workflow chaining)"
                    }
                },
                ["required"] = new JsonArray { "Ok" }
            };

            return JsonDocument.Parse(schema.ToJsonString()).RootElement;
        }

        /// <summary>
        /// Builds the comprehensive script description for AI agents.
        /// </summary>
        private static string BuildScriptDescription()
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("XML script defining the web automation workflow.");
            sb.AppendLine();
            sb.AppendLine("## Script Structure");
            sb.AppendLine();
            sb.AppendLine("<InteractConfig defaultTarget='TargetName'>");
            sb.AppendLine("    <target name='TargetName'>");
            sb.AppendLine("        <!-- Action elements go here -->");
            sb.AppendLine("    </target>");
            sb.AppendLine("</InteractConfig>");
            sb.AppendLine();
            sb.AppendLine("## Target Element");
            sb.AppendLine();
            sb.AppendLine("Targets are named workflow steps. The 'defaultTarget' attribute specifies which target runs first.");
            sb.AppendLine("Targets can call other targets using the <call> action for modular workflows.");
            sb.AppendLine();
            sb.AppendLine("## Supported Action Types");
            sb.AppendLine();
            
            // GET action
            sb.AppendLine("### 1. GET Request (<get>)");
            sb.AppendLine("Fetches a web page via HTTP GET and optionally extracts data.");
            sb.AppendLine();
            sb.AppendLine("Attributes:");
            sb.AppendLine("- url (required): URL to fetch. Supports $(Variable) substitution.");
            sb.AppendLine("- expectedHttpStatusCodes: Comma-separated valid status codes (default: 200).");
            sb.AppendLine();
            sb.AppendLine("Child Elements:");
            sb.AppendLine("- <output>: Extract data from the response (see Output Extraction).");
            sb.AppendLine();
            sb.AppendLine("Example:");
            sb.AppendLine("<get url='$(BaseUrl)/products'>");
            sb.AppendLine("    <output name='pageTitle' xpath='//h1' attr='text()' />");
            sb.AppendLine("</get>");
            sb.AppendLine();
            
            // POST action
            sb.AppendLine("### 2. POST Form Submission (<post>)");
            sb.AppendLine("Submits an HTML form. Must be preceded by a GET to load the page with the form.");
            sb.AppendLine();
            sb.AppendLine("Attributes (use ONE to identify the form):");
            sb.AppendLine("- formIndex: Zero-based index of form on page (e.g., formIndex='0').");
            sb.AppendLine("- formName: The 'name' attribute of the form element.");
            sb.AppendLine("- action: The 'action' attribute/URL of the form.");
            sb.AppendLine("- clientID: The 'id' attribute of the form element.");
            sb.AppendLine();
            sb.AppendLine("Child Elements:");
            sb.AppendLine("- <formValue name='fieldName' value='fieldValue' />: Set form field values.");
            sb.AppendLine("- <output>: Extract data from the response after submission.");
            sb.AppendLine();
            sb.AppendLine("Example:");
            sb.AppendLine("<post formIndex='0'>");
            sb.AppendLine("    <formValue name='username' value='$(Username)' />");
            sb.AppendLine("    <formValue name='password' value='$(Password)' />");
            sb.AppendLine("</post>");
            sb.AppendLine();
            
            // IF action
            sb.AppendLine("### 3. Conditional Execution (<if>)");
            sb.AppendLine("Executes child action only when a condition is met.");
            sb.AppendLine();
            sb.AppendLine("Attributes:");
            sb.AppendLine("- property (required): Input variable to check. Use $(VariableName) syntax.");
            sb.AppendLine("- value (required): Expected value to match.");
            sb.AppendLine();
            sb.AppendLine("Example:");
            sb.AppendLine("<if property='$(NeedsAuth)' value='true'>");
            sb.AppendLine("    <call target='LoginWorkflow' />");
            sb.AppendLine("</if>");
            sb.AppendLine();
            
            // CALL action
            sb.AppendLine("### 4. Call Another Target (<call>)");
            sb.AppendLine("Executes another named target, enabling modular workflows.");
            sb.AppendLine();
            sb.AppendLine("Attributes:");
            sb.AppendLine("- target (required): Name of the target to execute.");
            sb.AppendLine();
            sb.AppendLine("Example:");
            sb.AppendLine("<call target='ExtractProductDetails' />");
            sb.AppendLine();
            
            // Output extraction
            sb.AppendLine("## Output Extraction (<output>)");
            sb.AppendLine();
            sb.AppendLine("Extracts data from HTML responses using XPath.");
            sb.AppendLine();
            sb.AppendLine("Attributes:");
            sb.AppendLine("- name (required): Variable name for extracted value.");
            sb.AppendLine("- xpath (required): XPath expression to select element(s).");
            sb.AppendLine("- attr (required): What to extract ('text()' for inner text, or attribute name like 'href').");
            sb.AppendLine("- regex: Optional regex to further extract from the selected content.");
            sb.AppendLine("- isMultipleValue: Set 'true' to extract all matching elements as comma-separated values.");
            sb.AppendLine("- expectedValue: Validation - fails if extracted value doesn't match.");
            sb.AppendLine();
            sb.AppendLine("Example:");
            sb.AppendLine("<output name='title' xpath='//h1' attr='text()' />");
            sb.AppendLine("<output name='links' xpath='//a' attr='href' isMultipleValue='true' />");
            sb.AppendLine();
            
            // Variable substitution
            sb.AppendLine("## Variable Substitution");
            sb.AppendLine();
            sb.AppendLine("Use $(VariableName) syntax anywhere in attribute values.");
            sb.AppendLine("Variables are provided via the 'inputs' parameter as key-value pairs.");
            sb.AppendLine();
            sb.AppendLine("Example:");
            sb.AppendLine("<get url='$(BaseUrl)/api/$(Endpoint)?id=$(ItemId)' />");
            
            return sb.ToString();
        }

        private static NameValueCollection ParseInputsFromJsonElement(JsonElement inputsElement)
        {
            var result = new NameValueCollection();
            
            foreach (var property in inputsElement.EnumerateObject())
            {
                if (property.Value.ValueKind == JsonValueKind.String)
                {
                    result[property.Name] = property.Value.GetString() ?? string.Empty;
                }
                else
                {
                    result[property.Name] = property.Value.ToString();
                }
            }
            
            return result;
        }

        private static JsonNode? ConvertOutputsToJsonNode(NameValueCollection? outputs)
        {
            if (outputs == null || outputs.Count == 0)
                return new JsonObject();

            var jsonObject = new JsonObject();
            foreach (string? key in outputs.AllKeys)
            {
                if (key != null)
                {
                    jsonObject[key] = outputs[key] ?? string.Empty;
                }
            }
            return jsonObject;
        }
    }
}
