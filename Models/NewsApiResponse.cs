namespace DiscordDuolingo.Models;

public class NewsApiResponse
{
    public string? Status { get; set; }
    public int? TotalResults { get; set; }
    public List<Article>? Results { get; set; }
}