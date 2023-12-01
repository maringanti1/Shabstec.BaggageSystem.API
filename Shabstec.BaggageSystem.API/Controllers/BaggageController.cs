using BlazorApp.API.Models; 
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Azure.Core.HttpHeader; 

namespace WebApplication1.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class BaggageController : ControllerBase
    { 

        private readonly ILogger<BaggageController> _logger;
        private readonly CosmosDbSettings _cosmosDbSettings;
        public BaggageController(ILogger<BaggageController> logger, 
            IOptions<CosmosDbSettings> cosmosDbSettings)
        {
            _cosmosDbSettings = cosmosDbSettings.Value;
            _logger = logger;
        }


        
        [HttpGet("GetAllConfigurations")]
        public async Task<IActionResult> GetAllConfigurations()
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                using (cosmosClient)
                {
                     
                    var query = new QueryDefinition("SELECT * FROM c where c.ServiceID='Configuration'");
                    var iterator = container.GetItemQueryIterator<ClientConfiguration>(query);

                    var configurations = new List<ClientConfiguration>();

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        configurations.AddRange(response.ToList());
                    }

                    return Ok(configurations);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return an error response
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpGet("GetAllMessages")]
        public async Task<IActionResult> GetAllMessages()
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                using (cosmosClient)
                { 
                    var query = new QueryDefinition("SELECT * FROM c where c.ServiceID='Message'");
                    var iterator = container.GetItemQueryIterator<BrokerMessage>(query);

                    var configurations = new List<BrokerMessage>();

                    while (iterator.HasMoreResults)
                    {
                        var response = await iterator.ReadNextAsync();
                        configurations.AddRange(response.ToList());
                    }

                    return Ok(configurations);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return an error response
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }


        [HttpPost("SaveConfiguration")]
        public async Task<IActionResult> SaveConfiguration([FromBody] ClientConfiguration model)
        {
            try
            {
                var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
                var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
                var container = database.GetContainer(_cosmosDbSettings.ContainerName);

                // Check if model is valid
                if (ModelState.IsValid)
                {
                    // You don't need to create a sampleConfiguration, use the provided model.
                    using (cosmosClient)
                    {
                        model.id = System.Guid.NewGuid().ToString();
                        model.ServiceID = "Configuration";
                        var response = await container.CreateItemAsync(model, new PartitionKey("Configuration"));

                        if (response.StatusCode == System.Net.HttpStatusCode.OK || response.StatusCode == System.Net.HttpStatusCode.Created)
                        {
                            // Data saved successfully
                            return Ok("Configuration added successfully");
                        }
                        else
                        {
                            // Handle failure to save data.
                            return BadRequest("Failed to save configuration");
                        }
                    }
                }
                else
                {
                    // Model is not valid, return a bad request with validation errors
                    return BadRequest(ModelState);
                }
            }
            catch (Exception ex)
            {
                // Handle any exceptions and return an error response
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

    }


}
