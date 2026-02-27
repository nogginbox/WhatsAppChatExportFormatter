using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace WhatsAppChatExportFormatter.ConsoleApp;

partial class Program
{
    private const string WhatsAppDateTimeFormat = "dd/MM/yyyy, HH:mm:ss";

    static void Main(string[] args)
    {
        /*if (args.Length == 0)
        {
            Console.WriteLine("Usage: ChatToHtml <input-file> [output-file]");
            Console.WriteLine("Example: ChatToHtml chat.txt chat.html");
            return;
        }*/

        string contentDirectory = $"{AppContext.BaseDirectory}\\..\\..\\..\\Content\\Chats\\";
        string inputFile = $"{contentDirectory}Chat 2\\_chat.txt";  // args[0];
        string outputFile = $"{contentDirectory}Chat 2\\chat.html";  // args.Length > 1 ? args[1] : Path.ChangeExtension(inputFile, ".html");

        if (!File.Exists(inputFile))
        {
            Console.WriteLine($"Error: File '{inputFile}' not found.");
            Environment.Exit(2);
        }
        else
        {
            Console.WriteLine($"Read file '{inputFile}'.");
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
        var messagePattern = FindMessageHeaderRegex();

        ChatMessage? currentMessage = null;

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
                    Timestamp = ParseDateTime(match.Groups[1].Value),
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
        string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputFile))
            ?? throw new ArgumentException($"Could not get path for output file {outputFile}");

        html.AppendLine("""
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="UTF-8" />
                <meta name="viewport" content="width=device-width, initial-scale=1.0" />
                <meta name="generator" content="WhatsApp Chat Export Formatter" />
                <link rel="generator" href="https://github.com/nogginbox/WhatsAppChatExportFormatter" />
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
                    .message.sender-0 {
                        border-left: 3px solid #2563eb; /* Blue */
                    }
                    .message.sender-1 {
                        border-left: 3px solid #0891b2; /* Cyan */
                    }
                    .message.sender-2 {
                        border-left: 3px solid #16a34a; /* Green */
                    }
                    .message.sender-3 {
                        border-left: 3px solid #9333ea; /* Purple */
                    }
                    .message.sender-4 {
                        border-left: 3px solid #ea580c; /* Orange */
                    }
                    .message.sender-5 {
                        border-left: 3px solid #db2777 /* Magenta */
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
                    .timestamp a {
                        color: #666;
                        text-decoration: none;
                        cursor: default;
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
                    .toc-link {
                        text-align: right;
                        font-size: 0.6em;
                    }
                    footer {
                        font-size: 0.6em;
                        margin: 4em 0 1em 0;
                        text-align: center;
                    }
                </style>
            </head>
            <body>
                <div class="chat-container">
                    <h1>Chat Export</h1>
            """);


        int? CurrentMonth = null;
        List<MessageGroup> months = [];
        var htmlMessages = new StringBuilder();

        // Split messages into groups for each month
        var messageGroups = messages.GroupBy(m => new DateTime(m.Timestamp?.Year ?? 0, m.Timestamp?.Month ?? 1, 1));

        foreach (var messageGroup in messageGroups)
        {
            var monthId = $"{messageGroup.Key:yyyy-MM}";
            var monthName = $"{messageGroup.Key:MMMM yyyy}";
            months.Add(new (monthId, monthName, messageGroup.Count()));
                
            if (CurrentMonth != null)
            {
                htmlMessages.AppendLine("<p class=\"toc-link\"><a href=\"#toc\">Table of contents</a></p>");
            }
            htmlMessages.AppendLine($"<h2 id=\"h-{monthId}\">{monthName}</h2>");
                
            CurrentMonth = messageGroup.Key.Month;
            
            foreach(var message in messageGroup)
            {
                string content = ProcessMessageContent(message.Content, outputDirectory);
                var senderId = Array.IndexOf(senders, message.Sender);

                htmlMessages.AppendLine($"""
                            <div id="{message.LinkId}" class="message sender-{senderId}">
                                <div class="message-header">
                                    <span class="sender">{EscapeHtml(message.Sender)}</span>
                                    <span class="timestamp"><a href="#{message.LinkId}">{message.Timestamp:ddd dd MMM yyyy - HH:mm:ss}</a></span>
                                </div>
                                <div class="content">{content}</div>
                            </div>
                    """);
            }

            
        }

        // HTML - Contents
        html.AppendLine("<ul id=\"toc\">");
        foreach(var month in months)
        {
            html.AppendLine($"<li><a href=\"#h-{month.Id}\">{month.Name}</a> ({month.Count})</li>");
        }
        html.AppendLine("</ul>");

        // HTML - Add the messages
        html.Append(htmlMessages);

        // HTML - Footer
        html.AppendLine("""
                </div>
                <footer>
                    <p>Made using <a href="https://github.com/nogginbox/WhatsAppChatExportFormatter">WhatsApp Chat Export Formatter</a>.</p>
                </footer>
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

        // Then process media attachments (after escaping, so the pattern matches escaped text)
        // Image in message example: <attached: 00000033-PHOTO-2023-12-30-17-06-30.jpg>
        var attachmentPattern = FindImageMarkupRegex();

        return attachmentPattern.Replace(escapedContent, match => {
            string filename = match.Groups[1].Value.Trim();
            string extension = filename.Split('.').Last();
            string imagePath = Path.Combine(outputDirectory, filename);

            // Check if a media file exists
            if (!File.Exists(imagePath))
            {
                Console.WriteLine($"Media file '{filename}' is not available. If you want this to apear, make sure you copy it from orginal chat export into the same folder as the output file.");
            }
            
            return $"<br>{markupForFileType(extension, filename)}";
        });

        static string markupForFileType(string extension, string file)
        {
            extension = extension.ToLower();
            string? mimeType = extension switch
            {
                "mp4" => "video/mp4",
                "webm" => "video/webm",
                "ogg" => "video/ogg",
                "ogv" => "video/ogg",
                "mov" => "video/quicktime",
                "avi" => "video/x-msvideo",
                "3gp" => "video/3gpp",
                "3g2" => "video/3gpp2",
                _ => null
            };

            return extension switch
            {
                "gif" or
                "jpg" or
                "png" => $"""<img src="{file}" alt="{file}">""",
                "mp4" or
                "webm" or
                "ogg" or
                "ogv" or
                "mov" or
                "avi" or
                "3gp" or
                "3g2" => $"""
                    <video width="640" height="480" controls>
                      <source src="{file}" type="{mimeType}">
                      Your browser does not support the video tag.
                    </video>
                    """,
                _ => $"""<em>{file}</em>"""
            };
        }

    }

    private static string EscapeHtml(string? text)
    {
        return text?
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;")
            ?? string.Empty;
    }

    private static DateTime? ParseDateTime(string stringDateTime)
    {
        if (DateTime.TryParseExact(
            stringDateTime,
            WhatsAppDateTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime parsedDate))
        {
            return parsedDate;
        }
        else
        {
            // Handle the failure case
            Console.WriteLine($"Warning - Unable to parse the date/time {stringDateTime}.");
            return null;
        }
    }

    /// <summary>
    /// Match the message header: [date, time] Sender.
    /// (Handles optional invisible characters (like left-to-right marks) at the start)
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"^[\u200E\u200F\s]*\[(.+?)\]\s+(.+?):\s*(.*)$")]
    
    private static partial Regex FindMessageHeaderRegex();
    [GeneratedRegex(@"&lt;attached:\s*([^&]+)&gt;")]
    private static partial Regex FindImageMarkupRegex();
}
