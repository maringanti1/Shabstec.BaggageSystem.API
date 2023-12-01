using Azure.Containers.ContainerRegistry;
using Azure.Identity;
using BlazorApp.API.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

public static class DockerImageBuilder
{
    
    public static void PushImageToACR(string acrName, string imageName, string imageTag, 
        string dockerfilePath, ILogger<DeploymentController> _logger, string dockerEXEFilePath)
    {

        try
        {
            string acrUsername = "baggageplatform";
            string acrPassword = "Mpq+hy2ko01gLyg/oFtYBCKiXOsCDP63";

            string acrLoginServer = $"{acrName}.azurecr.io";
            _logger.LogInformation(acrLoginServer);
            _logger.LogInformation(acrUsername);
            _logger.LogInformation(imageName);
            _logger.LogInformation(imageTag);

            _logger.LogInformation("StartDocker started");

            StartDocker(dockerEXEFilePath, _logger);
            _logger.LogInformation("StartDocker ended");

            _logger.LogInformation("DockerLogin started");
            DockerLogin(acrLoginServer, acrUsername, acrPassword, imageName.ToLower(),
                imageTag.ToLower(), dockerfilePath, _logger, dockerEXEFilePath);
            _logger.LogInformation("DockerBuild started");
            DockerBuild(acrLoginServer, acrUsername, acrPassword, imageName.ToLower(),
                imageTag.ToLower(), dockerfilePath, _logger, dockerEXEFilePath);
            _logger.LogInformation("DockerPush started");
            DockerPush(acrLoginServer, acrUsername, acrPassword,
                imageName.ToLower(), imageTag.ToLower(), dockerfilePath, _logger, dockerEXEFilePath);
            _logger.LogInformation("DockerPush started");
            _logger.LogInformation($"Docker image '{acrLoginServer}/{imageName}:{imageTag}' pushed to ACR '{acrName}' successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogInformation($"Error: {ex.Message}");
        }
    }

    private static void StartDocker(string dockerEXEFilePath, ILogger<DeploymentController> _logger)
    {
        try
        {

            //string startDockerDaemonCommand = "sc start docker";
            //_logger.LogInformation(startDockerDaemonCommand);

           


            // Define the command to start the Docker daemon
            string startDockerDaemonCommand = "dockerd";
            _logger.LogInformation(startDockerDaemonCommand);
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = dockerEXEFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "daemon"
            };
            _logger.LogInformation(startInfo.Arguments);
            using (Process process = new Process { StartInfo = startInfo })
            {
                process.Start();
                process.WaitForExit();

                // Read the output and error, if needed
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                // Handle the output and error as needed
                _logger.LogInformation("Docker Daemon Start Output:");
                _logger.LogInformation(output);

                if (!string.IsNullOrEmpty(error))
                {
                    _logger.LogInformation("Docker Daemon Start Error:");
                    _logger.LogInformation(error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message.ToString());
            _logger.LogInformation(ex.StackTrace);
        }
    }

    private static void DockerPush(string acrLoginServer, string acrUsername, string acrPassword,
        string imageName, string imageTag, string dockerfilePath,
        ILogger<DeploymentController> _logger, string dockerEXEFilePath)
    {
        try 
        { 
        ProcessStartInfo pushInfo = new ProcessStartInfo
        {
            FileName = dockerEXEFilePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            Arguments = $"push baggageplatform.azurecr.io/{imageName}:{imageTag}"
        };
        _logger.LogInformation(pushInfo.Arguments);
        using (Process pushProcess = new Process { StartInfo = pushInfo })
        {
            pushProcess.Start();
            string output = pushProcess.StandardOutput.ReadToEnd();
            string errorOutput = pushProcess.StandardError.ReadToEnd();
            _logger.LogInformation(output);
            _logger.LogInformation(errorOutput);
            pushProcess.WaitForExit();

            if (pushProcess.ExitCode != 0)
            {
                // Handle the error
                _logger.LogInformation($"Docker push failed: {errorOutput}");
                return;
            }
        }
    }
         catch (Exception ex)
        {
            _logger.LogInformation(ex.Message.ToString());
            _logger.LogInformation(ex.StackTrace);
        }

        _logger.LogInformation("Docker image pushed successfully.");

    }

    private static void DockerBuild(string acrLoginServer, string acrUsername, string acrPassword,
        string imageName, string imageTag, string dockerfilePath,
        ILogger<DeploymentController> _logger, string dockerEXEFilePath)
    {
        try
        {
            ProcessStartInfo buildInfo = new ProcessStartInfo
            {
                FileName= dockerEXEFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = $"build -t baggageplatform.azurecr.io/{imageName}:{imageTag} -f {dockerfilePath} ."
            };
            _logger.LogInformation(buildInfo.Arguments);
            using (Process buildProcess = new Process { StartInfo = buildInfo })
            {
                buildProcess.Start();
                string output = buildProcess.StandardOutput.ReadToEnd();
                string errorOutput = buildProcess.StandardError.ReadToEnd();
                _logger.LogInformation(output);
                _logger.LogInformation(errorOutput);
                buildProcess.WaitForExit();

                if (buildProcess.ExitCode != 0)
                {
                    // Handle the error
                    _logger.LogInformation($"Docker build failed: {errorOutput}");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation(ex.Message.ToString());
            _logger.LogInformation(ex.StackTrace);
        }

    }

    private static void DockerLogin(string acrLoginServer, string acrUsername, string acrPassword,
        string imageName, string imageTag, string dockerfilePath,
        ILogger<DeploymentController> _logger, string dockerEXEFilePath)
    {
        try
        {

            ProcessStartInfo loginInfo = new ProcessStartInfo
            {
                FileName = dockerEXEFilePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                Arguments = "login baggageplatform.azurecr.io -u baggageplatform -p Mpq+hy2ko01gLyg/oFtYBCKiXOsCDP63"
            };
            _logger.LogInformation(loginInfo.Arguments);
            using (Process loginProcess = new Process { StartInfo = loginInfo })
            {
                loginProcess.Start();
                loginProcess.WaitForExit();

                if (loginProcess.ExitCode != 0)
                {
                    // Handle the error
                    string errorOutput = loginProcess.StandardError.ReadToEnd();
                    _logger.LogInformation($"Docker login failed: {errorOutput}");
                    return;
                }
            }
        }
        catch(Exception ex )
        {
            _logger.LogInformation(ex.Message.ToString());
            _logger.LogInformation(ex.StackTrace);
        }

    }
}
//     
//}
