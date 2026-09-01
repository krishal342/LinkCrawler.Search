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

var host = builder.Build();

Console.WriteLine("Web Crawler Search - starting up...");

// ---- TEMPORARY TEST CODE (remove after verifying) ----
using (var scope = host.Services.CreateScope())
{
    var fetcher = scope.ServiceProvider.GetRequiredService<IPageFetcher>();

    var testUrl = "https://example.com";
    Console.WriteLine($"Fetching: {testUrl}");

    var result = await fetcher.FetchAsync(testUrl);

    if (result.Success)
    {
        Console.WriteLine($"Success! Status: {result.StatusCode}");
        Console.WriteLine($"HTML length: {result.Html?.Length} characters");
        Console.WriteLine(result.Html ?? "Nothing");
    }
    else
    {
        Console.WriteLine($"Failed: {result.ErrorMessage} (Status: {result.StatusCode})");
    }
}
// ---- END TEMPORARY TEST CODE ----

await host.RunAsync();
