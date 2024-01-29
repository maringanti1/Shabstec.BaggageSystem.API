using System.ComponentModel.DataAnnotations;

namespace BlazorApp.API.Models
{
    public class AddUser
    {
        public string id { get; set; } // 'id' property is required

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string Username { get; set; }
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "The Password field must be a minimum of 6 characters")]
        public string Password { get; set; }

        [Required]
        [MinLength(6, ErrorMessage = "The Organisation field must be a minimum of 6 characters")]
        public string Organisation { get; set; }

        public string entity { get; set; } // 'id' property is required
    }
}