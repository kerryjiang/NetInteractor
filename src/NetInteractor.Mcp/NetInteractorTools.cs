using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using NetInteractor.WebAccessors;

namespace NetInteractor.Mcp
{
    /// <summary>
    /// MCP Server Tool for NetInteractor web automation.
    /// This tool enables AI agents to execute web interactions and automation scripts using InteractExecutor.
    /// </summary>
    public class NetInteractorTool
    {
        private readonly IWebAccessor _webAccessor;

        /// <summary>
        /// Initializes a new instance of the NetInteractorTool class with the default HttpClientWebAccessor.
        /// </summary>
        public NetInteractorTool()
            : this(new HttpClientWebAccessor())
        {
        }

        /// <summary>
        /// Initializes a new instance of the NetInteractorTool class with a custom web accessor.
        /// </summary>
        /// <param name="webAccessor">The web accessor to use for HTTP requests.</param>
        public NetInteractorTool(IWebAccessor webAccessor)
        {
            _webAccessor = webAccessor ?? throw new ArgumentNullException(nameof(webAccessor));
        }

        /// <summary>
        /// Executes a NetInteractor XML script for web automation.
        /// </summary>
        /// <param name="script">The XML script defining the web interaction workflow. Example:
        /// &lt;InteractConfig defaultTarget='Main'&gt;
        ///     &lt;target name='Main'&gt;
        ///         &lt;get url='https://example.com'&gt;
        ///             &lt;output name='title' xpath='//title' attr='text()' /&gt;
        ///         &lt;/get&gt;
        ///     &lt;/target&gt;
        /// &lt;/InteractConfig&gt;</param>
        /// <param name="inputs">Optional comma-separated key=value pairs for script inputs (e.g., "BaseUrl=https://example.com,Username=user").</param>
        /// <param name="target">Optional target name to execute. If not specified, the default target will be used.</param>
        /// <returns>The execution result containing outputs and status.</returns>
        [McpServerTool(Name = "netinteractor_execute_script")]
        [Description("Executes a NetInteractor XML script for web automation. Use this to run complex multi-step web workflows including GET requests, form submissions, data extraction, and conditional logic.")]
        public async Task<ExecuteScriptResult> ExecuteScriptAsync(
            string script,
            string? inputs = null,
            string? target = null)
        {
            try
            {
                var executor = new InterationExecutor(_webAccessor);
                
                var inputValues = ParseInputs(inputs);
                var result = await executor.ExecuteAsync(script, inputValues, target);

                return new ExecuteScriptResult
                {
                    Success = result.Ok,
                    Message = result.Message,
                    Outputs = result.Outputs != null ? ConvertOutputs(result.Outputs) : null
                };
            }
            catch (Exception ex)
            {
                return new ExecuteScriptResult
                {
                    Success = false,
                    Message = $"Script execution failed: {ex.Message}"
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
                    Description = "The XML script defining the web interaction workflow",
                    Type = "string",
                    Required = true
                },
                ["inputs"] = new ToolParameterMetadata
                {
                    Name = "inputs",
                    Description = "Optional comma-separated key=value pairs for script inputs (e.g., 'BaseUrl=https://example.com,Username=user')",
                    Type = "string",
                    Required = false
                },
                ["target"] = new ToolParameterMetadata
                {
                    Name = "target",
                    Description = "Optional target name to execute. If not specified, the default target will be used",
                    Type = "string",
                    Required = false
                }
            };
        }

        /// <summary>
        /// Gets the output metadata for the ExecuteScript tool.
        /// </summary>
        /// <returns>A dictionary describing the output properties.</returns>
        public static Dictionary<string, ToolParameterMetadata> GetOutputMetadata()
        {
            return new Dictionary<string, ToolParameterMetadata>
            {
                ["Success"] = new ToolParameterMetadata
                {
                    Name = "Success",
                    Description = "Indicates whether the script execution was successful",
                    Type = "boolean",
                    Required = true
                },
                ["Message"] = new ToolParameterMetadata
                {
                    Name = "Message",
                    Description = "Message describing the result or error",
                    Type = "string",
                    Required = false
                },
                ["Outputs"] = new ToolParameterMetadata
                {
                    Name = "Outputs",
                    Description = "Extracted output values from the script execution as key-value pairs",
                    Type = "object",
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

        private static Dictionary<string, string>? ConvertOutputs(NameValueCollection outputs)
        {
            if (outputs == null || outputs.Count == 0)
                return null;

            var dict = new Dictionary<string, string>();
            foreach (string? key in outputs.AllKeys)
            {
                if (key != null)
                {
                    dict[key] = outputs[key] ?? string.Empty;
                }
            }
            return dict;
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
