using System;
using System.Collections.Generic;
using System.Text;

namespace LinkCrawler.Services;

public interface ICrawlerService
{
    Task CrawlAsync(string startUrl, int maxDepth, int maxPages);
}
