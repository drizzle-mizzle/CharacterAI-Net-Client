namespace CharacterAi.Client.Models;


public record CaiSendMessageInputData
{
    public string CharacterId { get; set; }
    public string ChatId { get; set; }
    public string Message { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public string UserAuthToken { get; set; }
}
