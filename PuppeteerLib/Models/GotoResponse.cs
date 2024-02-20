namespace PuppeteerLib.Models
{
    public class GotoResponse
    {
        //public IResponse OriginalResponse { get; }
        //public Payload OriginalRequestPayload { get; }
        public string? Content { get; }
        public bool IsSuccessful { get; }
        public bool InQueue { get; }
        public System.Net.HttpStatusCode? Status { get; set; } = null!;
        public GotoResponse(string? responseContent, bool isSuccessful)
        {
            Content = responseContent;
            IsSuccessful = isSuccessful;
            InQueue = Content?.Contains("Waiting Room") ?? false;
        }
    }
}
