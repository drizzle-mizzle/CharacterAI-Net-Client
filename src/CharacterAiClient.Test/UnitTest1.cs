using NLog;

namespace CharacterAi.Test
{
    public class Tests
    {
        private static readonly NLog.Logger _log = LogManager.GetCurrentClassLogger();
        private const string _token = "Token 6afc431a3c3eeb71df52f274fb8a724cd09ba6a7";
        private const string _webNextAuthToken = "Fe26.2*1*f3e7904ceeb7c64efa0e5b2090908c78b2168e43abc717e08a850e199ce7c313*YQ3yen-y-ExIGrG6g1F_VA*7DxerVz9MDgoCzXgZlwCAw9u2Aizzp_wT1D-S9gGcMyQN11EszG46bCXLNx9LJhB87m7cGOurgHTdm4vNb4vZA**200d81b5457a8f97437eb367e14cf83d95f66737da670a96a281042b0c8e536d*j9Ium5l4w2ok8VDFei8T-eTdXNQVcbBEyiD2eXCUoJE~2";

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public async Task Init()
        {
            using var client = new CharacterAiClient();
            await client.InitializeAsync();

            Assert.Pass();
        }

        [Test]
        public async Task GetUserId()
        {
            using var client = new CharacterAiClient();
            var id = await client.GetUserIdAsync(authToken: _token, webNextAuthToken: _webNextAuthToken);

            Assert.Pass();
        }

        [Test]
        public async Task CreateNewChat()
        {
            using var client = new CharacterAiClient();
            var id = await client.CreateNewChatAsync("eGPYvuu9WnIzP4gHbkgwe3cTtqwfnLi5QUNip_q8Le4", 1148415, _token);

            Assert.Pass();
        }

        [Test]
        public async Task Ping()
        {
            using var client = new CharacterAiClient();
            bool ok = await client.PingAsync();

            Assert.Pass();
        }

        [Test]
        public async Task GetInfo()
        {
            using var client = new CharacterAiClient();
            var character = await client.GetCharacterInfoAsync("eGPYvuu9WnIzP4gHbkgwe3cTtqwfnLi5QUNip_q8Le4", _token);

            Assert.Pass();
        }

        [Test]
        public async Task Search()
        {
            
            using var client = new CharacterAiClient();
            var result = await client.SearchAsync("Urushibara", _token);

            Assert.Pass();
        }

        [Test]
        public async Task GetChats()
        {
            using var client = new CharacterAiClient();
            var result = await client.GetChatsAsync("U3dJdreV9rrvUiAnILMauI-oNH838a8E_kEYfOFPalE", _token);

            Assert.Pass();
        }


    }
}
