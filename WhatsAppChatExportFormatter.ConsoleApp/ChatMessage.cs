namespace WhatsAppChatExportFormatter.ConsoleApp;

class ChatMessage
{
    public DateTime? Timestamp { get; init; }

    public string? Sender { get; init; }

    public string? Content { get; set; }
}