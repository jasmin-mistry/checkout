using System;
using System.Linq;
using System.Threading.Tasks;
using Bank.Client;
using Bank.Client.Auth;
using Bank.Client.Payment;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Shouldly;
using WireMock.Admin.Mappings;

namespace FunctionalTests.BankApi
{
    [TestFixture]
    public class AuthClientTests : TestBase
    {
        private BankClient bankClient;
        private Payment payment;
        private const string TokenPath = "/login/v1/oauth2/token";

        private MappingModel GetAuthMapping()
        {
            var mapping = new MappingModel
            {
                Guid = Guid.Parse("425db3ef-06ca-4a69-b5c9-a9384cabc34e"),
                Title = "Post Authentication",
                Request = new RequestModel
                {
                    Methods = new[] {"Post"},
                    Path = JObject.Parse(
                        "{\"Matchers\": [{\"Name\": \"WildcardMatcher\",\"Pattern\": \"/login/v1/oauth2/token\",\"IgnoreCase\": true}]}")
                },
                Response = new ResponseModel
                {
                    StatusCode = 200,
                    BodyAsJson = new Token
                    {
                        AccessToken = "access_token",
                        ExpiresIn = "20",
                        TokenType = "token_type"
                    }
                }
            };
            return mapping;
        }

        [SetUp]
        public void BeforeEachTest()
        {
            bankClient = BuildBankClient();
        }

        [Test, Order(1)]
        public async Task MakingTwoApiRequests_GetsNewToken_WhenTokenIsExpired()
        {
            await StubAdminClient.ResetRequestsAsync();

            payment = new Payment
            {
                CardNumber = "1234-1234-1234-1234",
                Amount = 1000m,
                Currency = "GBP",
                Cvv = "123",
                ExpiryMonth = "Dec",
                ExpiryYear = "2022"
            };

            await bankClient.Payment.Process(payment);
            await bankClient.Payment.Process(payment);

            var requestsMade = await StubAdminClient.GetRequestsAsync();

            requestsMade.Count(x => x.Request.AbsolutePath == TokenPath &&
                                    x.Response.StatusCode.Equals(200L))
                .ShouldBe(2);
        }

        [Test, Order(2)]
        public async Task MakingTwoApiRequests_GetsTokenOnlyOnce_WhenTokenIsNotExpired()
        {
            await StubAdminClient.PutMappingAsync(Guid.Parse("425db3ef-06ca-4a69-b5c9-a9384cabc34e"), GetAuthMapping());
            await StubAdminClient.ResetRequestsAsync();

            await bankClient.Payment.Process(payment);

            var requestsMade = await StubAdminClient.GetRequestsAsync();

            requestsMade.Count(r => r.Request.AbsolutePath == TokenPath && r.Response.StatusCode.Equals(200L))
                .ShouldBe(1);
        }
    }
}