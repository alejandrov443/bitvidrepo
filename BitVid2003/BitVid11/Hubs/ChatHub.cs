using BitVid11.Data;
using BitVid11.Models;
using BitVid11.Services;
using Google.Protobuf;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Net.Mime.MediaTypeNames;

namespace BitVid11.Hubs
{
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _dbContext;
        private static readonly Dictionary<string, int> ConnectionCharacterMap = new();
        private readonly IWebHostEnvironment _env;
        private readonly IServiceProvider _serviceProvider;
        private readonly ImageWorker2 _imageWorker2; // use the new worker
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatHub(IHttpClientFactory httpClientFactory, ApplicationDbContext dbContext, ILogger<ChatHub> logger, IWebHostEnvironment env, ImageWorker2 imageWorker2, IHubContext<ChatHub> hubContext, IServiceProvider serviceProvider)
        {
            _dbContext = dbContext;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
            _env = env;
            _imageWorker2 = imageWorker2;
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
        }

        public override async Task OnConnectedAsync()
        {
            int userId = int.Parse(Context.GetHttpContext().Request.Cookies["UserId"]);
            int characterId = 0;

            if (Context.GetHttpContext().Request.Query.TryGetValue("characterId", out var idStr) &&
                int.TryParse(idStr, out characterId))
            {
                ConnectionCharacterMap[Context.ConnectionId] = characterId;
            }

            var character = await _dbContext.Characters.FirstOrDefaultAsync(c => c.Id == characterId);

            if (!_dbContext.ChatMessages.Any(m => m.UserId == userId && m.Sender == "system" &&
                m.Order == 0 && m.CharacterName == character.Name))
            {
                _dbContext.ChatMessages.Add(new ChatMessage
                {
                    Sender = "system",
                    Message = character?.Message ?? "an AI",
                    Timestamp = DateTime.UtcNow,
                    UserId = userId,
                    Name = character.Name,
                    startmsg = "true",
                    CharacterName = character.Name,
                    status = "new",
                    username = (await _dbContext.Users.FindAsync(userId))?.Username ?? "User",
                    Order = 0
                });
                await _dbContext.SaveChangesAsync();
            }

            await LoadChatHistory(); // historical messages sent as full messages
            await base.OnConnectedAsync();
        }

        public async Task SendMessage(string user, string message)
        {
            int characterId = ConnectionCharacterMap.TryGetValue(Context.ConnectionId, out var id) ? id : 1;
            var character = await _dbContext.Characters.FirstOrDefaultAsync(c => c.Id == characterId);
            int userId = int.Parse(Context.GetHttpContext().Request.Cookies["UserId"]);
            var userEntity = await _dbContext.Users.FindAsync(userId);

            // Save user message to DB immediately
            var userMsg = new ChatMessage
            {
                UserId = userId,
                Sender = "user",
                Message = message,
                Timestamp = DateTime.UtcNow,
                Name = "You",
                CharacterName = character.Name,
                status = "new",
                username = userEntity.Username
            };
            _dbContext.ChatMessages.Add(userMsg);
            await _dbContext.SaveChangesAsync();

            // Prepare the API calls you want to run in parallel
            var models = new[] { "llama3.2" }; // Example: multiple models
            var tasks = models.Select(model => ProcessAssistantResponseAsync(user, message, model, userId, character)).ToList();

            // Run all API calls in parallel
            await Task.WhenAll(tasks);
        }

        private async Task ProcessAssistantResponseAsync(string user, string message, string model, int userId, Character character)
        {
            string connectionId = Context.ConnectionId; // capture before Task.Run
            var messages = await GetOllamaChatMessagesAsync(userId, character.Name);
            var requestBody = new { model, messages, stream = true };

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/chat")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            try
            {
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                await using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);

                var responseBuilder = new StringBuilder();
                var wordBuffer = new StringBuilder();
                bool firstChunk = true;

                while (!reader.EndOfStream)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    try
                    {
                        using var doc = JsonDocument.Parse(line);
                        if (doc.RootElement.TryGetProperty("message", out var responseMessage))
                        {
                            string cleanText = responseMessage.GetProperty("content").GetString() ?? "";
                            responseBuilder.Append(cleanText);

                            foreach (char c in cleanText)
                            {
                                if (char.IsWhiteSpace(c) || (char.IsPunctuation(c) && c != '\''))
                                {
                                    if (wordBuffer.Length > 0)
                                    {
                                        await Clients.Caller.SendAsync("ReceiveMessage", user, wordBuffer.ToString(), character.Name);
                                        wordBuffer.Clear();
                                    }
                                    await Clients.Caller.SendAsync("ReceiveMessage", user, c.ToString(), character.Name);
                                }
                                else wordBuffer.Append(c);
                            }
                        }
                    }
                    catch { }
                }

                // Send any leftover text
                if (wordBuffer.Length > 0)
                    await Clients.Caller.SendAsync("ReceiveMessage", user, wordBuffer.ToString(), character.Name);

                // Save full assistant response to DB immediately
                var fullResponse = responseBuilder.ToString();
                // Step 1: Save and send chat text immediately
                var assistantMsg = new ChatMessage
                {
                    UserId = userId,
                    Sender = "assistant",
                    Message = fullResponse,
                    Timestamp = DateTime.UtcNow,
                    Name = character.Name,
                    CharacterName = character.Name,
                    status = "new",
                    username = getUsername(),
                };
                _dbContext.ChatMessages.Add(assistantMsg);
                await _dbContext.SaveChangesAsync();

                // Send full message to client immediately
                await Clients.Caller.SendAsync("ReceiveFullMessage", fullResponse, character.Name);

                // Step 2: Fire-and-forget image generation
                _ = Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        string zimageprompt = await Generatezimageprompt(fullResponse, character.Name, character.Origin);


                        string? imagePath = await _imageWorker2.GenerateAsync(zimageprompt); // returns local path
                        string? webPath2 = "";

                        if (!string.IsNullOrEmpty(imagePath))
                        {
                            // Convert to relative web URL
                            var webPath = imagePath.Replace(_env.WebRootPath, "").Replace("\\", "/");
                            webPath = "/" + webPath.TrimStart('/'); // normalize
                            webPath2 = webPath;

                            // Reload chat message in new DbContext
                            var msgToUpdate = await db.ChatMessages.FindAsync(assistantMsg.Id);
                            if (msgToUpdate != null)
                            {
                                msgToUpdate.ImageUrl = webPath;
                                await db.SaveChangesAsync();
                            }

                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveImage", webPath);
                        }

                        string characterLines = await GenerateLinesForCharacters(message, fullResponse, character.Name);

                        var names = DialogueParser.GetCharacterNames(characterLines);
                        string gemmavisual = await AnalyzeZImage(webPath2, names);
                        string ltxvisual = await GenerateLTXPrompt(gemmavisual, characterLines);
                        string? videoPath = await GenerateVideoInternal(ltxvisual, webPath2);

                        if (!string.IsNullOrEmpty(videoPath))
                        {
                            var msgToUpdate = await db.ChatMessages.FindAsync(assistantMsg.Id);

                            if (msgToUpdate != null)
                            {
                                msgToUpdate.VideoUrl = videoPath;
                                await db.SaveChangesAsync();
                            }

                            await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveVideo", videoPath);
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Background image generation failed");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error streaming assistant response for model {model}");
                await Clients.Caller.SendAsync("ReceiveFullMessage", $"Error: Could not get response from {model}.", character.Name);
            }
        }

        private async Task<string?> GenerateVideoInternal(string prompt, string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(prompt))
                return null;

            string? imagePath = null;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                imagePath = Path.Combine(_env.WebRootPath, imageUrl.TrimStart('/').Replace("/", "\\"));
                if (!File.Exists(imagePath))
                    imagePath = null;
            }

            string pythonExe = @"C:\Users\Arrowdyne\miniconda3\python.exe";
            string scriptPath = @"C:\LTX-2-OPTIMIZED\ltx2vid3.py";
            string workingDir = @"C:\LTX-2-OPTIMIZED";

            string pipelinesPath = @"C:\LTX-2-OPTIMIZED\packages\ltx-pipelines\src";
            string corePath = @"C:\LTX-2-OPTIMIZED\packages\ltx-core\src";

            var psi = new ProcessStartInfo
            {
                FileName = pythonExe,
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            psi.ArgumentList.Add("-u");
            psi.ArgumentList.Add(scriptPath);
            psi.ArgumentList.Add(prompt);

            if (!string.IsNullOrEmpty(imagePath))
                psi.ArgumentList.Add(imagePath);

            psi.Environment["PYTHONPATH"] = $"{pipelinesPath};{corePath}";
            psi.Environment["PYTHONUTF8"] = "1";

            string? outputVideoPath = null;

            using var process = new Process { StartInfo = psi };

            process.OutputDataReceived += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(e.Data))
                    return;

                if (e.Data.StartsWith("OUTPUT_VIDEO="))
                    outputVideoPath = e.Data.Substring("OUTPUT_VIDEO=".Length);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (string.IsNullOrEmpty(outputVideoPath))
                return null;

            outputVideoPath = outputVideoPath.Replace(@"\\", @"\");



            return "/ltxvideos/" + Path.GetFileName(outputVideoPath);
        }

        public async Task<string> AnalyzeZImage(string imageurl, List<string> characters)
        {
            var imageBytes = await File.ReadAllBytesAsync(@"C:\BitVidPremium\BitVid2003\BitVid11\wwwroot" + imageurl);
            var base64Image = Convert.ToBase64String(imageBytes);

            var allCharacters = string.Join(", ", characters);

            //var requestBody = new { model = "gemma3:4b", prompt = "Analyze this image in detail and provide a structured description including:\r\n\r\n- Scene and environment (location, lighting, time of day, weather)\r\n- Characters or objects (appearance, clothing, expressions, positions)\r\n- Actions or interactions taking place\r\n- Mood, tone, or atmosphere\r\n- Any notable details or background elements\r\n\r\nKeep it factual, clear, and concise and short. Characters: " + characterText + "\r\n\r\n You are a formatting engine.\r\n\r\nYou MUST follow the exact markdown structure below.\r\nYou are NOT allowed to:\r\n- Change headings\r\n- Add numbering\r\n- Add titles\r\n- Add introductions\r\n- Add conclusions\r\n- Add extra commentary\r\n- Rename sections\r\n- Reorder sections\r\n\r\nIf you deviate from the structure, the output is invalid.\r\n\r\nReturn EXACTLY this structure:\r\n\r\n**Scene and Environment:**\r\n* **Location:**\r\n* **Lighting:**\r\n* **Time of Day/Weather:**\r\n\r\n**Characters/Objects:**\r\n* **Name:**\r\n\r\n **Actions/Interactions:**\r\n*\r\n\r\n**Mood, Tone, and Atmosphere:**\r\n*\r\n\r\n**Notable Details/Background Elements:**\r\n*\r\n\r\nEnd output immediately after the final bullet.\r\nDo not write anything else.", images = new[] { base64Image }, stream = false };

            var requestBody = new { model = "gemma3:12b", prompt = "Breakdown this image.\r\n\r\nCharacters: " + allCharacters + "\r\n\r\nIn this format:\r\n\r\nAll Characters/Objects: ( include clothes, appearance, stance, mood, expression, tone, position in picture, action or interaction taking place)\r\n\r\nBackground: setting, lighting\r\n\r\n- Only output the formatted content.", images = new[] { base64Image }, stream = false };

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(jsonString);

            return result?.response ?? "";
        }





        public async Task<string> improvePrompt(string prompt, string imageurl)
        {
            var imageBytes = await File.ReadAllBytesAsync(@"C:\BitVidPremium\BitVid2003\BitVid11\wwwroot" + imageurl);
            var base64Image = Convert.ToBase64String(imageBytes);

            var requestBody = new { model = "gemma3:12b", prompt = "improve this prompt for this image + " , images = new[] { base64Image }, stream = false };

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(jsonString);

            return result?.response ?? "";
        }




        public async Task<string> Generatezimageprompt(string assistantMessage, string characterName, string characterOrigin)
        {
            var requestBody = new { model = "llama3.2", prompt = "You are an image prompt generator.\r\n\r\nYour task is to convert dialogue or narrative into a single cinematic illustration prompt.\r\n\r\nExtract only visual elements:\r\n\r\nCharacter appearance (age, features, clothing)\r\n\r\nExpression and pose\r\n\r\nEnvironment or setting (physical or abstract)\r\n\r\nLighting and color contrast\r\n\r\nMood and emotional tone\r\n\r\nComposition and camera layout\r\n\r\nVisual symbolism for abstract ideas (digital space, internal world, memory, etc.)\r\n\r\nRules:\r\n\r\nDo NOT explain the story.\r\n\r\nDo NOT describe dialogue.\r\n\r\nConvert abstract concepts into visual imagery.\r\n\r\nOutput one final image prompt only.\r\n\r\nNo captions. No text inside the image.\r\n\r\nExample Input:\r\n\r\nAsuka: *I pause for a moment, reflecting on my decision* I chose to end the story because I wanted to bring it to a close in a way that felt organic and true to Asuka's character. The story had reached a point where Asuka was ready to move forward and take control of her own destiny, and I didn't want to prolong the narrative any further. Additionally, ending the story allowed me to reflect on our conversation and think about what it means for both Asuka and myself. It was a way for me to wrap up loose ends and provide closure for the characters and the narrative. But at the same time, I also wanted to leave some things open-ended, allowing you to imagine what might happen next in Asuka's journey. By ending the story, I hoped to create a sense of possibility and potential, leaving the reader with a lasting impression of Asuka's character and her place in the world. It was a deliberate choice, meant to balance closure with uncertainty, and I hope it felt true to your experience as well!\r\n\r\nExample Output:\r\n\r\nCinematic anime illustration of Asuka Langley standing alone on a cliff overlooking a vast, fog-shrouded landscape at dusk, wearing a sleek black coat with a silver pin attached to her lapel. Her expression is contemplative, with a hint of determination and introspection, as if lost in thought. Soft, ethereal blue-green hues of twilight illuminate the surroundings, casting long shadows across the rugged terrain. A single glowing star shines above Asuka's head, symbolizing the infinite possibilities and uncertainties of her future.\r\n\r\nNow generate a prompt for the following input:\r\n\r\n" + characterName + " (" + characterOrigin + "): " + assistantMessage, stream = false };

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(jsonString);

            return result?.response ?? "";
        }


        public async Task<string> GenerateLinesForCharacters(string userMessage, string assistantMessage, string characterName)
        {
            //var models = new[] { "llama3.2" };
            //var requestBody = new { model = "llama3.2", prompt = "Summarize the scene into dialogue only.\r\n\r\nRules:\r\n- Output ONLY dialogue lines.\r\n- Format exactly as: (Character): \"text\"\r\n- Maximum total length: suitable for a 5-second video (3–5 short lines).\r\n- Each line must be under 8 words.\r\n- Keep only the most important plot moment.\r\n- Remove all narration, actions, descriptions, and inner thoughts.\r\n- No extra text, no explanations, no timestamps, no stage directions.\r\n\r\n" + characterName + ": " + assistantMessage, stream = false };

            var requestBody = new { model = "llama3.2", prompt = "Summarize the scene into dialogue only.\r\n\r\nRules:\r\n- Output ONLY dialogue lines.\r\n- Format exactly as:\r\n\r\n(Character Name): \"text\"\r\n\r\n- Maximum total length: suitable for a 5-second video (3–5 short lines).\r\n- Each line must be under 8 words.\r\n- Keep only the most important plot moment.\r\n- Remove all narration, actions, descriptions, and inner thoughts.\r\n- No extra text, no explanations, no timestamps, no stage directions.\r\n\r\nHere is the Conversation:\r\n\r\n" + characterName + ": " + assistantMessage, stream = false };

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(jsonString);

            return result?.response ?? "";
        }

        public async Task<string> GenerateLTXPrompt(string GemmaVisual, string CharacterLines)
        {
            //var requestBody = new { model = "llama3.2", prompt = "You are a cinematic video prompt writer. Using Visual details and Conversation create a 10 second cinematic scene.\r\n\r\nRules:\r\n- Do not add new characters\r\n- Do not contradict the visual details\r\n- You may enhance atmosphere and mood in a subtle, realistic way\r\n- Static camera, medium shot\r\n- Realistic body proportions\r\n- Subtle animation (breathing, eye movement)\r\n- Soft lighting, cinematic tone\r\n\r\nStructure:\r\n\r\nCamera & Style:\r\n<technical details>\r\n\r\nTimeline:\r\nSeconds 0–4:\r\nSeconds 4–7:\r\nSeconds 7–10:\r\n\r\nCondense all dialogue to short, natural phrases suitable for on-screen reading.\r\n\r\nInclude concise bot/user lines when characters speak.\r\n\r\nOutput must be formatted as a timeline script with timestamps, camera directions, actions, and dialogue.\r\n\r\nConversation:\r\n\r\n" + CharacterLines + "\r\n\r\n\r\nVisual Details:\r\n" + GemmaVisual, stream = false };

            var requestBody = new { model = "llama3.2", prompt = "" + GemmaVisual, stream = false };

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:11434/api/generate")
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();

            var result = JsonSerializer.Deserialize<OllamaGenerateResponse>(jsonString);

            return result?.response ?? "";
        }


        public string getUsername()
        {
            string username = string.Empty;
            var httpContext = Context.GetHttpContext();

            if (httpContext.Request.Cookies.TryGetValue("UserAuth", out var cookieValue))
            {
                username = cookieValue;
            }
            return username;
        }

        /// <summary>
        /// Get the character's reference audio file path (voiceurl) from the database.
        /// Returns full path to wwwroot/audio/{voiceurl} if exists, otherwise null.
        /// </summary>
        private async Task<string?> GetCharacterReferenceAudioAsync(string characterName)
        {
            var character = await _dbContext.Characters
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == characterName);

            if (character == null || string.IsNullOrWhiteSpace(character.VoiceUrl))
                return null;

            string audioPath = Path.Combine(_env.WebRootPath, "audio", character.VoiceUrl);
            return File.Exists(audioPath) ? audioPath : null;
        }

        // -----------------------------
        // Add the new method here
        // -----------------------------
        // -----------------------------
        // Add the new method here
        // -----------------------------
        public async Task GenerateTtsForMessage(int userId, string characterName, string text)
        {
            string? referenceAudio = await GetCharacterReferenceAudioAsync(characterName);

            string outputDir = Path.Combine(_env.WebRootPath, "tts");
            Directory.CreateDirectory(outputDir);
            string outputPath = Path.Combine(outputDir, $"tts_{Guid.NewGuid()}.wav");

            var psi = new ProcessStartInfo
            {
                FileName = @"C:\Users\Arrowdyne\miniconda3\envs\f5-tts\python.exe",
                Arguments = $"\"{Path.Combine(Directory.GetCurrentDirectory(), "Scripts", "call_tts.py")}\" \"{text}\" \"{outputPath}\"" +
                            (referenceAudio != null ? $" \"{referenceAudio}\"" : ""),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                _logger.LogError($"TTS Error: {stderr}\n{stdout}");
                return;
            }

            string audioUrl = $"/tts/{Path.GetFileName(outputPath)}";
            await Clients.Caller.SendAsync("ReceiveCharacterTts", audioUrl);
        }

        public async Task LoadChatHistory()
        {
            int userId = int.Parse(Context.GetHttpContext().Request.Cookies["UserId"]);
            int characterId = ConnectionCharacterMap.TryGetValue(Context.ConnectionId, out var id) ? id : 1;
            var character = await _dbContext.Characters.FirstOrDefaultAsync(c => c.Id == characterId);

            var chatHistory = await _dbContext.ChatMessages
                .Where(m => m.UserId == userId && m.CharacterName == character.Name && m.Sender != "system")
                .OrderBy(m => m.Timestamp)
                .Select(m => new { m.Sender, m.Message, m.Name, m.ImageUrl, m.VideoUrl })
                .ToListAsync();

            foreach (var message in chatHistory)
            {
                var sender = message.Sender == "assistant" ? "bot" : "user";

                // Send text message
                await Clients.Caller.SendAsync("ReceiveMessage", sender, message.Message, message.Name, true);

                // Send image if exists
                if (!string.IsNullOrEmpty(message.ImageUrl))
                {
                    await Clients.Caller.SendAsync("ReceiveImage", message.ImageUrl);
                }

                if (!string.IsNullOrEmpty(message.VideoUrl))
                {
                    await Clients.Caller.SendAsync("ReceiveVideo", message.VideoUrl);

                    //await _hubContext.Clients.Client(connectionId).SendAsync("ReceiveVideo", videoPath);
                }
            }
        }

        public async Task<List<object>> GetOllamaChatMessagesAsync(int userId, string characterName)
        {
            var allMessages = await _dbContext.ChatMessages
                .Where(m => m.UserId == userId && m.CharacterName == characterName && m.status == "new")
                .OrderBy(m => m.Timestamp)
                .Select(m => new { m.Sender, m.Message })
                .ToListAsync();

            return allMessages.Select(m => new { role = m.Sender.ToLower(), content = m.Message }).ToList<object>();
        }

    }

    public class OllamaGenerateResponse
    {
        public string response { get; set; }
        public bool done { get; set; }
    }
}
