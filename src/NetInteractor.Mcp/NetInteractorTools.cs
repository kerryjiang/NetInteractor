using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using NetInteractor.Config;
using NetInteractor.WebAccessors;

namespace NetInteractor.Mcp
{
    /// <summary>
    /// MCP Server Tools for NetInteractor web automation.
    /// These tools enable AI agents to execute web interactions and automation scripts.
    /// </summary>
    [McpServerToolType]
    public static class NetInteractorTools
    {
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
        public static async Task<ExecuteScriptResult> ExecuteScriptAsync(
            string script,
            string? inputs = null,
            string? target = null)
        {
            try
            {
                var webAccessor = new HttpClientWebAccessor();
                var executor = new InterationExecutor(webAccessor);
                
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
        /// Performs an HTTP GET request and extracts data using XPath.
        /// </summary>
        /// <param name="url">The URL to fetch.</param>
        /// <param name="xpath">Optional XPath expression to extract content from the HTML.</param>
        /// <param name="attribute">Optional attribute to extract from the XPath result (e.g., 'href', 'src'). Use 'text()' for text content.</param>
        /// <returns>The result containing the extracted content or raw HTML.</returns>
        [McpServerTool(Name = "netinteractor_get")]
        [Description("Performs an HTTP GET request and optionally extracts data using XPath. Use this for simple web scraping and data extraction from web pages.")]
        public static async Task<GetRequestResult> GetAsync(
            string url,
            string? xpath = null,
            string? attribute = null)
        {
            try
            {
                var webAccessor = new HttpClientWebAccessor();
                var response = await webAccessor.GetAsync(url);

                var result = new GetRequestResult
                {
                    Success = response.StatusCode >= 200 && response.StatusCode < 300,
                    StatusCode = response.StatusCode,
                    Url = response.Url
                };

                if (!string.IsNullOrEmpty(xpath))
                {
                    var pageInfo = new PageInfo(response.Url ?? url, response.Html ?? string.Empty);
                    var extractedValue = ExtractValue(pageInfo, xpath, attribute);
                    result.ExtractedValue = extractedValue;
                }
                else
                {
                    result.Html = response.Html;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new GetRequestResult
                {
                    Success = false,
                    StatusCode = 0,
                    Message = $"GET request failed: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Performs an HTTP POST request with form data.
        /// </summary>
        /// <param name="url">The URL to post to.</param>
        /// <param name="formData">Comma-separated key=value pairs for form data (e.g., "name=John,email=john@example.com").</param>
        /// <param name="xpath">Optional XPath expression to extract content from the response HTML.</param>
        /// <param name="attribute">Optional attribute to extract from the XPath result.</param>
        /// <returns>The result containing the extracted content or raw HTML.</returns>
        [McpServerTool(Name = "netinteractor_post")]
        [Description("Performs an HTTP POST request with form data and optionally extracts data from the response using XPath. Use this for submitting forms and processing responses.")]
        public static async Task<PostRequestResult> PostAsync(
            string url,
            string formData,
            string? xpath = null,
            string? attribute = null)
        {
            try
            {
                var webAccessor = new HttpClientWebAccessor();
                var formValues = ParseInputs(formData);
                var response = await webAccessor.PostAsync(url, formValues);

                var result = new PostRequestResult
                {
                    Success = response.StatusCode >= 200 && response.StatusCode < 300,
                    StatusCode = response.StatusCode,
                    Url = response.Url
                };

                if (!string.IsNullOrEmpty(xpath))
                {
                    var pageInfo = new PageInfo(response.Url ?? url, response.Html ?? string.Empty);
                    var extractedValue = ExtractValue(pageInfo, xpath, attribute);
                    result.ExtractedValue = extractedValue;
                }
                else
                {
                    result.Html = response.Html;
                }

                return result;
            }
            catch (Exception ex)
            {
                return new PostRequestResult
                {
                    Success = false,
                    StatusCode = 0,
                    Message = $"POST request failed: {ex.Message}"
                };
            }
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

        private static string? ExtractValue(PageInfo pageInfo, string xpath, string? attribute)
        {
            var node = pageInfo.Document.DocumentNode.SelectSingleNode(xpath);
            
            if (node == null)
                return null;

            if (string.IsNullOrEmpty(attribute) || attribute.Equals("text()", StringComparison.OrdinalIgnoreCase))
            {
                return node.InnerText?.Trim();
            }

            return node.GetAttributeValue(attribute, string.Empty) is { Length: > 0 } value ? value : null;
        }

        private static System.Collections.Generic.Dictionary<string, string>? ConvertOutputs(NameValueCollection outputs)
        {
            if (outputs == null || outputs.Count == 0)
                return null;

            var dict = new System.Collections.Generic.Dictionary<string, string>();
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
}
