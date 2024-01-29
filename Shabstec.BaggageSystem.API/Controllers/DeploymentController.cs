using BlazorApp.API.Services;
using BlazorApp.API.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management.Automation;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json;
using System.Text;
using System.Xml;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Rest.Azure.Authentication;
using System.Xml.Linq;
using System.Configuration;
using Microsoft.Azure.Management.Graph.RBAC.Fluent.Models;
using WebApplication1.Controllers;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Azure.ServiceBus;
using Docker.DotNet;

[Route("api/deployment")]
[ApiController]
public class DeploymentController : ControllerBase
{

    private readonly CosmosDbSettings _cosmosDbSettings;
    private readonly ILogger<DeploymentController> _logger;
    // Inject IWebHostEnvironment in your class
    private readonly IWebHostEnvironment _hostingEnvironment;
    public DeploymentController(ILogger<DeploymentController> logger, IWebHostEnvironment hostingEnvironment,
        IOptions<CosmosDbSettings> cosmosDbSettings)
    {
        _logger = logger;
        _hostingEnvironment = hostingEnvironment;
        _cosmosDbSettings = cosmosDbSettings.Value;
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

    [HttpPost("BuildAndPushImages")]
    public async Task<IActionResult> BuildAndPushImages(ClientConfiguration configuration)
    {
        string ClientId = _cosmosDbSettings.ClientId;
        string ClientSecret = _cosmosDbSettings.ClientSecret;
        string TenantId = _cosmosDbSettings.TenantId;
        string SubscriptionId = _cosmosDbSettings.SubscriptionId;
        string armTemplatePath = "ARMTemplate.json";
        string resourceGroupName = "BIXSystem";
        string organisation = TransformOrganization(configuration.Organization);
        try
        {
            var azurecreds = SdkContext.AzureCredentialsFactory
                        .FromServicePrincipal(
                        ClientId, //client ID  
                        ClientSecret, //client secret  
                        TenantId, //tenant ID  
                        AzureEnvironment.AzureGlobalCloud);

            var azure1 = Microsoft.Azure.Management.Fluent.Azure.Configure()
            .Authenticate(azurecreds)
            .WithSubscription(SubscriptionId); //subscription ID 
            string armTemplateFilePath = armTemplatePath;
            string armTemplateContent = "";
            // Check if the file exists
            if (System.IO.File.Exists(armTemplateFilePath))
            {
                // Read the content of the ARM template file
                armTemplateContent = System.IO.File.ReadAllText(armTemplateFilePath);
                armTemplateContent = armTemplateContent.
                Replace("{PublishTo}", configuration.PublishTo.ToLower()).
                Replace("{SubscribeTo}", configuration.SubscribeTo.ToLower()).
                Replace("{Organisation}", organisation).
                Replace("{dnsNameLabel}", organisation).
                Replace("{RABBITMQ_DEFAULT_USER}", organisation).
                Replace("{RABBITMQ_DEFAULT_PASS}", organisation).
                Replace("{brokerappname}", $"{organisation}-brokersvc").
                Replace("{clientappname}", $"{organisation}-clientsvc");
                _logger.LogInformation(armTemplateContent);
            }
            else
            {
                _logger.LogInformation("The ARM template file does not exist.");
                return BadRequest("Unable to generate the docker images for this client. The ARM template file does not exist.");
            }


            Dictionary<string, Dictionary<string, string>> parameters =
                  new Dictionary<string, Dictionary<string, string>>();

            // Deploy the ARM template
            var deployment = azure1.Deployments.Define($"{organisation}-deployment")
                .WithExistingResourceGroup(resourceGroupName)
                .WithTemplate(armTemplateContent).WithParameters(parameters).
                WithMode(Microsoft.Azure.Management.ResourceManager.Fluent.Models.DeploymentMode.Incremental)
                .Create();


            // Optionally, you can check the deployment result
            if (deployment.ProvisioningState == Microsoft.Azure.Management.ResourceManager.Fluent.Models.ProvisioningState.Failed)
            {
                // Handle the failure, log errors, or throw an exception
                Console.WriteLine($"Deployment failed: {deployment.Name}");
                return BadRequest($"Deployment failed: {deployment.Name}");
            }
            else
            {
                Console.WriteLine("Deployment completed successfully");
                _logger.LogInformation("Docker images built and pushed successfully. ARM: " + armTemplateContent);

                var blnResult = SaveRabbitMQPODDetails(configuration.UserName,
                organisation,
                configuration.PublishTo,
                configuration.SubscribeTo,
                $"{organisation}-brokersvc",
                $"{organisation}-clientsvc", configuration.CompanyCode, configuration.Organization);
                if (blnResult.Result == true)
                {
                    await CreatePublisherTopicAndSubscription(_cosmosDbSettings.PublisherSvcBusConnString, organisation);

                    return Ok("Docker images built and pushed successfully for the user ." + configuration.Organization);
                }
                else
                {
                    return BadRequest("Docker images are generated successfully. But saving POD details failed.");
                }
            }


        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
            return BadRequest("Unable to generate the docker images for this client " + ex.Message);
        }
    }

    static async Task CreatePublisherTopicAndSubscription(string PublisherSvcBusConnString,
        string organisation)
    //string exchangeName)
    {
        var managementClient = new ManagementClient(PublisherSvcBusConnString);

        if (!await managementClient.QueueExistsAsync(organisation + "_transmitter"))
        {
            await managementClient.CreateQueueAsync(organisation + "_transmitter");
        }
        if (!await managementClient.QueueExistsAsync(organisation + "_reciever"))
        {
            await managementClient.CreateQueueAsync(organisation + "_reciever");
        }

        //if (!await managementClient.SubscriptionExistsAsync(organisation, exchangeName + "_publisher"))
        //{
        //    // Define a filter expression to filter messages with a specific eventType property
        //    var filterExpression = $"EventType = '"+ exchangeName +"'";

        //    // Create a RuleDescription with the filter expression
        //    var ruleDescription = new RuleDescription("EventTypeRule", new SqlFilter(filterExpression));

        //    // Create a subscription with the RuleDescription
        //    var subscriptionDescription = new SubscriptionDescription(organisation, exchangeName+"_publisher")
        //    {
        //        DefaultMessageTimeToLive = TimeSpan.FromDays(100), // Customize TTL as needed
        //                                                         // Add other subscription properties as needed
        //    };

        //    await managementClient.CreateSubscriptionAsync(subscriptionDescription, ruleDescription);
        //}
    }

    static async Task CreateSubscriberTopicAndSubscription(string PublisherSvcBusConnString, string organisation, string exchangeName)
    {
        var managementClient = new ManagementClient(PublisherSvcBusConnString);

        if (!await managementClient.TopicExistsAsync(organisation))
        {
            await managementClient.CreateTopicAsync(organisation);
        }

        if (!await managementClient.SubscriptionExistsAsync(organisation, exchangeName + "_subscriber"))
        {
            await managementClient.CreateSubscriptionAsync(organisation, exchangeName + "_subscriber");
        }
    }

    [HttpPost("SaveRabbitMQPODDetails")]
    public async Task<bool> SaveRabbitMQPODDetails(string UserName, string organisation,
        string PublishTo, string SubscribeTo, string brokersvc, string clientsvc, string companyCode, string originalOrgCode)
    {
        organisation = organisation.ToLower();
        try
        {
            var cosmosClient = new CosmosClient(_cosmosDbSettings.Endpoint, _cosmosDbSettings.Key);
            var database = cosmosClient.GetDatabase(_cosmosDbSettings.DatabaseName);
            var container = database.GetContainer(_cosmosDbSettings.ContainerName);
            var model = new BlazorApp.API.Models.PODConfiguration();
            // You don't need to create a sampleConfiguration, use the provided model.
            using (cosmosClient)
            {
                model.id = System.Guid.NewGuid().ToString();
                model.entity = "PODConfiguration";
                model.Organisation = organisation;
                model.CompanyCode = companyCode;
                model.UserName = UserName;
                model.PublishTo = PublishTo;
                model.SubscribeTo = SubscribeTo;
                model.PublisherSvcBusURL = _cosmosDbSettings.PublisherSvcBusConnString;
                model.SubscriberSvcBusURL = _cosmosDbSettings.SubscriberSvcBusConnString;
                model.PublisherSvcBusQueue = organisation + "_transmitter";
                model.SubscriberSvcBusQueue = organisation + "_reciever";
                model.RabbitMQUsername = organisation;
                model.RabbitMQHost = organisation + ".uksouth.azurecontainer.io";
                model.RabbitMQPassword = organisation;
                model.RabbitMQPort = 5672;
                model.brokersvc = brokersvc;
                model.clientsvc = clientsvc;
                var response = await container.CreateItemAsync(model, new PartitionKey("PODConfiguration"));
                if (response.StatusCode == System.Net.HttpStatusCode.OK ||
                response.StatusCode == System.Net.HttpStatusCode.Created)
                {
                    var query = new QueryDefinition("SELECT * FROM c where c.entity='Configuration' and c.Organization ='" + originalOrgCode + "'");
                    //var query = new QueryDefinition("SELECT * FROM c WHERE c.Organisation = @organisation AND " +
                    //    "c.entity = @entity")
                    //    .WithParameter("@organisation", originalOrgCode)
                    //    .WithParameter("@entity", "Configuration");

                    var iterator = container.GetItemQueryIterator<ClientConfiguration>(query);
                    while (iterator.HasMoreResults)
                    {
                        var configResponse = await iterator.ReadNextAsync();
                        // Assuming you are expecting only one result, you can return the first item
                        if (configResponse.Any())
                        {
                            var item = configResponse.First();
                            item.IsDeployed = 1;
                            var updateResponse = await container.ReplaceItemAsync(item, item.id, new PartitionKey("Configuration"));
                            if (updateResponse.StatusCode == System.Net.HttpStatusCode.OK ||
                                updateResponse.StatusCode == System.Net.HttpStatusCode.Created)
                            {
                                // Update successful
                                return true;
                            }
                            else
                            {
                                // Handle update failure
                                return
                                    false;
                            }
                        } 
                    }
                    return true;
                }
                else
                {
                    // Handle failure to save data.
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            // Handle any exceptions and return an error response
            throw ex;
        }
    }

    //[HttpPost("BuildAndPushImages")]
    //public IActionResult BuildAndPushImages(Configuration configuration)
    //{
    //    string deploymentFolderPath = Path.Combine(_hostingEnvironment.ContentRootPath, "Deployment");
    //    _logger.LogInformation(deploymentFolderPath);
    //    string imagesFolderPath = Path.Combine(deploymentFolderPath, "BIXBrokerApp");
    //    try
    //    {

    //        // Get the base directory where the application executable resides
    //         string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
    //        //string sourceDockerfilePath = Path.Combine(baseDirectory, "BIXBrokerApp_RabbitMQ");

    //        _logger.LogInformation(imagesFolderPath);
    //        // Get a list of files in the source directory.
    //        string[] files = Directory.GetFiles(imagesFolderPath);
    //        // Specify the subfolder within the "bin" directory
    //        string subfolderName = configuration.UserName;
    //        // Combine the base directory and subfolder name to create the complete folder path
    //        string folderPath = Path.Combine(baseDirectory, subfolderName);
    //        // Specify the source path of the Dockerfile in the "bin" directory
    //        // Create the subfolder
    //        Directory.CreateDirectory(folderPath);
    //        // Check if the folder was created
    //        if (Directory.Exists(folderPath))
    //        {
    //            _logger.LogInformation("Folder created successfully within the 'bin' directory.");
    //        }
    //        else
    //        {
    //            _logger.LogInformation("Folder creation failed.");
    //        }
    //        _logger.LogInformation(folderPath); 
    //        try
    //        {
    //            // Copy each file to the destination directory.
    //            foreach (string file in files)
    //            {
    //                string fileName = Path.GetFileName(file);
    //                string destinationFile = Path.Combine(folderPath, fileName);

    //                // Copy the file to the destination folder.
    //                System.IO.File.Copy(file, destinationFile, true); // Set the third argument to true to overwrite existing files.
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogInformation(ex.Message);
    //            throw ex;
    //        }
    //        // prepare xml files.
    //        SerializeDataToXml serializeDataToXml = new SerializeDataToXml();
    //        serializeDataToXml.Serialize(configuration, folderPath);

    //        string destinationDockerfilePath = Path.Combine(folderPath, "Dockerfile");
    //        _logger.LogInformation(destinationDockerfilePath);

    //        string dockerEXEFilePath = Path.Combine(folderPath, "docker.exe");
    //        _logger.LogInformation(dockerEXEFilePath);


    //        string scEXEFilePath = Path.Combine(folderPath, "sc.exe");
    //        _logger.LogInformation(scEXEFilePath);


    //        ProcessStartInfo startInfo1 = new ProcessStartInfo
    //        {
    //            FileName = scEXEFilePath, // Specify the 'sc.exe' executable
    //            RedirectStandardOutput = true,
    //            RedirectStandardError = true,
    //            UseShellExecute = false,
    //            CreateNoWindow = true,
    //            Arguments = "start docker"
    //        };

    //        try
    //        {
    //            using (Process process = new Process())
    //            {
    //                process.StartInfo = startInfo1;
    //                process.OutputDataReceived += (sender, e) => _logger.LogInformation(e.Data);
    //                process.ErrorDataReceived += (sender, e) => _logger.LogError(e.Data);

    //                process.Start();
    //                process.BeginOutputReadLine();
    //                process.BeginErrorReadLine();
    //                process.WaitForExit();
    //            }
    //        }
    //        catch (Exception ex)
    //        {
    //            _logger.LogError($"Error starting Docker daemon: {ex.Message}");
    //        }


    //        DockerImageBuilder.PushImageToACR("baggageplatform", configuration.UserName, 
    //            "latest", destinationDockerfilePath, _logger, dockerEXEFilePath);

    //        //BuildImageData(destinationDockerfilePath, configuration.Username + "broker", "latestfile");
    //        //BuildImage(destinationDockerfilePath, configuration.UserName + "broker", "latestfile");
    //        //RunContainer(destinationDockerfilePath, configuration.UserName + "broker", "latestfile");
    //        _logger.LogInformation("Docker images built and pushed successfully." + destinationDockerfilePath);

    //        return Ok("Docker images built and pushed successfully.");
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogInformation(ex.Message);
    //        return BadRequest(ex.Message);
    //    }

    //}

    [NonAction]
    public IActionResult BuildImageData(string dockerfilePath, string imageName, string imageTag)
    {
        using (PowerShell ps = PowerShell.Create())
        {
            ps.AddScript("BuildAndRunDocker.ps1"); // Replace with the actual path to your PowerShell script.

            ps.AddParameter("dockerfilePath", dockerfilePath);
            ps.AddParameter("imageName", imageName);
            ps.AddParameter("imageTag", imageTag);

            Collection<PSObject> output = ps.Invoke();

            if (ps.HadErrors)
            {
                foreach (ErrorRecord error in ps.Streams.Error)
                {
                    _logger.LogInformation("PowerShell Error: " + error.Exception.Message);
                }
            }
            else
            {
                foreach (var result in output)
                {
                    _logger.LogInformation(result.ToString());
                }
            }
            return Ok(output);
        }
    }
    [NonAction]
    public IActionResult BuildImage(string dockerfilePath, string imageName, string imageTag)
    {
        try
        {
            string buildCommand = "docker";
            string buildArguments = $"build -t {imageName}:{imageTag} -f {dockerfilePath} .";
            //string buildArguments = "build -t my-docker-image:latest .";  // Replace with your Dockerfile path and image name/tag

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = buildCommand,
                    Arguments = buildArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Redirect standard error
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            //process.Start();
            //string output = process.StandardOutput.ReadToEnd();
            //process.WaitForExit();
            //return Ok(output);
            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd(); // Capture standard error
            process.WaitForExit();
            if (process.ExitCode != 0)
            {
                // Log the error output
                _logger.LogInformation(errorOutput);
                return StatusCode(500, "Docker build failed. Check the logs for details.");
            }
            _logger.LogInformation(output);
            return Ok(output);

        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
            return BadRequest(ex.Message);
        }
    }



    [NonAction]
    public IActionResult RunContainer(string dockerfilePath, string imageName, string imageTag)
    {
        try
        {
            string runCommand = "docker";
            //string runArguments = "run -d my-docker-image:latest";  // Replace with your image name/tag
            string buildArguments = $"run -d {imageName}:{imageTag} -f {dockerfilePath} .";
            _logger.LogInformation(buildArguments);

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = runCommand,
                    Arguments = buildArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true, // Redirect standard error
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string errorOutput = process.StandardError.ReadToEnd(); // Capture standard error
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                // Log the error output
                _logger.LogInformation(errorOutput);
                return StatusCode(500, "Docker build failed. Check the logs for details.");
            }
            _logger.LogInformation(output);
            return Ok(output);
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message);
            return BadRequest(ex.Message);
        }
    }
    //[NonAction]
    //private void LogMessage(string messsage)
    //{
    //    string logFilePath = "Log.txt";
    //    using (StreamWriter writer = new StreamWriter(logFilePath, true))
    //    {
    //        writer.WriteLine($"[{DateTime.Now}] - Exception: {messsage}");
    //    }
    //}
}
