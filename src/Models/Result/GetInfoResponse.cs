using Microsoft.VisualBasic;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace CharacterAiNetApiWrapper.Models.Result;

public class GetInfoResponse
{
    public Character? Character { get; } 
    public string? ErrorReason { get; }
    public bool IsSuccessful => ErrorReason is null;

    public GetInfoResponse(dynamic response)
    {
        try
        {
            var contentParsed = JsonConvert.DeserializeObject<dynamic>(response.Content)
                                ?? throw new Exception("No content");

            var characterJson = contentParsed.character
                                ?? throw new Exception("Something went wrong");

            if (characterJson is JArray)
                throw new Exception($"Failed to get character info. Perhaps the character is private?{(contentParsed.error is string e ? $" | Error: {e}" : "")}");

            Character = new Character(characterJson);
        }
        catch (Exception e)
        {
            Character = null;
            ErrorReason = e.Message;
        }
    }
}