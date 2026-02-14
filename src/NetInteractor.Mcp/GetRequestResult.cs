namespace NetInteractor.Mcp
{
    /// <summary>
    /// Result of an HTTP GET request.
    /// </summary>
    public class GetRequestResult
    {
        /// <summary>
        /// Indicates whether the request was successful (2xx status code).
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// HTTP status code of the response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Final URL after any redirects.
        /// </summary>
        public string? Url { get; set; }

        /// <summary>
        /// Value extracted using XPath (if xpath parameter was provided).
        /// </summary>
        public string? ExtractedValue { get; set; }

        /// <summary>
        /// Raw HTML content (if no xpath was specified).
        /// </summary>
        public string? Html { get; set; }

        /// <summary>
        /// Error message if the request failed.
        /// </summary>
        public string? Message { get; set; }
    }
}
