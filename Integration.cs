#region Assembly CharacterAI, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null
// location unknown
// Decompiled with ICSharpCode.Decompiler 7.1.0.6543
#endregion

using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using CharacterAI.Models;
using CharacterAI.Services;
using Newtonsoft.Json;

namespace CharacterAI
{
    public class Integration : CommonService
    {
        private Character _currentCharacter = new Character();

        private readonly List<string> _chatsList = new List<string>();

        private readonly HttpClient _httpClient = new HttpClient();

        private readonly string? _userToken;

        public Character CurrentCharacter => _currentCharacter;

        public List<string> Chats => _chatsList;

        public Integration(string userToken)
        {
            _userToken = userToken;
        }

        public async Task<SetupResult> SetupAsync(string? characterId = null, bool startWithNewChat = false)
        {
            CommonService.Log("\nStarting character setup...");
            CommonService.Success("OK\n  (Character ID: " + (characterId ?? _currentCharacter.Id) + ")\n");
            CommonService.Log("Fetching character info... ");
            Character character = await GetInfoAsync(characterId);
            if (character.IsEmpty)
            {
                return new SetupResult(isSuccessful: false, "Failed to get character info.");
            }

            CommonService.Success("OK\n  (Character name: " + character.Name + ")");
            _currentCharacter = character;
            _chatsList.Clear();
            CommonService.Log("Fetching dialog history... ");
            string text = ((!startWithNewChat) ? (await GetLastChatAsync()) : (await CreateNewChatAsync()));
            string historyId = text;
            if (historyId == null)
            {
                return new SetupResult(isSuccessful: false, "Failed to get chat history.");
            }

            CommonService.Success("OK\n  (History ID: " + historyId + ")");
            _chatsList.Add(historyId);
            CommonService.Log("CharacterAI - ");
            CommonService.Success("Ready\n");
            return new SetupResult(isSuccessful: true);
        }

        public async Task<SetupResult> Reset()
        {
            _chatsList.Clear();
            string historyId = await CreateNewChatAsync();
            if (historyId == null)
            {
                return new SetupResult(isSuccessful: false, "Failed to create new chat.");
            }

            _chatsList.Add(historyId);
            return new SetupResult(isSuccessful: true);
        }

        public async Task<CharacterResponse> CallCharacterAsync(string message = "", string? imagePath = null, string? historyId = null, ulong? primaryMsgId = null, ulong? parentMsgId = null)
        {
            dynamic contentDynamic = CommonService.BasicCallContent(_currentCharacter, message, imagePath, historyId ?? _chatsList.First());
            if (parentMsgId.HasValue)
            {
                contentDynamic.parent_msg_id = parentMsgId;
            }
            else if (primaryMsgId.HasValue)
            {
                contentDynamic.primary_msg_id = primaryMsgId;
                contentDynamic.seen_msg_ids = new ulong[1] { primaryMsgId.Value };
            }

            ByteArrayContent requestContent = new ByteArrayContent(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(contentDynamic)));
            requestContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            HttpRequestMessage request = SetHeadersForRequest(new HttpRequestMessage(HttpMethod.Post, "https://beta.character.ai/chat/streaming/")
            {
                Content = requestContent,
                Headers = { { "accept-encoding", "gzip" } }
            });
            return new CharacterResponse(await _httpClient.SendAsync(request));
        }

        public async Task<Character> GetInfoAsync(string? characterId = null)
        {
            string url = "https://beta.character.ai/chat/character/info/";
            HttpRequestMessage request = SetHeadersForRequest(new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                {
                    "external_id",
                    characterId ?? _currentCharacter.Id
                } })
            });
            HttpResponseMessage response = await _httpClient.SendAsync(request);
            string content = await response.Content.ReadAsStringAsync();
            dynamic character = null;
            if (response.IsSuccessStatusCode)
            {
                character = ((dynamic)JsonConvert.DeserializeObject<object>(content))?.character;
            }
            else
            {
                CommonService.Failure("", response);
            }

            return new Character(character);
        }

        public async Task<string?> GetLastChatAsync(string? characterId = null)
        {
            HttpRequestMessage request = SetHeadersForRequest(new HttpRequestMessage(HttpMethod.Post, "https://beta.character.ai/chat/history/continue/")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> {
                {
                    "character_external_id",
                    characterId ?? _currentCharacter.Id
                } })
            });
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                return ((dynamic)JsonConvert.DeserializeObject<object>(await response.Content.ReadAsStringAsync()))?.external_id;
            }

            return await CreateNewChatAsync(characterId);
        }

        public async Task<string?> CreateNewChatAsync(string? characterId = null)
        {
            HttpRequestMessage request = SetHeadersForRequest(new HttpRequestMessage(HttpMethod.Post, "https://beta.character.ai/chat/history/create/")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {
                        "character_external_id",
                        characterId ?? _currentCharacter.Id
                    },
                    { "override_history_set", null }
                })
            });
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                CommonService.Failure("", response);
                return null;
            }

            dynamic externalId = ((dynamic)JsonConvert.DeserializeObject<object>(await response.Content.ReadAsStringAsync()))?.external_id;
            if ((object)externalId == null)
            {
                CommonService.Failure("Something went wrong...", response);
            }

            return externalId;
        }

        public async Task<HistoriesResponse> GetHistoriesAsync(string? characterId = null)
        {
            HttpRequestMessage request = SetHeadersForRequest(new HttpRequestMessage(HttpMethod.Post, "https://beta.character.ai/chat/character/histories/")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    {
                        "external_id",
                        characterId ?? _currentCharacter.Id
                    },
                    { "number", "50" }
                }),
                Headers = { { "accept-encoding", "gzip" } }
            });
            return new HistoriesResponse(await _httpClient.SendAsync(request));
        }

        public async Task<SearchResponse> SearchAsync(string query)
        {
            HttpRequestMessage request2 = new HttpRequestMessage(requestUri: "https://beta.character.ai/chat/characters/search/?query=" + query, method: HttpMethod.Get);
            request2 = SetHeadersForRequest(request2);
            using HttpResponseMessage response = await _httpClient.SendAsync(request2);
            return new SearchResponse(response);
        }

        public async Task<string?> UploadImageAsync(byte[] img)
        {
            ByteArrayContent image = new ByteArrayContent(img);
            image.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
            HttpRequestMessage request = SetHeadersForRequest(new HttpRequestMessage(HttpMethod.Post, "https://beta.character.ai/chat/upload-image/")
            {
                Content = new MultipartFormDataContent { { image, "\"image\"", "\"image.jpg\"" } }
            });
            using HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                CommonService.Failure("", response);
                return null;
            }

            return ((dynamic)JsonConvert.DeserializeObject<object>(await response.Content.ReadAsStringAsync()))?.value;
        }

        private HttpRequestMessage SetHeadersForRequest(HttpRequestMessage request)
        {
            string[] array = new string[30]
            {
                "Accept",
                "application/json, text/plain, */*",
                "Authorization",
                "Token " + _userToken,
                "accept-Language",
                "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7",
                "accept-encoding",
                "deflate, br",
                "ContentType",
                "application/json",
                "dnt",
                "1",
                "Origin",
                "https://beta.character.ai",
                "Referer",
                "https://beta.character.ai/" + ((_currentCharacter?.Id == null) ? "search?" : ("chat?char=" + _currentCharacter.Id)),
                "sec-ch-ua",
                "\"Not?A_Brand\";v=\"8\", \"Chromium\";v=\"108\", \"Google Chrome\";v=\"108\"",
                "sec-ch-ua-mobile",
                "?0",
                "sec-ch-ua-platform",
                "Windows",
                "sec-fetch-dest",
                "empty",
                "sec-fetch-mode",
                "cors",
                "sec-fetch-site",
                "same-origin",
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36"
            };
            for (int i = 0; i < array.Length - 1; i += 2)
            {
                request.Headers.Add(array[i], array[i + 1]);
            }

            return request;
        }
    }
}
#if false // Decompilation log
'177' items in cache
------------------
Resolve: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Runtime, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Runtime.dll'
------------------
Resolve: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Collections, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Collections.dll'
------------------
Resolve: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Net.Http, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Net.Http.dll'
------------------
Resolve: 'System.Linq.Expressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq.Expressions, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Linq.Expressions.dll'
------------------
Resolve: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Console, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Console.dll'
------------------
Resolve: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Found single assembly: 'Newtonsoft.Json, Version=13.0.0.0, Culture=neutral, PublicKeyToken=30ad4fe6b2a6aeed'
Load from: 'C:\Users\t y\.nuget\packages\newtonsoft.json\13.0.2\lib\net6.0\Newtonsoft.Json.dll'
------------------
Resolve: 'Microsoft.CSharp, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'Microsoft.CSharp, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\Microsoft.CSharp.dll'
------------------
Resolve: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Found single assembly: 'System.Linq, Version=7.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a'
Load from: 'C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\7.0.0\ref\net7.0\System.Linq.dll'
#endif
