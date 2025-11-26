using AIProductSearch.DAL;
using AIProductSearch.DAO;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.AI;
using System.Diagnostics;

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
    decimal ThinkingTime { get; set; } = 0;
    bool ShowThinkingTime { get; set; } = false;
    long MaxAllowedUploadedFileSize = 100 * 1024 * 1024;

    private async Task Search()
    {
        if (SearchText is null or "")
            return;

        Result = string.Empty;
        IsOutputVisible = true; // The actual result is not yet available, so ‘Thinking’ will be displayed.
        ShowThinkingTime = false;

        string prompt = @$"""
            Look at the following data about products:
            {AllProducts.ToJson()}
            Give the short answer to the following question: 
            {SearchText}""";

        ChatHistory.Add(new ChatMessage(ChatRole.User, prompt));
        List<ChatResponseUpdate> completeResponse = [];

        ChatOptions options = new()
        {
            Temperature = 0.95f,
            // We have switched to the "gemma3:4b" model, so we need to comment out ToolCalls
            // because this model does not support ToolCalls.
            //AllowMultipleToolCalls = true,
            //Tools = [AIFunctionFactory.Create(IsRecommendedProduct)]
        };

        Stopwatch stopwatch = new();
        stopwatch.Start();

        await foreach (ChatResponseUpdate responseUpdate in ChatClient.GetStreamingResponseAsync(ChatHistory, options))
        {
            Result += responseUpdate.Text;
            StateHasChanged();

            completeResponse.Add(responseUpdate);
        }

        stopwatch.Stop();
        ThinkingTime = stopwatch.ElapsedMilliseconds;
        ShowThinkingTime = true;

        ChatHistory.AddMessages(completeResponse);
    }

    private async Task AnalyzeProducts()
    {
        Result = string.Empty;
        IsOutputVisible = true; // The actual result is not yet available, so ‘Thinking’ will be displayed.
        ShowThinkingTime = false;

        Stopwatch stopwatch = new();
        stopwatch.Start();

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

        stopwatch.Stop();
        ThinkingTime = stopwatch.ElapsedMilliseconds;
        ShowThinkingTime = true;
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

    public bool IsRecommendedProduct(Product product)
    {
        if (product is null || product.Name is null)
            return false;

        return product.Name.Contains("raspberry pi", StringComparison.InvariantCultureIgnoreCase) || product.Name.Contains("arduino", StringComparison.InvariantCultureIgnoreCase);
    }

    private async Task SearchUsingImage(IBrowserFile file)
    {
        byte[] imageContent = await GetFileContent(file);

        ChatMessage message = new ChatMessage(ChatRole.User, "What is in this image? The answer must be short.");
        message.Contents.Add(new DataContent(imageContent, file.ContentType));
        ChatResponse response = await ChatClient.GetResponseAsync(message);

        SearchText = $"Find products similar to {response.Text}";

        await Search();
    }

    private async Task<byte[]> GetFileContent(IBrowserFile file)
    {
        if (file is null)
            return [];

        using Stream stream = file.OpenReadStream(MaxAllowedUploadedFileSize);
        using MemoryStream memoryStream = new();
        await stream.CopyToAsync(memoryStream);

        return memoryStream.ToArray();
    }
}