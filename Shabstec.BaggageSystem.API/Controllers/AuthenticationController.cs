using BlazorApp.API.Models;
using BlazorApp.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using WebApplication1.Controllers;
using Microsoft.AspNetCore.Cors; 
using Microsoft.Extensions.Options;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using System;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;
using User = BlazorApp.API.Models.User;
using System.Text;
using Docker.DotNet;

[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private CosmosDBService cosmosDBService;

    public UserController(ILogger<UserController> logger, 
        IOptions<CosmosDbSettings> cosmosDbSettings,
        IOptions<EmailConfiguration> emailconfig)
    {
        _logger = logger;
        cosmosDBService = new CosmosDBService(_logger,cosmosDbSettings, emailconfig);
       
    }
    private readonly ILogger<UserController> _logger;

    [HttpPost("CreateUser")]
    public async Task<IActionResult> CreateUser([FromBody] AddUser user)
    {
        _logger.LogInformation("CreateUser started");
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await cosmosDBService.CreateUserAsync(user);

        return Ok("User created successfully");
    }

    [HttpPost("RegisterUser")]
    public async Task<IActionResult> RegisterUser([FromBody]Registration user)
    {
        _logger.LogInformation("RegisterUser started");
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        await cosmosDBService.RegisterUserAsync(user);

        return Ok("User request has created successfully");
    }

    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUser(string userId)
    {
        var user = await cosmosDBService.GetUserAsync(userId);

        if (user == null)
        {
            return NotFound(); // User not found
        }

        return Ok(user);
    }

    public static string DecodePassword(string encodedPassword, string key)
    {
        // Decode Base64 string to bytes
        byte[] bytesToDecode = Convert.FromBase64String(encodedPassword);

        // Convert bytes back to the combined string
        string combinedString = Encoding.UTF8.GetString(bytesToDecode);

        // Remove the key to get the original password
        string originalPassword = combinedString.Substring(0, combinedString.Length - key.Length);

        return originalPassword;
    }


    [HttpPost("validate")]
    public async Task<IActionResult> ValidateUser([FromBody] Login credentials)
    {
        _logger.LogInformation("ValidateUser started");       
        var isValid = await cosmosDBService.ValidateUserAsync(credentials);

        if (isValid)
        {
            return Ok("User is valid");
        }

        return BadRequest("Invalid user credentials");
    }
    public static string EncodePassword(string password, string key)
    {
        // Combine password and key
        string combinedString = password + key;

        // Convert the combined string to bytes
        byte[] bytesToEncode = Encoding.UTF8.GetBytes(combinedString);

        // Encode the bytes in Base64
        string encodedPassword = Convert.ToBase64String(bytesToEncode);

        return encodedPassword;
    }
    [HttpPost("Authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] Login model)
    {
         _logger.LogInformation("Authenticate started");
        string key = "randomKey123";
        // Encode and save the password to the database
        string encodedPassword = EncodePassword(model.Password, key);
        model.Password = encodedPassword;
        var user = await cosmosDBService.ValidateUserAsync(model); 
        if (user)
        {
            var userData = await cosmosDBService.GetUserDetails(model);
            
            return Ok(userData);
        }
        return BadRequest(new { message = "Invalid username or password" });
        
    }

    [HttpGet("GetAllUsers")]
    public async Task<IActionResult> GetAllUsers()
    {
        _logger.LogInformation("GetAllUsers started");


        try
        {
            List<User> users = await cosmosDBService.GetAllUsers();

            if (users == null)
            {
                return NotFound(); // User not found
            }

            return Ok(users);
        }
        catch (Exception ex)
        {
            // Handle any exceptions and return an error response
            return StatusCode(500, $"Internal server error: {ex.Message}");
        }
    }
}
