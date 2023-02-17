using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CharacterAI.Services;

namespace CharacterAI.Models
{
    public class SearchResponse : CommonService
    {
        public List<Character> Characters { get; } = new();
        public string? ErrorReason { get; }
        public bool IsSuccessful { get => ErrorReason is null; }
        public bool IsEmpty { get => Characters is null; }

        public SearchResponse(HttpResponseMessage httpResponse)
        {
            dynamic? responseParsed = ParseSearchResponse(httpResponse).Result;

            if (responseParsed is null) return;
            if (responseParsed is string)
            {
                ErrorReason = responseParsed;

                return;
            }

            Characters = responseParsed;
        }

        private static async Task<dynamic?> ParseSearchResponse(HttpResponseMessage httpResponse)
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
                charactersList.Add(new Character(character));

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
