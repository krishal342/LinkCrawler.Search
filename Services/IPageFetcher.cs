
namespace LinkCrawler.Services;

public class FetchResult
{
    public bool Success { get; set; }
    public string? Html { get; set; }
    public int? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
}

public interface IPageFetcher
{
    Task<FetchResult> FetchAsync(string url);
}
