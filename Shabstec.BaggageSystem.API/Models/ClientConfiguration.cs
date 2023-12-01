namespace BlazorApp.API.Models
{
    public class ClientConfiguration
    {
        public string UserName { get; set; }

        #region Publisher Configuration
        public string id { get; set; } // 'id' property is required
        public string ServiceID { get; set; }
        public string AirLineCodes { get; set; }
        public string BrokerSvcBusQueueName { get; set; }
        public string BrokerSvcBusURL { get; set; }
        #endregion

        #region Message broker Configuration
        public string RabbitMQUsername { get; set; }
        public string RabbitMQPassword { get; set; }
        #endregion

        #region Subscriber Configuration
        public string SubscribedQueueEndPoint { get; set; }
        public string SubscribedQueueName { get; set; }
        public string SubscribedQueueVersion { get; set; }
        public string SubscribedQueueTopic { get; set; }
        #endregion

        public string RabbitMQHost { get; set; }

        public string RabbitMQPort{ get; set; }
    }

}