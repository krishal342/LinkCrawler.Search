using System;
using System.Collections.Generic;
using System.Text;

namespace LinkCrawler.Services;

public class ParsedPage
{
    public string? Title { get; set; }
    public string CleanText { get; set; } = string.Empty;
    public List<string> Links { get; set; } = new();
}

public interface IHtmlParser
{
    ParsedPage Parse(string html, string baseUrl);
}
