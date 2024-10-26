namespace CharacterAi.Client.Models.Common;

public class AuthorizedUser
{
    public string Token { get; set; }
    public string UserId { get; set; }
    public string Username { get; set; }
    public string UserEmail { get; set; }
    public string? UserImageUrl { get; set; } // uploaded/...
}
