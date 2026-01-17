using System.Text;
using System.Text.RegularExpressions;

namespace WhatsAppExportChatMaker.ConsoleApp;

class Program
{
    static void Main(string[] args)
    {
        /*if (args.Length == 0)
        {
            Console.WriteLine("Usage: ChatToHtml <input-file> [output-file]");
            Console.WriteLine("Example: ChatToHtml chat.txt chat.html");
            return;
        }*/

        string inputFile = $"{AppContext.BaseDirectory}\\..\\..\\..\\Content\\Chat 1\\_chat.txt";  // args[0];
        string outputFile = $"{AppContext.BaseDirectory}\\..\\..\\..\\Content\\Chat 1\\chat.html";  // args.Length > 1 ? args[1] : Path.ChangeExtension(inputFile, ".html");

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: File '{inputFile}' not found.");
            return;
        }

        try
        {
            var messages = ParseChatFile(inputFile);
            var senders = messages.Select(m => m.Sender).Distinct().Order().ToArray();

            GenerateHtml(senders, messages, outputFile);
            Console.WriteLine($"Successfully converted {messages.Count} messages to {outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    static List<ChatMessage> ParseChatFile(string filePath)
    {
        var messages = new List<ChatMessage>();
        var lines = File.ReadAllLines(filePath);

        // Regex to match the message header: [date, time] Sender:
        // Handles optional invisible characters (like left-to-right marks) at the start
        var messagePattern = new Regex(@"^[\u200E\u200F\s]*\[(.+?)\]\s+(.+?):\s*(.*)$");

        ChatMessage currentMessage = null;

        foreach (var line in lines)
        {
            var match = messagePattern.Match(line);

            if (match.Success)
            {
                // Save previous message if exists
                if (currentMessage != null)
                {
                    messages.Add(currentMessage);
                }

                // Start new message
                currentMessage = new ChatMessage
                {
                    Timestamp = match.Groups[1].Value,
                    Sender = match.Groups[2].Value,
                    Content = match.Groups[3].Value
                };
            }
            else if (currentMessage != null && !string.IsNullOrWhiteSpace(line))
            {
                // Continuation of previous message
                currentMessage.Content += "\n" + line;
            }
        }

        // Add the last message
        if (currentMessage != null)
        {
            messages.Add(currentMessage);
        }

        return messages;
    }

    static void GenerateHtml(string?[] senders, List<ChatMessage> messages, string outputFile)
    {
        var html = new StringBuilder();
        string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputFile));

        html.AppendLine("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8">
                <meta name="viewport" content="width=device-width, initial-scale=1.0">
                <title>Chat Export</title>
                <style>
                    body {
                        font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Arial, sans-serif;
                        max-width: 800px;
                        margin: 0 auto;
                        padding: 20px;
                        background-color: #f5f5f5;
                    }
                    .chat-container {
                        background-color: white;
                        border-radius: 8px;
                        padding: 20px;
                        box-shadow: 0 2px 4px rgba(0,0,0,0.1);
                    }
                    .message {
                        margin-bottom: 16px;
                        padding: 12px;
                        border-left: 3px solid #007bff;
                        background-color: #f8f9fa;
                        border-radius: 4px;
                    }
                    .message .sender-1 {
                        border-left: 3px solid #2563eb;
                    }
                    .message .sender-2 {
                        border-left: 3px solid #16a34a;
                    }
                    .message .sender-3 {
                        border-left: 3px solid #9333ea;
                    }
                    .message .sender-4 {
                        border-left: 3px solid #ea580c;
                    }
                    .message .sender-5 {
                        border-left: 3px solid #0891b2;
                    }
                    .message .sender-6 {
                        border-left: 3px solid #dc2626;
                    }
                    .message-header {
                        display: flex;
                        justify-content: space-between;
                        margin-bottom: 8px;
                    }
                    .sender {
                        font-weight: bold;
                        color: #333;
                    }
                    .timestamp {
                        color: #666;
                        font-size: 0.9em;
                    }
                    .content {
                        color: #333;
                        white-space: pre-wrap;
                        word-wrap: break-word;
                    }
                    .content img {
                        max-width: 100%;
                        height: auto;
                        margin-top: 8px;
                        border-radius: 4px;
                    }
                    h1 {
                        color: #333;
                        margin-bottom: 20px;
                    }
                </style>
            </head>
            <body>
                <div class="chat-container">
                    <h1>Chat Export</h1>
            """);

        foreach (var message in messages)
        {
            string content = ProcessMessageContent(message.Content, outputDirectory);
            var senderId = Array.IndexOf(senders, message.Sender);

            html.AppendLine($"""
                        <div class="message sender-{senderId}">
                            <div class="message-header">
                                <span class="sender">{EscapeHtml(message.Sender)}</span>
                                <span class="timestamp">{EscapeHtml(message.Timestamp)}</span>
                            </div>
                            <div class="content">{content}</div>
                        </div>
                """);
        }

        html.AppendLine("""
                </div>
            </body>
            </html>
            """);

        File.WriteAllText(outputFile, html.ToString());
    }

    static string ProcessMessageContent(string content, string outputDirectory)
    {
        // First escape the text content
        string escapedContent = content
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");

        // Then process image attachments (after escaping, so the pattern matches escaped text)
        var attachmentPattern = new Regex(@"&lt;attached:\s*([^&]+)&gt;");

        return attachmentPattern.Replace(escapedContent, match => {
            string filename = match.Groups[1].Value.Trim();
            string imagePath = Path.Combine(outputDirectory, filename);

            // Check if image exists
            if (File.Exists(imagePath))
            {
                return $"<br><img src=\"{filename}\" alt=\"{filename}\">";
            }
            else
            {
                return $"<br><em>[Image: {filename}]</em>";
            }
        });
    }

    static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
