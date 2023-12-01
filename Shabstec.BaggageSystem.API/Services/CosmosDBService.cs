namespace BlazorApp.API.Services
{ 
    using BlazorApp.API.Models; 
    using Microsoft.Azure.Cosmos;
    using Microsoft.Azure.Cosmos.Serialization.HybridRow;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.Threading.Tasks;
    using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
    using Container = Microsoft.Azure.Cosmos.Container;
    using User = Models.User;

    public class CosmosDBService
    { 
        private readonly CosmosDbSettings _cosmosDbSettings;
        private readonly EmailConfiguration _emailConfig;
        private readonly ILogger<UserController> _logger;
        public CosmosDBService(ILogger<UserController> logger, 
            IOptions<CosmosDbSettings> cosmosDbSettings,
            IOptions<EmailConfiguration> emailconfig)
        {
            _cosmosDbSettings = cosmosDbSettings.Value;
            _emailConfig = emailconfig.Value;
            _logger = logger;
        }
        public async Task RegisterUserAsync(Registration user)
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                user.id = System.Guid.NewGuid().ToString();
                user.ServiceID = "Registration";
                ItemResponse<Registration> response = await container.CreateItemAsync(user, new PartitionKey(user.ServiceID));
                _logger.LogInformation($"Registerd user with id: {response.Resource.Username}");
                EmailManager emailManager = new EmailManager(_emailConfig);
                emailManager.SendEmail(user);
                //Send an email
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error creating user: {ex.Message}");
            }
        }

        public async Task CreateUserAsync(AddUser user)
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {  
                user.id = System.Guid.NewGuid().ToString();
                user.ServiceID = "User";
                ItemResponse<AddUser> response = await container.CreateItemAsync(user, new PartitionKey(user.ServiceID));
                _logger.LogInformation($"Created user with id: {response.Resource.Username}");
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error creating user: {ex.Message}");
            }
        }

        public async Task<AddUser> GetUserAsync(string userId)
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                var response = await container.ReadItemAsync<AddUser>(userId, new PartitionKey("User"));
                return response.Resource;
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Handle the case when the user is not found
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error retrieving user: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> ValidateUserAsync(Login credentials)
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                var query = new QueryDefinition("SELECT * FROM c WHERE c.Username = @username AND c.Password = @password AND c.ServiceID=@serviceid")
                    .WithParameter("@username", credentials.Username)
                    .WithParameter("@password", credentials.Password)
                    .WithParameter("@serviceid", "User");

                var iterator = container.GetItemQueryIterator<AddUser>(query);
                var results = await iterator.ReadNextAsync();

                return results.Any();
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error validating user: {ex.Message}");
                throw;
            }
        }

        public async Task<List<User>> GetAllUsers()
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                var query = new QueryDefinition("SELECT * FROM c  where c.ServiceID='User'");
                var iterator = container.GetItemQueryIterator<User>(query);

                var users = new List<User>();

                while (iterator.HasMoreResults)
                {
                    var response = await iterator.ReadNextAsync();
                    users.AddRange(response.ToList()); 
                }

                return users;
            }
            catch (Exception ex)
            {
                _logger.LogInformation($"Error validating user: {ex.Message}");
                throw;
            }
        }
    }
}