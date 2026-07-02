# High-Performance Asynchronous Link Checker & Crawler

A high-performance C# console utility designed to target a seed webpage, extract all internal/external hyperlinks, and concurrently validate their HTTP status codes. It leverages **TPL (Task Parallel Library) Dataflow** for non-blocking processing pipelines alongside strict throttling mechanisms to ensure safe network traffic footprinting.

## Key Architectural Features

* **Advanced Pipeline Processing:** Built on a decoupled producer-consumer model using TPL Dataflow blocks (`TransformBlock`, `TransformManyBlock`, and `ActionBlock`) to handle async execution fluidly.
* **Regex Link Harvesting:** Scans raw HTML responses in parallel using an optimized regular expression pattern to parse out absolute and relative `href` web addresses[cite: 3].
* **Hybrid Parallel Throttling:** Combines localized Dataflow `MaxDegreeOfParallelism` thresholds with a central structural `SemaphoreSlim(10, 10)`[cite: 3]. This ensures that the combined network footprint of crawling operations and validation operations never spikes past 10 concurrent outgoing sockets[cite: 3].
* **Resource Optimization:** 
  * Downloads target page bodies efficiently using `HttpCompletionOption.ResponseHeadersRead`[cite: 3].
  * Validates downstream discovered links via lightweight HTTP `HEAD` requests to eliminate massive payload downloads[cite: 3].
* **Thread-Safe Data Structures:** Prepared for expansive state management via `ConcurrentDictionary<Uri, byte>` to support instantaneous `O(1)` tracking of historical page routes[cite: 3].
* **Automatic Pipeline Cleanup:** Utilizes `PropagateCompletion = true` across block links so that completion signals cascade through the pipeline, tearing down the environment cleanly when work ceases[cite: 3].

---

## Pipeline Design & Data Flow

Data moves asynchronously down a sequential assembly line[cite: 3]:

```text
 [ Seed URL ] 
      │
      ▼
┌──────────────┐
│  crawlBlock  │ ──► Downloads raw HTML string concurrently (Max = 5)[cite: 3]
└──────────────┘
      │
      ▼
┌─────────────────┐
│ extractionBlock │ ──► Regular Expression Link Extraction (Max = CPU Cores)[cite: 3]
└─────────────────┘
      │
      ▼
┌─────────────────┐
│ validationBlock │ ──► Validates links via fast HTTP HEAD (Max = 10)[cite: 3]
└─────────────────┘