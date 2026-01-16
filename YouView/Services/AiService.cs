using Azure;
using Azure.AI.OpenAI;
using OpenAI;
using OpenAI.Chat; 
using OpenAI.Audio; 

namespace YouView.Services;

public class AiService
{
    private readonly string _apiKey;
    private readonly OpenAIClient _client;

    public AiService(IConfiguration config)
    {
        _apiKey = config["OpenAI:ApiKey"]; 
        
        if (!string.IsNullOrEmpty(_apiKey))
        {
            _client = new OpenAIClient(_apiKey);
        }
    }

    // 1. Audio -> Text (Transcription)
    public async Task<string> TranscribeAudioAsync(string audioPath)
    {
        if (_client == null) return "AI Service not configured.";

        try
        {
            var audioClient = _client.GetAudioClient("whisper-1");

            // Transcribe the audio file
            AudioTranscription transcription = await audioClient.TranscribeAudioAsync(audioPath);
            
            return transcription.Text; 
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI Transcription Failed: {ex.Message}");
            return null;
        }
    }

    // 2. Text -> Summary
    public async Task<string> GenerateSummaryAsync(string transcript)
    {
        if (_client == null || string.IsNullOrEmpty(transcript)) return "No summary available.";

        try
        {
            var chatClient = _client.GetChatClient("gpt-4o-mini"); // Use "gpt-3.5-turbo" or "gpt-4o-mini" (Cheaper)

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a helpful video assistant. Summarize the following video transcript in 2-3 sentences. Keep it exciting."),
                new UserChatMessage(transcript)
            };

            ChatCompletion completion = await chatClient.CompleteChatAsync(messages);

            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"AI Summary Failed: {ex.Message}");
            return "Summary generation failed.";
        }
    }
}