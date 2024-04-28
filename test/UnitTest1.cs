using NUnit.Framework.Internal;
using NLog;
using System.Security.Cryptography.X509Certificates;

namespace CharacterAiNet.Test
{
    public class Tests
    {
        private static readonly NLog.Logger _log = LogManager.GetCurrentClassLogger();
        private const string _token = "Token 6afc431a3c3eeb71df52f274fb8a724cd09ba6a7";

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
            var character = await client.GetCharacterInfo("eGPYvuu9WnIzP4gHbkgwe3cTtqwfnLi5QUNip_q8Le4", _token);

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