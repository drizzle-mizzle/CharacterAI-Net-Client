using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using CharacterAI.Services;

namespace CharacterAI.Models
{
    public class HistoriesResponse : CommonService
    {
        public List<Character>? Histories { get; }
        public string? ErrorReason { get; }
        public bool IsSuccessful { get => ErrorReason is null; }
        public bool IsEmpty { get => Histories is not null; }

        public HistoriesResponse(HttpResponseMessage httpResponse)
        {
            dynamic? responseParsed = ParseHistoriesResponse(httpResponse).Result;

            if (responseParsed is null) return;
            if (responseParsed is string)
            {
                ErrorReason = responseParsed;

                return;
            }

            Histories = responseParsed;
        }

        private static async Task<dynamic?> ParseHistoriesResponse(HttpResponseMessage httpResponse)
        {
            if (!httpResponse.IsSuccessStatusCode)
            {
                Failure(response: httpResponse);
                return "Something went wrong";
            }

            var content = await httpResponse.Content.ReadAsStringAsync();
            JArray jHistories = JsonConvert.DeserializeObject<dynamic>(content)!.histories;

            if (!jHistories.HasValues) return null;

            var historiesList = new List<History>();
            foreach (var history in jHistories.ToObject<List<dynamic>>()!)
                historiesList.Add(new History(history));

            return historiesList;
        }
    }
}
