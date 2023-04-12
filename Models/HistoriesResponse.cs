using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using CharacterAI.Services;

namespace CharacterAI.Models
{
    public class HistoriesResponse : CommonService
    {
        //public List<Character>? Histories { get; }
        //public string? ErrorReason { get; }
        //public bool IsSuccessful => ErrorReason is null;
        //public bool IsEmpty => Histories is null;

        //public HistoriesResponse(PuppeteerResponse httpResponse)
        //{
        //    dynamic? responseParsed = ParseHistoriesResponse(httpResponse);

        //    if (responseParsed is null) return;
        //    if (responseParsed is string)
        //    {
        //        ErrorReason = responseParsed;

        //        return;
        //    }

        //    Histories = responseParsed;
        //}

        //private static dynamic? ParseHistoriesResponse(PuppeteerResponse response)
        //{
        //    if (!response.IsSuccessful)
        //    {
        //        Failure(response: response);
        //        return "Something went wrong";
        //    }

        //    var content = response.Content!;
        //    JArray jHistories = JsonConvert.DeserializeObject<dynamic>(content)!.histories;

        //    if (!jHistories.HasValues) return null;

        //    var historiesList = new List<History>();
        //    foreach (var history in jHistories.ToObject<List<dynamic>>()!)
        //        historiesList.Add(new History(history));

        //    return historiesList;
        //}
    }
}
