using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using BlazorApp.API.Models;
using System.Configuration;
using System.Security.Policy;
using Newtonsoft.Json.Linq;
using System;

namespace BlazorApp.API.Services
{
    //public class SerializeDataToXml
    //{

    //    public string Serialize(ClientConfiguration configuration, string folderPath)
    //    {
    //        // Split the comma-separated values in configuration.AirLineCodes
    //        if (string.IsNullOrEmpty(configuration.SelectedAirLineCode))
    //        {
    //            Console.WriteLine("Airlines code not found for the User");
    //            return "";
    //        }
    //        string[] airlineCodes = configuration.SelectedAirLineCode.Split(',');
    //        // Create a list to hold the topics
    //        List<Topic> topics = new List<Topic>();
    //        // Iterate through the airline codes and create Topic objects
    //        foreach (string airlineCode in airlineCodes)
    //        {
    //            Topic topic = new Topic
    //            {
    //                TopicName = airlineCode,
    //                TopicHost = airlineCode, // You can set this to the same value as TopicName if needed
    //                TopicVersion = airlineCode // You can set this to the same value as TopicName if needed
    //            };

    //            topics.Add(topic);
    //        }
    //        string[] subscriberCodes = configuration.SubscribedQueueTopic.Split(',');
    //        List<Topic> subscribertopics = new List<Topic>();
    //        // Iterate through the airline codes and create Topic objects
    //        foreach (string airlineCode in subscriberCodes)
    //        {
    //            Topic topic = new Topic
    //            {
    //                TopicName = airlineCode,
    //                TopicHost = airlineCode, // You can set this to the same value as TopicName if needed
    //                TopicVersion = airlineCode // You can set this to the same value as TopicName if needed
    //            };

    //            subscribertopics.Add(topic);
    //        }

    //        string rabbitMQHost = $"{configuration.UserName.ToLower()}.uksouth.azurecontainer.io";
    //        // Create a Publisher instance and set its ConfigData and TopicCodeData properties
    //        Models.Publisher publisher = new Models.Publisher
    //        {
    //            //Testdata.uksouth.azurecontainer.io
    //            ConfigData = new ConfigData
    //            {
    //                // Map the properties from the Configuration object
    //                BrokerSvcBusQueueName = configuration.BrokerSvcBusQueueName,
    //                BrokerSvcBusURL = configuration.BrokerSvcBusURL,
    //                RabbitMQHostSecretName = rabbitMQHost,
    //                RabbitMQPortSecretName = 5672,
    //                RabbitMQUsernameSecretName = configuration.RabbitMQUsername,
    //                RabbitMQPasswordSecretName = configuration.RabbitMQPassword,
    //                RabbitMQExchange = configuration.UserName.ToLower(),
    //                SubscribedQueueEndPoint = configuration.SubscribedQueueEndPoint,
    //                SubscribedQueueName = configuration.SubscribedQueueName,
    //            },
    //            TopicCodeData = new TopicCodeData
    //            {
    //                Topics = new Topics
    //                { 
    //                    TopicList= topics
    //                }
    //            },
    //            SubscriberTopics = new SubscriberTopics
    //            {
    //                SubTopics = new SubTopics
    //                {
    //                    SubTopicList = subscribertopics
    //                }
    //            }
    //        }; 
    //        // Set values for ConfigData and TopicCodeData

    //        // Serialize to XML
    //        SerializeToXml(publisher, folderPath + "/" + "PublisherConfiguration.xml");
    //        return folderPath + "/" + "PublisherConfiguration.xml";
    //    }
    //    public void SerializeToXml<T>(T data, string outputFileName)
    //    {
    //        var serializer = new XmlSerializer(typeof(T));
    //        using (var writer = new StreamWriter(outputFileName))
    //        {
    //            serializer.Serialize(writer, data);
    //        }
    //    }
    //}
}
