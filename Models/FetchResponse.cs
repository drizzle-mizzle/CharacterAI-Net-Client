using CharacterAI.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CharacterAI.Models
{
    public class FetchResponse
    {
        public string? Content { get; set; }
        public string? Status { get; set; }
        public bool IsSuccessful { get; set; }
        public bool InQueue { get; set; }
        public bool IsBlocked { get; set; }

        public FetchResponse(JToken? response)
        {
            if (response is null)
            {
                IsSuccessful = false;
                InQueue = false;
                return;
            }
            
            var result = JsonConvert.DeserializeObject<dynamic>(response.ToString());
            IsSuccessful = $"{result?.status}" == "200";
            Status = result?.status;
            Content = result?.content;
            InQueue = Content?.Contains("Waiting Room") ?? false;
            IsBlocked = Content?.Contains("Just a moment") ?? false;
        }
    }
}
