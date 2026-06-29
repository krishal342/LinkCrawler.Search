using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow; // Essential for later!
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace LinkChecker
{
    class Program
    {
        // 1. Share a single HttpClient instance to prevent socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        {
            Timeout = TimeSpan.FromSeconds(10) // Don't let slow sites hang our threads
        };

        // 2. Limit maximum concurrent requests to 10 so we don't overwhelm the server
        private static readonly SemaphoreSlim _networkThrottle = new SemaphoreSlim(10, 10);

        private static readonly ConcurrentDictionary<Uri, byte> _visitedUrls = new ConcurrentDictionary<Uri, byte>();

        // take http response and convert it into string and return the string
        private static async Task<string> DownloadHtmlAsync(Uri url)
        {
            await _networkThrottle.WaitAsync();

            try
            {
                Console.WriteLine($"[Crawling] {url}");

                using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Error] Failed to download {url}:{ex.Message}");
            }
            finally
            {
                _networkThrottle.Release();
            }

            return string.Empty;
        }

        // part to extract the url and list them
        private static IEnumerable<Uri> ExtractLinks(string html, Uri baseUrl)
        {
            var discoveredUrls = new List<Uri>();

            if (string.IsNullOrWhiteSpace(html))
                return discoveredUrls;

            var linkPattern = new Regex(@"href\s*=\s*[""'](?<url>[^""'#]+)[""']", RegexOptions.IgnoreCase);
            var matches = linkPattern.Matches(html);

            foreach (Match match in matches)
            {
                string currentUrl = match.Groups["url"].Value;

                if (Uri.TryCreate(baseUrl, currentUrl, out Uri? absoluteUri))
                {
                    if (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps)
                    {
                        discoveredUrls.Add(absoluteUri);
                    }
                }
            }

            return discoveredUrls;
        }

        private static async Task VerifyUrlAsync(Uri url)
        {
            await _networkThrottle.WaitAsync();

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Head, url);
                using var response = await _httpClient.SendAsync(request);

                if ((int)response.StatusCode == 404)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[BROKEN 404] - {url}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[OK] {(int)response.StatusCode} - {url}");
                    Console.ResetColor();
                }

            }
            catch (Exception)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[INVALID] Failed to reach - {url}");
                Console.ResetColor();
            }
            finally
            {
                _networkThrottle.Release();
            }
        }


        static async Task Main(string[] args)
        {
            Console.WriteLine("--- High-Performance Link Checker Initialized ---");

            // Next step will go here

            var seedUrl = new Uri("https://github.com/");
            _visitedUrls.TryAdd(seedUrl, 0);

            var crawlBlock = new TransformBlock<Uri, string>(
                async url => await DownloadHtmlAsync(url),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 10
                }
                );

            var extractionBlock = new TransformManyBlock<string, Uri>(
                html => ExtractLinks(html, seedUrl),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = Environment.ProcessorCount
                }
                );

            var validationBlock = new ActionBlock<Uri>(
                async url => await VerifyUrlAsync(url),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 10
                }
                );

            crawlBlock.LinkTo(extractionBlock, new DataflowLinkOptions { PropagateCompletion = true });
            extractionBlock.LinkTo(validationBlock, new DataflowLinkOptions { PropagateCompletion = true });

            Console.WriteLine($"Starting crawl at: {seedUrl}");
            await crawlBlock.SendAsync(seedUrl);
            crawlBlock.Complete();

            //await Task.Delay(5000);
            await validationBlock.Completion;

            Console.WriteLine("\nCrawl complete or timed out. Press any key to exit.");
            Console.ReadKey();

        }
    }
}
