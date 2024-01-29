namespace BlazorApp.API.Models
{
    public class MessageModel
    {
        public string EventData { get; set; }
        public string Topic { get; set; }
        public string Subscription { get; set; }
        // Add other properties as needed for your message
    }

    public class PODConfiguration
    {
        public string id { get; set; } // 'id' property is required
        public string Organisation { get; set; }
        public string CompanyCode { get; set; }
        public string UserName { get; set; }
        public string PublisherSvcBusURL { get; set; }
        public string SubscriberSvcBusURL { get; set; }
        public string PublisherSvcBusQueue { get; set; }
        public string SubscriberSvcBusQueue { get; set; }
        

        #region Publisher Configuration 
        public string entity { get; set; }
        public string PublishTo { get; set; }
        public string SubscribeTo { get; set; }
        
        #endregion

        #region Message broker Configuration
        public string RabbitMQUsername { get; set; }
        public string RabbitMQPassword { get; set; }
        #endregion 

        public string RabbitMQHost { get; set; }

        public int RabbitMQPort{ get; set; }

        public string brokersvc { get; set; }
        public string clientsvc { get; set; }
        
    }

}