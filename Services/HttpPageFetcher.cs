using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;

namespace LinkCrawler.Services;

public class HttpPageFetcher : IPageFetcher
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpPageFetcher> _logger;
    public HttpPageFetcher(HttpClient httpClient, ILogger<HttpPageFetcher> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }
    public async Task<FetchResult> FetchAsync(string url)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url);

            var statusCode = (int)response.StatusCode;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Non-success status {StatusCode} for {Url}", statusCode, url);
                return new FetchResult
                {
                    Success = false,
                    StatusCode = statusCode,
                    ErrorMessage = $"Http {statusCode}"
                };
            }

            var contentType = response.Content.Headers.ContentType?.MediaType; // it gives "application/json" or "text/html" 
            if (contentType != null && !contentType.Contains("html"))
            {
                _logger.LogInformation("Skipping non-HTML content ({ContentType}) at {Url}", contentType, url);
                return new FetchResult
                {
                    Success = false,
                    StatusCode = statusCode,
                    ErrorMessage = $"Non-HTML content type: {contentType}"
                };
            }

            var html = await response.Content.ReadAsStringAsync();

            return new FetchResult
            {
                Success = true,
                Html = html,
                StatusCode = statusCode
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Request failed for {Url}", url);
            return new FetchResult { Success = false, ErrorMessage = ex.Message };

        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Request timed out for {Url}", url);
            return new FetchResult { Success = false, ErrorMessage = "Timeout" };
        }
    }
}
