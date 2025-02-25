using CharacterAi.Client;

namespace CharacterAi.Test;


public class Tests
{
    [SetUp]
    public void Setup()
    {
    }


    [Test]
    public async Task SendLoginEmail()
    {
        const string EMAIL = "drizzle.n.mizzle87@gmail.com";

        using var client = new CharacterAiClient();

        await client.SendLoginEmailAsync(EMAIL);

        Assert.Pass();
    }


    [Test]
    public async Task LoginByLink()
    {
        const string LINK_FROM_EMAIL = "https://character.ai/login/0a04161d-e721-427a-9ecc-be1faa4a04d6";
        const string EXPECTED_USERNAME = "Neko87";

        using var client = new CharacterAiClient();
        var user = await client.LoginByLinkAsync(LINK_FROM_EMAIL);

        Assert.That(user.Username, Is.EqualTo(EXPECTED_USERNAME));
    }


    private const string AUTH_TOKEN = "";
    
    [Test]
    public async Task GetCharacterInfo()
    {
        using var client = new CharacterAiClient();
        var character = await client.GetCharacterInfoAsync("00nGgJmkFv7Ntw1QUfnzGRxnU1h0Un7VHg5N7qhg4Cs", AUTH_TOKEN);

        Assert.That(character.participant__name!, Does.StartWith("Urushibara"));
    }


    [Test]
    public async Task Search()
    {
        using var client = new CharacterAiClient();
        var result = await client.SearchAsync("Urushibara", AUTH_TOKEN);

        Assert.That(result.Any(character => character.participant__name.Contains("Urushibara")));
    }


    [Test]
    public async Task GetChats()
    {
        using var client = new CharacterAiClient();
        var result = await client.GetChatsAsync("00nGgJmkFv7Ntw1QUfnzGRxnU1h0Un7VHg5N7qhg4Cs", AUTH_TOKEN);

        Assert.That(result, Is.Not.Empty);
    }


    [Test]
    public async Task CreateNewChat()
    {
        const string USER_ID = "1148415";

        using var client = new CharacterAiClient();
        var chatId = client.CreateNewChat("00nGgJmkFv7Ntw1QUfnzGRxnU1h0Un7VHg5N7qhg4Cs", USER_ID, AUTH_TOKEN);

        Assert.That(chatId is not null);
    }
}
