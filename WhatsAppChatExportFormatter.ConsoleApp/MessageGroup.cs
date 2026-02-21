namespace WhatsAppChatExportFormatter.ConsoleApp;

/// <summary>
/// Stats about a group of messages.
/// </summary>
public class MessageGroup(string id, string name, int count)
{
    public string Id { get; init; } = id;

    public int Count { get; init; } = count;

    public string Name { get; init; } = name;
}
