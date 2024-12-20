using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class TerrariaMediaWiki
{
    private static readonly HttpClient client;

    static TerrariaMediaWiki()
    {
        client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3");
    }

    public async Task<JsonElement> SearchWiki(string query)
    {
        // Construct the API URL
        string url = $"https://terraria.wiki.gg/api.php?action=query&list=search&srsearch={Uri.EscapeDataString(query)}&format=json";

        try
        {
            // Send the request
            var response = await client.GetStringAsync(url);
            var jsonDocument = JsonDocument.Parse(response);

            // Navigate the JSON structure
            var searchResults = jsonDocument.RootElement.GetProperty("query").GetProperty("search");
            foreach (var result in searchResults.EnumerateArray())
            {
                string title = result.GetProperty("title").GetString();
                string snippet = result.GetProperty("snippet").GetString();
                Console.WriteLine($"Title: {title}\nSnippet: {snippet}\n");
            }
            // Output the response (you can parse the JSON here if needed)
            Console.WriteLine(response);
            return searchResults;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }

    public async Task<JsonNode> GetSections(string id)
    {
        string url = $"https://terraria.wiki.gg/api.php?action=parse&pageid={id}&prop=sections&format=json";

        try
        {
            var response = await client.GetStringAsync(url);
            var jsonDocument = JsonNode.Parse(response);

            var sections = jsonDocument?["parse"]?["sections"];
            return sections;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            throw;
        }
    }

    public async Task<string> GetWikitextForSection(string pageId, string sectionIndex)
    {
        string url = $"https://terraria.wiki.gg/api.php?action=parse&pageid={pageId}&prop=text&section={sectionIndex}&format=json";
        string response = await client.GetStringAsync(url);
        var json = JsonNode.Parse(response);
        var text = json?["parse"]?["text"]?["*"]?.ToString();
        string plainText = Regex.Replace(text, "<.*?>", string.Empty);
        return plainText;
    }

}
