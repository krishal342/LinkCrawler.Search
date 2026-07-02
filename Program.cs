using System.Threading.Tasks.Dataflow; 
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

namespace LinkCrawler
{
    class Program
    {
        // Share a single HttpClient instance to prevent socket exhaustion
        private static readonly HttpClient _httpClient = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        })
        {
            Timeout = TimeSpan.FromSeconds(10) 
        };

        //  Limit maximum concurrent requests to 10 so we don't overwhelm the server         
        // SemaphoreSlim( initialCount, maxCount)

        private static readonly SemaphoreSlim _networkThrottle = new SemaphoreSlim(10, 10);

        // it is a storage for visited url
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
                    Console.WriteLine($"[BROKEN 404] - {url}");
                }
                else
                {
                    Console.WriteLine($"[OK] {(int)response.StatusCode} - {url}");
                }

            }
            catch (Exception)
            {
                Console.WriteLine($"[INVALID] Failed to reach - {url}");
            }
            finally
            {
                _networkThrottle.Release();
            }
        }


        static async Task Main(string[] args)
        {
            Console.WriteLine("Link Crawler Initialized\n");


            var seedUrl = new Uri("https://github.com/");

            // this doesn't have any purpose in this code as only one url is visited
            _visitedUrls.TryAdd(seedUrl, 0);

            var crawlBlock = new TransformBlock<Uri, string>(
                async url => await DownloadHtmlAsync(url),
                new ExecutionDataflowBlockOptions
                {
                    MaxDegreeOfParallelism = 5
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

            await validationBlock.Completion;

            Console.WriteLine("\nCrawl complete or timed out. Press any key to exit.");
            Console.ReadKey();

        }
    }
}
