using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace LinkCrawler.Services;

public class CrawlerService : ICrawlerService
{
    private readonly IPageFetcher _fetcher;
    private readonly IHtmlParser _parser;
    private readonly ILogger<CrawlerService> _logger;

    private readonly HashSet<string> _visitedUrls = new();

    public CrawlerService(IPageFetcher fetcher, IHtmlParser parser, ILogger<CrawlerService> logger)
    {
        _fetcher = fetcher;
        _parser = parser;
        _logger = logger;
    }

    public async Task CrawlAsync(string startUrl, int maxDepth, int maxPages)
    {
        var startDomain = new Uri(startUrl).Host;

        var queue = new Queue<(string Url, int Depth)>();
        queue.Enqueue((startUrl, 0));
        _visitedUrls.Add(startUrl);

        int pagesProcessed = 0;

        while(queue.Count > 0 && pagesProcessed < maxPages)
        {
            var (currentUrl, depth) = queue.Dequeue();

            //_logger.LogInformation("Crawling [{Depth}]: {Url}", depth, currentUrl);
            Console.WriteLine($"Crawling [{depth}]: {currentUrl}");

            var fetchResult = await _fetcher.FetchAsync(currentUrl);

            if(!fetchResult.Success || fetchResult.Html == null)
            {
                //_logger.LogWarning("Skipping {Url}: {Error}", currentUrl, fetchResult.ErrorMessage);
                Console.WriteLine($"Skipping {currentUrl}: {fetchResult.ErrorMessage}");
                continue;
            }

            var parsed = _parser.Parse(fetchResult.Html, currentUrl);

            pagesProcessed++;

            // TODO: Save 'parsed' + fetchResult.StatusCode + currentUrl + depth to database (next step)
            //_logger.LogInformation("Parsed '{Title}' - {Length} chars, {LinkCount} links",
            //    parsed.Title, parsed.CleanText.Length, parsed.Links.Count);
            Console.WriteLine($"Parsed '{parsed.Title}' - {parsed.CleanText.Length} chars, {parsed.Links.Count} links");

            // Don't queue new links if we've hit max depth
            if (depth >= maxDepth) continue;

            foreach(var link in parsed.Links)
            {
                if (!Uri.TryCreate(link, UriKind.Absolute, out var linkUri))
                    continue;

                if (!linkUri.Host.Equals(startDomain, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (_visitedUrls.Contains(link))
                    continue;

                _visitedUrls.Add(link);
                queue.Enqueue((link, depth + 1));
            }

            // Politeness delay between requests
            await Task.Delay(1000);
        }

        //_logger.LogInformation("Crawl finished.Pages processed: {Count}", pagesProcessed);
        Console.WriteLine($"Crawl finished.Pages processed: {pagesProcessed}");
    }
}
