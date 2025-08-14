using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using static System.Net.WebRequestMethods;


namespace FoodyExamPrep
{
    [TestFixture]
    public class FoodyTests
    {
        private RestClient _client;
        private static string createdFoodId;
        private const string baseUrl = 
            "http://softuni-qa-loadbalancer-2137572849.eu-north-1.elb.amazonaws.com:86";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJwtToken("Leni", "Leni123456");

            var option = new RestClientOptions(baseUrl)
            {
                Authenticator = new JwtAuthenticator(token)
            };

            _client = new RestClient(option);
        }

        private string GetJwtToken(string username, string password)
        {
            var loginClient = new RestClient(baseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);

            request.AddJsonBody(new { username, password });

            var response = loginClient.Execute(request);

            var json = JsonSerializer.Deserialize<JsonElement>(response.Content);

            return json.GetProperty("accessToken").GetString() ?? string.Empty;

        }

        [Test, Order(1)]
        public void FoodyTest_CreateNewFood_WithAllRequiredFields_ShouldSucceed()
        {
            var food = new
            {
                Name = "First food",
                Description = "description",
                Url = ""

            };

            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = _client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        }

   
        [OneTimeTearDown]

        public void Cleanup()
        {
            _client?.Dispose();
        }
    }
}