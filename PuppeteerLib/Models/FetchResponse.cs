using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PuppeteerLib.Models
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
                IsBlocked = false;
                return;
            }
            
            var result = JsonConvert.DeserializeObject<dynamic>(response.ToString());
            
            Status = result?.status;
            Content = result?.content;
            InQueue = Content?.Contains("Waiting Room") ?? false;
            IsBlocked = Content?.Contains("Just a moment") ?? false;

            IsSuccessful = !InQueue && $"{result?.status}" == "200";
        }
    }
}
