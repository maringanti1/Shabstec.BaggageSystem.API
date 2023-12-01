namespace BlazorApp.API.Models
{
    public class User
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Username { get; set; }
        public string Token { get; set; }
        public bool IsDeleting { get; set; }
        public string ServiceID { get; set; } // 'id' property is required
    }
}