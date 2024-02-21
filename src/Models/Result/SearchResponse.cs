using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CharacterAiNetApiWrapper.Models.Result;

public class SearchResponse
{
    public List<Character> Characters { get; } = [];
    public string? ErrorReason { get; }
    public string OriginalQuery { get; }
    public bool IsSuccessful => ErrorReason is null;
    public bool IsEmpty => Characters.Count == 0;

    public SearchResponse(dynamic response, string originalQuery)
    {
        OriginalQuery = originalQuery;

        try
        {
            if (!response.IsSuccessful)
                throw new Exception(response.Content ?? "Failed to fetch CharacterAI API");

            JArray? jCharacters = JsonConvert.DeserializeObject<dynamic>(response.Content!)?.characters;
            if (jCharacters is null || !jCharacters.HasValues)
                throw new Exception("Failed to parse the response");

            var charactersList = new List<Character>();
            foreach (var character in jCharacters.ToObject<List<dynamic>>()!)
                charactersList.Add(new Character(character));
                
            Characters = charactersList;
        }
        catch (Exception e)
        {
            ErrorReason = e.Message;
        }
    }
   
}