using BlazorApp.API.Models;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Mail;
using System.Net;

public class EmailManager
{
    private readonly EmailConfiguration _emailConfiguration; 

    public EmailManager(EmailConfiguration _emailCon)
    { 
        _emailConfiguration = _emailCon;
    }

    public void SendEmail(Registration model)
    {
        string emailBody = LoadTemplate(model);
        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(_emailConfiguration.Email);
        //mail.To.Add("laxman.marin@shabstec.com"); // Replace with the recipient's email address
        //mail.To.Add("shabbir.girach@shabstec.com");
        mail.To.Add("laxman.marin@shabstec.com");
        mail.Subject = "Request for Baggage Information Service";
        mail.IsBodyHtml = true; // Set the email body as HTML 
        mail.Body = emailBody;
        SmtpClient smtpClient = new SmtpClient(_emailConfiguration.SmtpServer);
        smtpClient.Port = _emailConfiguration.SmtpPort;
        smtpClient.Credentials = new NetworkCredential(_emailConfiguration.Email, _emailConfiguration.Password);
        smtpClient.EnableSsl = true; // GoDaddy's SMTP server requires SSL

        try
        {
            smtpClient.Send(mail);
            Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }

        Console.WriteLine("Email sent successfully.");
    }

    public void SendNewUserLoginCredentialsEmail(AddUser model)
    {
        string emailBody = LoadNewUserLoginTemplate(model);
        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(_emailConfiguration.Email);
        //mail.To.Add("laxman.marin@shabstec.com"); // Replace with the recipient's email address
        //mail.To.Add("shabbir.girach@shabstec.com");
        mail.To.Add(model.Email);
        mail.Subject = "Baggage Information Service - Login credentials";
        mail.IsBodyHtml = true; // Set the email body as HTML 
        mail.Body = emailBody;
        SmtpClient smtpClient = new SmtpClient(_emailConfiguration.SmtpServer);
        smtpClient.Port = _emailConfiguration.SmtpPort;
        smtpClient.Credentials = new NetworkCredential(_emailConfiguration.Email, _emailConfiguration.Password);
        smtpClient.EnableSsl = true; // GoDaddy's SMTP server requires SSL 
        try
        {
            smtpClient.Send(mail);
            Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }

        Console.WriteLine("Email sent successfully.");
    }

    public void SendUserEmail(Registration model)
    {
        string emailBody = LoadUserTemplate(model);
        MailMessage mail = new MailMessage();
        mail.From = new MailAddress(_emailConfiguration.Email);
        //mail.To.Add("laxman.marin@shabstec.com"); // Replace with the recipient's email address
        mail.To.Add(model.Email);
        //mail.To.Add("laxman.marin@shabstec.com");
        mail.Subject = "Thank you for your registration - Shabstec Baggage Information Services";
        mail.IsBodyHtml = true; // Set the email body as HTML 
        mail.Body = emailBody;
        SmtpClient smtpClient = new SmtpClient(_emailConfiguration.SmtpServer);
        smtpClient.Port = _emailConfiguration.SmtpPort;
        smtpClient.Credentials = new NetworkCredential(_emailConfiguration.Email, _emailConfiguration.Password);
        smtpClient.EnableSsl = true; // GoDaddy's SMTP server requires SSL

        try
        {
            smtpClient.Send(mail);
            Console.WriteLine("Email sent successfully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to send email: {ex.Message}");
        }

        Console.WriteLine("Email sent successfully.");
    }

    private string LoadTemplate(Registration model)
    {
        string templatePath = @"Template/RegistrationEmail.txt";
        string templateContent = File.ReadAllText(templatePath);
        // Replace placeholders with actual values
        string replacedContent = ReplacePlaceholders(templateContent, new
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            OfficePhoneNumber = model.OfficePhoneNumber,
            JobTitle = model.JobTitle,
            CompanyType = model.CompanyType, 
            CompanyName = model.CompanyName,
            Country = model.CountryTerritory,
            Consent = model.ConsentCheckbox 
        }); 

        return replacedContent;
    }

    private string LoadUserTemplate(Registration model)
    {
        string templatePath = @"Template/AckEmail.txt";
        string templateContent = File.ReadAllText(templatePath);
        // Replace placeholders with actual values
        string replacedContent = ReplacePlaceholders(templateContent, new
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Email = model.Email,
            PhoneNumber = model.PhoneNumber,
            OfficePhoneNumber = model.OfficePhoneNumber,
            JobTitle = model.JobTitle,
            CompanyType = model.CompanyType, 
            CompanyName = model.CompanyName,
            Country = model.CountryTerritory,
            Consent = model.ConsentCheckbox
        });

        return replacedContent;
    }

    private string LoadNewUserLoginTemplate(AddUser model)
    {
        string templatePath = @"Template/LoginEmail.txt";
        string templateContent = File.ReadAllText(templatePath);
        // Replace placeholders with actual values
        string replacedContent = ReplacePlaceholders(templateContent, new
        {
            FirstName = model.FirstName,
            LastName = model.LastName,
            Password = model.Password,
            Username = model.Username,
            Organisation = model.Organisation,
            Email = model.Email
        });
        return replacedContent;
    }

    static string ReplacePlaceholders(string content, object values)
    {
        // Use reflection to replace placeholders with actual values
        foreach (var property in values.GetType().GetProperties())
        {
            string placeholder = $"{{{{{property.Name}}}}}";
            object propValue = property.GetValue(values, null);
            content = content.Replace(placeholder, propValue?.ToString() ?? string.Empty);
        }

        return content;
    }

    //private void SendUserLoginDetails(AddUser model)
    //{
    //    string emailBody = LoadUserTemplate(model);
    //    MailMessage mail = new MailMessage();
    //    mail.From = new MailAddress(_emailConfiguration.Email);
    //    //mail.To.Add("laxman.marin@shabstec.com"); // Replace with the recipient's email address
    //    mail.To.Add(model.Email);
    //    //mail.To.Add("laxman.marin@shabstec.com");
    //    mail.Subject = "Thank you for your registration - Shabstec Baggage Information Services";
    //    mail.IsBodyHtml = true; // Set the email body as HTML 
    //    mail.Body = emailBody;
    //    SmtpClient smtpClient = new SmtpClient(_emailConfiguration.SmtpServer);
    //    smtpClient.Port = _emailConfiguration.SmtpPort;
    //    smtpClient.Credentials = new NetworkCredential(_emailConfiguration.Email, _emailConfiguration.Password);
    //    smtpClient.EnableSsl = true; // GoDaddy's SMTP server requires SSL

    //    try
    //    {
    //        smtpClient.Send(mail);
    //        Console.WriteLine("Email sent successfully.");
    //    }
    //    catch (Exception ex)
    //    {
    //        Console.WriteLine($"Failed to send email: {ex.Message}");
    //    }

    //    Console.WriteLine("Email sent successfully.");
    //}
    }
