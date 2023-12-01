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

    [HttpPost("Authenticate")]
    public async Task<IActionResult> Authenticate([FromBody] Login model)
    {
        _logger.LogInformation("Authenticate started");
        var user = await cosmosDBService.ValidateUserAsync(model);

        if (user)
        {
                        
            return Ok(user);
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
