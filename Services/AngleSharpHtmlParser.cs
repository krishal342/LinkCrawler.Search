using AngleSharp;
using AngleSharp.Dom;

namespace LinkCrawler.Services;

public class AngleSharpHtmlParser : IHtmlParser
{
    public ParsedPage Parse(string html, string baseUrl)
    {
        var context = BrowsingContext.New(Configuration.Default);
        var document = context.OpenAsync(req => req.Content(html)).Result;

        RemoveElements(document, "script, style, noscript, nav, footer, header, iframe, svg");

        var title = document.Title?.Trim();

        var cleanText = ExtractCleanText(document);

        var links = ExtractLinks(document, baseUrl);

        return new ParsedPage
        {
            Title = title,
            CleanText = cleanText,
            Links = links
        };


    }

    private void RemoveElements(IDocument document,string cssSelector)
    {
        var elements = document.QuerySelectorAll(cssSelector);
        foreach(var el in elements)
        {
            el.Remove();
        }
    }

    private string ExtractCleanText(IDocument document)
    {
        var bodyText = document.Body?.TextContent ?? string.Empty;

        var lines = bodyText
            .Split('\n',StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line));

        return string.Join(" ", lines);
    }

    private List<string> ExtractLinks(IDocument document, string baseUrl)
    {
        var links = new List<string>();
        var anchorTags = document.QuerySelectorAll("a[href]");

        foreach(var anchor in anchorTags)
        {
            var href = anchor.GetAttribute("href");
            if (string.IsNullOrWhiteSpace(href)) continue;

            if (href.StartsWith("#") || href.StartsWith("mailto:") ||
                    href.StartsWith("tel:") || href.StartsWith("javascript:"))
            {
                continue;
            }

            if(Uri.TryCreate(new Uri(baseUrl), href, out var absoluteUri))
            {
                var cleanUrl = absoluteUri.GetLeftPart(UriPartial.Query);
                links.Add(cleanUrl);
            }
        }

        return links.Distinct().ToList();

    }

}
