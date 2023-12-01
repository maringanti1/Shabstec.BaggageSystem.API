using System.Collections.Generic;
using System.Xml.Serialization;

namespace BlazorApp.API.Models
{
    public class Publisher
    {
        public ConfigData ConfigData { get; set; }
        public TopicCodeData TopicCodeData { get; set; }
        public SubscriberTopics SubscriberTopics { get; set; }
    }

    public class ConfigData
    {
        public string BrokerSvcBusQueueName { get; set; }
        public string BrokerSvcBusURL { get; set; }
        public string RabbitMQHostSecretName { get; set; }
        public int RabbitMQPortSecretName { get; set; }
        public string RabbitMQUsernameSecretName { get; set; }
        public string RabbitMQPasswordSecretName { get; set; }
        public string RabbitMQExchange { get; set; }
        public string SubscribedQueueName { get; set; }
        public string SubscribedQueueEndPoint { get; set; }
    }

    public class TopicCodeData
    {
        public Topics Topics { get; set; }
    }
    public class SubscriberTopics
    {
        public SubTopics SubTopics { get; set; }
    }

    public class Topics
    {
        [XmlElement("Topic")]
        public List<Topic> TopicList { get; set; }
    }

    public class SubTopics
    {
        [XmlElement("Topic")]
        public List<Topic> SubTopicList { get; set; }
    }

    public class Topic
    {
        public string TopicName { get; set; }
        public string TopicHost { get; set; }
        public string TopicVersion { get; set; }
    }
}
