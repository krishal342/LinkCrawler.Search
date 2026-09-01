using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LinkCrawler.Models;

public class CrawledPage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Title { get; set; }

    [Column(TypeName = "text")]
    public string? Content { get; set; }

    [Column(TypeName = "text")]
    public string? RawHtml { get; set; }

    [MaxLength(255)]
    public string? Domain { get; set; }

    public int Depth { get; set; }

    public int? StatusCode { get; set; }

    public DateTime CrawledAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastModified { get; set; }
}
