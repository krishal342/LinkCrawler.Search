using LinkCrawler.Data;
using LinkCrawler.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

IConfiguration config = builder.Configuration;

var connectionString = config.GetConnectionString("DefaultConnections");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddHttpClient<IPageFetcher, HttpPageFetcher>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent",
        config["CrawlSettings:UserAgent"] ?? "MyCrawlerBot/1");
});

builder.Services.AddSingleton<IHtmlParser, AngleSharpHtmlParser>();

builder.Services.AddScoped<ICrawlerService, CrawlerService>();

var host = builder.Build();

Console.WriteLine("Web Crawler Search - starting up...");

// ---- TEMPORARY TEST CODE (remove after verifying) ----
using (var scope = host.Services.CreateScope())
{
    var crawler = scope.ServiceProvider.GetRequiredService<ICrawlerService>();

    var testUrl = "https://books.toscrape.com"; 
    //var testUrl = "https://example.com";
    Console.WriteLine($"Starting crawl: {testUrl}");
    Console.WriteLine();

    await crawler.CrawlAsync(testUrl, maxDepth: 2, maxPages: 10);
}
// ---- END TEMPORARY TEST CODE ----

await host.RunAsync();
