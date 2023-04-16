using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CharacterAI.Models
{
    public class PuppeteerResponse
    {
        //public IResponse OriginalResponse { get; }
        //public Payload OriginalRequestPayload { get; }
        public string? Content { get; }
        public bool IsSuccessful { get; }
        public bool InQueue { get; }

        public PuppeteerResponse(string? responseContent, bool isSuccessful)
        {
            Content = responseContent;
            IsSuccessful = isSuccessful;
            InQueue = Content?.Contains("You are now in line") ?? false;
        }
    }
}
