namespace BlazorApp.API.Models
{
    public class ClientConfiguration
    {
        public string UserName { get; set; }
        public string CompanyCode { get; set; }
        public string Organization { get; set; }
        public string id { get; set; } // 'id' property is required
        public string entity { get; set; }
        public int IsDeployed { get; set; }
        public string PublishTo { get; set; }
        public string SubscribeTo { get; set; }
        
        
    }

}