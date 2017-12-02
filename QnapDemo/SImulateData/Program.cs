using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace SImulateData
{
    class Program
    {
        static MqttClient client;

        static string MQTT_BROKER_ADDRESS = ConfigurationManager.AppSettings["host"]; //"192.168.7.2";

        static void Main(string[] args)
        {
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS), 21883, false,null,null,MqttSslProtocols.None);
      

            // register to message received
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = ConfigurationManager.AppSettings["clientid"];//"raspithree_1512201069";//"lora_receiver_device";//Guid.NewGuid().ToString();
            client.Connect(clientId, ConfigurationManager.AppSettings["username"], ConfigurationManager.AppSettings["password"]);

            // subscribe to the topic "/home/temperature" with QoS 2
            //client.Subscribe(new string[] { "qiot/things/admin/loragateway/temp", "qiot/things/admin/loragateway/humid", "qiot/things/admin/loragateway/light", "qiot/things/admin/loragateway/relay" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

            client.Subscribe(new string[] { ConfigurationManager.AppSettings["topic1"],ConfigurationManager.AppSettings["topic2"]}, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            /*
                        string clientId = "loragateway_1512181778";//"lora_receiver_device";//Guid.NewGuid().ToString();
                        client.Connect(clientId, "535d205e-ec57-459d-9961-ffc2639866a9", "r:d8ef429894396eabbfb06beaf5d71d86");

                        // subscribe to the topic "/home/temperature" with QoS 2
                        client.Subscribe(new string[] { "qiot/things/admin/loragateway/temp", "qiot/things/admin/loragateway/humid", "qiot/things/admin/loragateway/light", "qiot/things/admin/loragateway/relay" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
                        */
            Random rnd = new Random();
            while (true)
            {
                submitData(1, "{\"value\":" + rnd.Next(1, 100).ToString() + "}");
                submitData(2, "{\"value\":" + rnd.Next(1, 100).ToString() + "}");
                //submitData(3, "{\"value\":" + rnd.Next(1, 1000).ToString() + "}");

                Thread.Sleep(2000);
            }
        }

        static void submitData(int category,string strValue)
        {
            // publish a message on "/home/temperature" topic with QoS 2
            switch (category)
            {
                case 1:
                    //client.Publish("qiot/things/admin/loragateway/temp", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    client.Publish(ConfigurationManager.AppSettings["topic1"], Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
          
                    break;
                case 2:
                    //client.Publish("qiot/things/admin/loragateway/humid", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    client.Publish(ConfigurationManager.AppSettings["topic2"], Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                    break;
                case 3:
                    client.Publish("qiot/things/admin/loragateway/light", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
                    break;
                default:
                    break;

            }


        }

        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Console.WriteLine(e.Topic + ":" + System.Text.Encoding.Default.GetString(e.Message));
            // handle message received
        }
    }

    class datasensor
    {
        public double value { get; set; }
    }
}
