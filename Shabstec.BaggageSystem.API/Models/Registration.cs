using System.ComponentModel.DataAnnotations;

namespace BlazorApp.API.Models
{
    public class Registration
    {
        public string id { get; set; } // 'Id' property is required

        [Required(ErrorMessage = "First Name is required")]
        public string FirstName { get; set; }

        [Required(ErrorMessage = "Last Name is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Phone Number is required")]
        [Phone(ErrorMessage = "Invalid phone number")]
        public string PhoneNumber { get; set; }

        [Required(ErrorMessage = "Office Phone Number is required")]
        [Phone(ErrorMessage = "Invalid office phone number")]
        public string OfficePhoneNumber { get; set; }

        [Required(ErrorMessage = "Job Title is required")]
        public string JobTitle { get; set; }

        [Required(ErrorMessage = "Company Type is required")]
        public string CompanyType { get; set; }

        [Required(ErrorMessage = "Airport is required")]
        public string Airport { get; set; }

        [Required(ErrorMessage = "Strategic Partner is required")]
        public string StrategicPartner { get; set; }

        [Required(ErrorMessage = "Company Name is required")]
        public string CompanyName { get; set; }

        [Required(ErrorMessage = "Country/Territory is required")]
        public string CountryTerritory { get; set; }

        //[Required(ErrorMessage = "Consent Checkbox is required")]
        //[Range(typeof(bool), "true", "true", ErrorMessage = "You must consent to the processing of your personal information.")]
        //public bool ConsentCheckbox { get; set; }

        // Additional fields for baggage system service request
        public string ServiceRequestDetails { get; set; }

        public string AdditionalComments { get; set; }

        // Status field to track the approval status
        public ApprovalStatus ApprovalStatus { get; set; }

        // Credentials to be generated upon approval 
        public string Consent = "I consent to the processing of my personal information in accordance with IATA's Privacy Policy.";

        public string ServiceID = "Registration";
    }

    public enum ApprovalStatus
    {
        Pending,
        Approved,
        Rejected
    }
}