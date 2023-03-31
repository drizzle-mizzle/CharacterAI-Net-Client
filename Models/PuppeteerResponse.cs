using System;
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
        public bool IsSuccessful { get; }
        public string? Content { get; }

        public PuppeteerResponse(string? responseContent, bool status)
        {
            IsSuccessful = status;
            Content = responseContent;
        }
    }
}
