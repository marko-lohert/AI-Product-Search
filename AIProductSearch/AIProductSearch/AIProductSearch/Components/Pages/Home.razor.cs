using AIProductSearch.DAL;
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
}