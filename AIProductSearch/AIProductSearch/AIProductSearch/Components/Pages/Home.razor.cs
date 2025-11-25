using AIProductSearch.DAL;
using AIProductSearch.DAO;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.AI;

namespace AIProductSearch.Components.Pages;

public partial class Home
{
    [Inject]
    public IChatClient ChatClient { get; set; }

    [Inject]
    Products AllProducts { get; set; }

    string SearchText { get; set; } = string.Empty;
    string Result { get; set; } = string.Empty;
    bool IsOutputVisible { get; set; } = false;

    List<ChatMessage> ChatHistory = new();

    private async Task Search()
    {
        if (SearchText is null or "")
            return;

        Result = string.Empty;
        IsOutputVisible = true; // The actual result is not yet available, so ‘Thinking’ will be displayed.

        string prompt = @$"""
            Look at the following data about products:
            {AllProducts.ToJson()}
            Give the short answer to the following question: 
            {SearchText}""";

        ChatHistory.Add(new ChatMessage(ChatRole.User, prompt));
        List<ChatResponseUpdate> completeResponse = [];

        await foreach (ChatResponseUpdate responseUpdate in ChatClient.GetStreamingResponseAsync(ChatHistory))
        {
            Result += responseUpdate.Text;
            StateHasChanged();

            completeResponse.Add(responseUpdate);
        }

        ChatHistory.AddMessages(completeResponse);
    }

    private async Task AnalyzeProducts()
    {
        Result = string.Empty;
        IsOutputVisible = true; // The actual result is not yet available, so ‘Thinking’ will be displayed.

        List<ProductAnalysisResult> productsSummaries = new();

        foreach (Product product in AllProducts.CompleteProductsList())
        {
            string prompt = @$"""
            Analyze the following product:
            name = {product.Name},
            description = {product.Description}
            Gather the following information about this product:
            - Product name
            - Keywords in the description
            - Language used to write the description
            """;

            var response = await ChatClient.GetResponseAsync<ProductAnalysisResult>(prompt);

            productsSummaries.Add(response.Result);
        }

        int countKeyword = productsSummaries.Sum(x => x.DescriptionKeywords?.Count ?? 0);

        Result = $"{productsSummaries.Count} products were analyzed. {countKeyword} keywords were found.";
    }

    public class ProductAnalysisResult 
    {
        public string ProductName { get; set; }
        public List<string> DescriptionKeywords { get; set; }
        public SupportedLanguages DescriptionLanguage { get; set; }
    }

    public enum SupportedLanguages
    {
        Croatian,
        English,
        German
    }
}