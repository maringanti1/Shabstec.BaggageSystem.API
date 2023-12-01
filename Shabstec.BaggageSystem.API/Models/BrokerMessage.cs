using System.ComponentModel.DataAnnotations;

namespace BlazorApp.API.Models
{
    public class BrokerMessage
    {
        public string id { get; set; }
        public string Message { get; set; }
        public string ServiceID = "Registration";
    }
}