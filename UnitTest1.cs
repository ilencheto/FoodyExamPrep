using System.Net;
using System.Text.Json;
using Foody.Models;
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
        private static string lastCreatedFoodId;
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
        public void FoodyTest_CreateNewFood_WithAllRequiredFields_ShouldReturnCreated()
        {
            //Arrange
            var food = new
            {
                Name = "First food",
                Description = "description",
                Url = ""

            };

            //Act
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = _client.Execute(request);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var foodBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            lastCreatedFoodId = foodBody.foodId.ToString();

            Assert.That(lastCreatedFoodId, Is.Not.Null);
        }

        [Test, Order(2)]

        public void FoodyTest_EditTitleOfCreatedFood_ShouldSucceed()
        {
            // Arrange
            string newName = "New Edited Food";
            string expectedMessage = "Successfully edited";

            var request = new RestRequest($"/api/Food/Edit/{lastCreatedFoodId}");
            request.AddJsonBody(new[]
            {
             new
             {

                path = "/name",
                op = "replace",
                value = newName
             }
            });

            // Act
            var response = _client.Execute(request, Method.Patch);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK),
                    "Expected status code OK (200)");
                var foodBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

                Assert.That(foodBody, Is.Not.Null);

                Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
                Assert.That(response.Content, Does.Contain("Successfully edited"));
            });
        }

        [Test, Order(3)]

        public void FoodyTest_GetAllFoods_ResponseIsNotEmptyArray()
        {
            // Arrange        
            var request = new RestRequest("/api/Food/All");

            // Act
            var response = _client.Execute(request, Method.Get);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), 
                    $"Status code is {response.StatusCode}");

                var allFoods = JsonSerializer.Deserialize<ApiResponseDTO[]>(response.Content);

                Assert.That(allFoods, Is.Not.Null);

                Assert.That(allFoods.Length, Is.GreaterThan(0), "Returned items are less than one");
            });

        }

        [Test, Order(4)]

        public void FoodyTest_DeleteCreatedFood_ShouldReturnDeletedSuccessfully()
        {
            // Arrange           
            string expectedMessage = "Deleted successfully!";

            var request = new RestRequest($"/api/Food/Delete/{lastCreatedFoodId}");

            // Act
            var response = _client.Execute(request, Method.Delete);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"Status code is {response.StatusCode}");
            var foodBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
        }

        [Test, Order(5)]

        public void FoodyTest_CreateFoodWithoutRequiredFeeld_ShouldReturnBadRequest()
        {
            //Arrange
            var food = new
            {
                Name = "",
                Description = "",
            };

            //Act
            var request = new RestRequest("/api/Food/Create", Method.Post);
            request.AddJsonBody(food);

            var response = _client.Execute(request);

            //Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), 
                $"Status code is {response.StatusCode}");
        }

        [Test, Order(6)]

        public void FoodyTest_EditNonExistingFood_ShouldReturnNotFound()
        {
            // Arrange
            string wrongFoodId = "9";
            string newName = "New Edited Food";
            string expectedMessage = "No food revues...";

            var request = new RestRequest($"/api/Food/Edit/{wrongFoodId}");
            request.AddJsonBody(new[]
            {
             new
             {

                path = "/name",
                op = "replace",
                value = newName
             }
            });

            // Act
            var response = _client.Execute(request, Method.Patch);

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound), $"Status code is {response.StatusCode}");

            Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

            var foodBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(foodBody, Is.Not.Null);

            Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
        }

        [Test, Order(7)]

        public void FoodyTest_DeleteNonExistingFood_ShouldReturnBadRequest()
        {
            //Arrange
            string wrongFoodId = "9";
            string expectedMessage = "Unable to delete this food revue!";

            var request = new RestRequest($"/api/Food/Delete/{wrongFoodId}");

            // Act
            var response = _client.Execute(request, Method.Delete);

            //Assert
            Assert.Multiple(() =>
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest), 
                    $"Status code is {response.StatusCode}");
                Assert.That(response.Content, Is.Not.Null, "Response content is not as expected");

                var foodBody = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

                Assert.That(foodBody, Is.Not.Null);
                Assert.That(foodBody.Msg, Is.EqualTo(expectedMessage), "Message is not as expected");
            });
        }

        [OneTimeTearDown]

        public void Cleanup()
        {
            _client?.Dispose();
        }
    }
}