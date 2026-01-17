namespace WhatsAppExportChatMaker.ConsoleApp;

class ChatMessage
{
    public string Timestamp { get; init; }

    public string? Sender { get; init; }

    public string? Content { get; set; }
}