using System.ComponentModel.DataAnnotations;

namespace BlazorApp.API.Models
{
    public class BrokerMessage
    {
        public string id { get; set; }
        public string Message { get; set; }
        public string RecievedFrom { get; set; }
        public string TopicHost { get; set; }
        public string TopicName { get; set; }
        public string Host { get; set; }
        public string ClientID { get; set; }
        public string CreateDate { get; set; }
        public string DateTime { get; set; }

        public string entity = "Message";
    }
}