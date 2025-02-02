using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using System.Net;
using System.Text;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;

namespace OpenPayment.Tests
{
    public class PaymentApiTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly ITestOutputHelper _testOutputHelper;

        public PaymentApiTests(WebApplicationFactory<Program> factory, ITestOutputHelper testOutputHelper)
        {
            _client = factory.CreateClient();
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task ConflictWhenMultipleRequestsWithSameClientIdAreComingInBeforeItIsProcessed()
        {
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < 5; i++)
            {
                var requestBody = new
                {
                    debtorAccount = "DE12345678901234567890",
                    creditorAccount = "FR12345678901234567890",
                    instructedAmount = 100.50 + i,
                    currency = "EUR"
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var request = new HttpRequestMessage(HttpMethod.Post, "/payments")
                {
                    Content = content
                };
                request.Headers.Add("Client-ID", "123e4567-e89b-12d3-a456-426614174000");

                tasks.Add(_client.SendAsync(request));
            }

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                var responseBody = await response.Content.ReadAsStringAsync();

                _testOutputHelper.WriteLine($"Response: {response}\nBody: {responseBody}");
            }

            var createdResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Created).Count();
            var conflictResponses = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).Count();

            Assert.True(createdResponses == 1, $"The amount of successfull responses was not 1");
            Assert.True(conflictResponses == 4, $"The amount of conflict responses was not 4");
        }

        [Fact]
        public async Task GetTransactionsByIban()
        {
            var requestBody1 = new
            {
                debtorAccount = "DE12345678901234567890",
                creditorAccount = "FR12345678901234567890",
                instructedAmount = 150.00,
                currency = "EUR"
            };

            var requestBody2 = new
            {
                debtorAccount = "FR12345678901234567890",
                creditorAccount = "NL98765432109876543210",
                instructedAmount = 200.00,
                currency = "EUR"
            };

            var content1 = new StringContent(JsonSerializer.Serialize(requestBody1), Encoding.UTF8, "application/json");
            var request1 = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = content1
            };
            request1.Headers.Add("Client-ID", Guid.NewGuid().ToString());

            var response1 = await _client.SendAsync(request1);
            Assert.Equal(HttpStatusCode.Created, response1.StatusCode);

            var content2 = new StringContent(JsonSerializer.Serialize(requestBody2), Encoding.UTF8, "application/json");
            var request2 = new HttpRequestMessage(HttpMethod.Post, "/payments")
            {
                Content = content2
            };
            request2.Headers.Add("Client-ID", Guid.NewGuid().ToString());

            var response2 = await _client.SendAsync(request2);
            Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

            await Task.Delay(2200);

            var ibanSearchResponse = await _client.GetAsync($"/accounts/{requestBody1.creditorAccount}/transactions");
            var ibanSearchBody = await ibanSearchResponse.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, ibanSearchResponse.StatusCode);
            Assert.Contains("FR12345678901234567890", ibanSearchBody);
            _testOutputHelper.WriteLine($"Response: {ibanSearchResponse}\nBody: {ibanSearchBody}");
        }
    }
}
