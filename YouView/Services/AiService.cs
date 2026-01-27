using System.Net.Http.Headers;
using System.Text.Json;
using System.ClientModel; 
using OpenAI; 
using OpenAI.Chat;

namespace YouView.Services;

public class AiService
{
    private readonly string _groqApiKey;
    private readonly string _openRouterApiKey;
    private readonly string _openRouterModel;
    
    private readonly OpenAIClient _openRouterClient;
    private readonly HttpClient _httpClient;

    public AiService(IConfiguration config)
    {
        _groqApiKey = config["AiSettings:GroqApiKey"];
        _openRouterApiKey = config["AiSettings:OpenRouterApiKey"];
        // Default to Llama 3.3 if not set
        _openRouterModel = config["AiSettings:OpenRouterModel"] ?? "meta-llama/llama-3.3-70b-instruct:free"; 
        
        _httpClient = new HttpClient();

        // Setup the "Thinking" Brain (OpenRouter)
        if (!string.IsNullOrEmpty(_openRouterApiKey))
        {
            var options = new OpenAIClientOptions 
            { 
                Endpoint = new Uri("https://openrouter.ai/api/v1") 
            };
            
            _openRouterClient = new OpenAIClient(new ApiKeyCredential(_openRouterApiKey), options);
        }
    }

    // 1. HEARING: Send Audio to Groq (Free Whisper)
    public async Task<string> TranscribeAudioAsync(string audioPath)
    {
        if (string.IsNullOrEmpty(_groqApiKey)) return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/audio/transcriptions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _groqApiKey);

            using var content = new MultipartFormDataContent();
            
            var fileStream = File.OpenRead(audioPath);
            var fileContent = new StreamContent(fileStream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("audio/mpeg");
            content.Add(fileContent, "file", Path.GetFileName(audioPath));
            content.Add(new StringContent("whisper-large-v3-turbo"), "model");

            request.Content = content;

            var response = await _httpClient.SendAsync(request);
            var responseString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Groq Error: {responseString}");
                return null;
            }

            using var doc = JsonDocument.Parse(responseString);
            if (doc.RootElement.TryGetProperty("text", out var textElement))
            {
                return textElement.GetString();
            }
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Transcription Failed: {ex.Message}");
            return null;
        }
    }

    // 2. THINKING: Send Text to OpenRouter (Free Llama 3.3)
    public async Task<string> GenerateSummaryAsync(string transcript)
    {
        if (_openRouterClient == null || string.IsNullOrEmpty(transcript)) return "No transcript available.";

        try
        {
            // Get the chat client for the specific model
            var chatClient = _openRouterClient.GetChatClient(_openRouterModel);

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage("You are a YouTube video expert. Read the following transcript and write a 2-sentence summary that makes people want to watch."),
                new UserChatMessage(transcript)
            };
            ChatCompletion completion = await chatClient.CompleteChatAsync(messages);
            
            // Return the AI's response
            return completion.Content[0].Text;
        }
        catch (Exception ex)
        {
            return $"AI Summary Error: {ex.Message}";
        }
    }
}