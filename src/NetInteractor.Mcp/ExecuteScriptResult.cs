using System.Collections.Generic;

namespace NetInteractor.Mcp
{
    /// <summary>
    /// Result of executing a NetInteractor script.
    /// </summary>
    public class ExecuteScriptResult
    {
        /// <summary>
        /// Indicates whether the script execution was successful.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result or error.
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Extracted output values from the script execution.
        /// </summary>
        public Dictionary<string, string>? Outputs { get; set; }
    }
}
