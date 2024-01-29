namespace BlazorApp.API.Models
{ 
    public class CosmosDbSettings
    {
        public string Endpoint { get; set; }
        public string Key { get; set; }
        public string DatabaseName { get; set; }
        public string ContainerName { get; set; }
        public string PublisherSvcBusConnString { get; set; }
        public string SubscriberSvcBusConnString { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string TenantId { get; set; }
        public string SubscriptionId { get; set; } 
    } 
}