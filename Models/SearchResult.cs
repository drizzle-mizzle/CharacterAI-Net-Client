using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CharacterAI.Services;

namespace CharacterAI.Models
{
    public class SearchResult : CommonService
    {
        public List<Character>? Characters { get => characters; }
        public string? ErrorReason { get => errorReason; }
        public bool IsSuccessful { get => isSuccessful; }
        public bool IsEmpty { get => isEmpty; }

        private List<Character>? characters = null;
        private string? errorReason = null;
        private bool isEmpty = true;
        private bool isSuccessful = true;

        public SearchResult(HttpResponseMessage httpResponse)
        {
            dynamic response = GetSearchResponse(httpResponse);

            if (response is null) return;
            if (response is string)
            {
                isSuccessful = false;
                errorReason = response;

                return;
            }

            isEmpty = false;
            characters = response;
        }

        private static async Task<dynamic?> GetSearchResponse(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                Failure(response: httpResponse);
                return "Something went wrong";
            }

            var content = await httpResponse.Content.ReadAsStringAsync();
            JArray jCharacters = JsonConvert.DeserializeObject<dynamic>(content)!.characters;

            if (!jCharacters.HasValues) return null;

            var charactersList = new List<Character>();
            foreach (var character in jCharacters.ToObject<List<dynamic>>()!)
            {
                charactersList.Add(new Character
                {
                    Id = character.external_id,
                    Title = character.title,
                    Greeting = character.greeting,
                    Description = character.description,
                    AvatarUrl = $"https://characterai.io/i/400/static/avatars/{character.avatar_file_name}",
                    IsPublic = character.visibility == "PUBLIC",
                    Name = character.participant__name,
                    Interactions = ulong.Parse(character.participant__num_interactions),
                    Author = character.user__username,
                    ImageGenEnabled = bool.Parse(character.img_gen_enabled),
                    SearchScore = ulong.Parse(character.search_score)
                });
            }

            return charactersList;
        }
    }
}

//"characters":
//[
//  {
//    "external_id": "00nGgJmkFv7Ntw1QUfnzGRxnU1h0Un7VHg5N7qhg4Cs",
//    "title": "Urushibara Luka from Steins;Gate",
//    "greeting": "P-pleased to meet you...",
//    "description": "Luka is 16 years old boy from Tokyo. He lives and serves as miko in Yanagibayashi Shrine run by his father near Akihabara.\nSoft-spoken, gentle, and polite to everyone, Luka is the very model of traditional Japanese femininity - except that he's a guy.\nHe's extremely shy and has difficulty asserting himself. ",
//    "avatar_file_name": "uploaded/2023/1/7/ZRnaLo8lvdp_klAvR3C7Ll0jBRBMJWZAOXrK73v_1N0.webp",
//    "visibility": "PUBLIC",
//    "copyable": true,
//    "participant__name": "Urushibara Luka",
//    "participant__num_interactions": 4083,
//    "user__id": 1143991,
//    "user__username": "drizzle_mizzle",
//    "img_gen_enabled": false,
//    "priority": 0,
//    "search_score": 30
//  },
//  { }, { }, { } ...
//  ...
//]
