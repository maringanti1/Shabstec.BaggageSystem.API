using BlazorApp.API.Models;
using Docker.DotNet.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Cosmos.Core;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OfficeOpenXml;
using OfficeOpenXml.Drawing;
using RabbitMQ.Client;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
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
        public async Task<IActionResult> GetAllConfigurations(string loggedInUser, string organisation)
        {
            //string organisationData = TransformOrganization(organisation);
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                using (cosmosClient)
                {

                    var query = new QueryDefinition("SELECT * FROM c where c.entity='Configuration' and c.Organization ='" + organisation + "'");
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

        private async Task<List<ExcelColumn>> GetValuesFromExcelAsync(string filePath, string sheetName)
        {
            return await Task.Run(() =>
            {
                ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
                List<ExcelColumn> myModels = new List<ExcelColumn>();

                using (var package = new ExcelPackage(new FileInfo(filePath)))
                {
                    var worksheet = package.Workbook.Worksheets[sheetName];

                    if (worksheet != null)
                    {
                        // Assuming you want to read data from the first 10 rows
                        for (int row = 2; row <= worksheet.Dimension.End.Row; row++)
                        {
                            string column1Value = worksheet.Cells[row, 1].Text;
                            string column2Value = worksheet.Cells[row, 2].Text;

                            if (!string.IsNullOrEmpty(column1Value))
                            {
                                // Create an ExcelColumn object and add it to the list
                                myModels.Add(new ExcelColumn
                                {
                                    Code = column1Value,
                                    Name = column2Value
                                    // Add more properties as needed
                                });
                            }
                        }
                    }
                }

                return myModels;
            });
        }

        [NonAction]
        static string TransformOrganization(string input)
        {
            // Remove numbers and special characters
            string alphanumericOnly = new string(input
                .Where(c => Char.IsLetter(c))
                .ToArray());

            // Convert to lowercase
            string lowercase = alphanumericOnly.ToLower();

            return lowercase;
        }
        [HttpGet("DownloadMessage")]
        public IActionResult DownloadMessage(BrokerMessage xmlData)
        {
            // Convert the XML string to bytes
            var xmlBytes = Encoding.UTF8.GetBytes(xmlData.Message);

            // Set the content-disposition header to force download
            var contentDisposition = new Microsoft.Net.Http.Headers.ContentDispositionHeaderValue("attachment")
            {
                FileName = xmlData.ClientID +"_"+ xmlData.CreateDate.ToString() +".xml"
            };
            Response.Headers.Add("Content-Disposition", contentDisposition.ToString());

            // Return the XML file as a FileContentResult
            return File(xmlBytes, "application/xml");
        }
        [HttpGet("GetAllPublishedMessages")]
        public async Task<IActionResult> GetAllPublishedMessages(string loggedInUser, string organisation)
        {
            string organisationData = TransformOrganization(organisation);
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                using (cosmosClient)
                {
                    var query = new QueryDefinition("SELECT * FROM c where c.entity='Sender' and c.ClientID = '" + organisationData + "'");
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

        [HttpGet("GetAllMessages")]
        public async Task<IActionResult> GetAllMessages(string loggedInUser, string organisation)
        {
            string organisationData = TransformOrganization(organisation);
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            try
            {
                using (cosmosClient)
                {
                    var query = new QueryDefinition("SELECT * FROM c where c.entity='Reciever' and c.ClientID = '" + organisationData + "'");
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
        private static QueueClient topicClient;

        [HttpPost("UploadMessage")]
        public async Task<IActionResult> UploadMessage([FromBody] MessageModel model)
        {
            try
            {
                if (model == null)
                {
                    return BadRequest("Invalid model data");
                }

                if (string.IsNullOrWhiteSpace(model.Topic))
                {
                    return BadRequest("Invalid or missing topic");
                }

                string eventData = model.EventData;
                // Assuming you have an instance of YourModel called 'model'
                model.Topic = model.Topic?.Replace(" ", "") + "_transmitter";

                try
                {

                    topicClient = new QueueClient(_cosmosDbSettings.PublisherSvcBusConnString,
                        model.Topic);
                    Microsoft.Azure.ServiceBus.Message serviceBusMessage = 
                        new Microsoft.Azure.ServiceBus.Message(Encoding.UTF8.GetBytes(eventData)); 
                    await topicClient.SendAsync(serviceBusMessage);

                    return Ok("Message sent successfully");
                }
                catch (Exception ex)
                {
                    // Log the exception or handle it based on your application's needs
                    return StatusCode(500, $"An error occurred: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                // Log the exception or handle it based on your application's needs
                return StatusCode(500, $"An error occurred: {ex.Message}");
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
                        if (string.IsNullOrEmpty(model.id))
                        {
                            model.id = System.Guid.NewGuid().ToString(); 
                        }                        
                        model.entity = "Configuration";
                        var response = await container.UpsertItemAsync(model, new PartitionKey("Configuration"));

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

        [HttpGet("GetAirportCodes")]
        public async Task<IActionResult> GetAirportCodes()
        {

            string templatePath = @"Template/LookupCodes.xlsx";

            List<ExcelColumn> columns = await GetValuesFromExcelAsync(templatePath, "Airports");
            columns = columns.ToList();
            return Ok(columns);

        }
        [HttpGet("GetAirlineCodes")]
        public async Task<IActionResult> GetAirlineCodes()
        {
            string templatePath = @"Template/LookupCodes.xlsx";
            List<ExcelColumn> columns = await GetValuesFromExcelAsync(templatePath, "Airlines");
            return Ok(columns);

        }

        [HttpGet("GetAllCountryCodes")]
        public async Task<IActionResult> GetAllCountryCodes()
        {
            {
                string templatePath = @"Template/Countries.xml";
                string templateContent = System.IO.File.ReadAllText(templatePath);
                List<string> countryCodes = new List<string>();

                try
                {
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.LoadXml(templateContent);

                    // Select all "code" elements under "countries"
                    XmlNodeList codeNodes = xmlDoc.SelectNodes("/CountriesList/countries/code");

                    foreach (XmlNode codeNode in codeNodes)
                    {
                        // Add the code value to the list
                        countryCodes.Add(codeNode.InnerText.Trim());
                    }
                    return Ok(countryCodes);
                }
                catch (Exception ex)
                {
                    // Handle any exceptions and return an error response
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }
        }
    }
    public class ExcelColumn
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }
}
