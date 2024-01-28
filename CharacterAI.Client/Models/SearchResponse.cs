using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PuppeteerLib.Models;
using static SharedUtils.Common;

namespace CharacterAI.Models
{
    public class SearchResponse
    {
        public List<Character> Characters { get; } = new();
        public string? ErrorReason { get; }
        public string OriginalQuery { get; }
        public bool IsSuccessful => ErrorReason is null;
        public bool IsEmpty => Characters.Count == 0;

        public SearchResponse(PuppeteerResponse response, string query)
        {
            OriginalQuery = query;
            dynamic? responseParsed = ParseSearchResponse(response);

            if (responseParsed is null) return;
            if (responseParsed is string)
            {
                ErrorReason = responseParsed;

                return;
            }

            Characters = responseParsed;
        }

        private static dynamic? ParseSearchResponse(PuppeteerResponse response)
        {
            if (!response.IsSuccessful)
            {
                LogRed(response.Content);
                return "Something went wrong";
            }

            JArray? jCharacters = JsonConvert.DeserializeObject<dynamic>(response.Content!)?.characters;
            if (jCharacters is null || !jCharacters.HasValues) return null;

            var charactersList = new List<Character>();
            foreach (var character in jCharacters.ToObject<List<dynamic>>()!)
                charactersList.Add(new Character(character));

            return charactersList;
        }
    }
}
