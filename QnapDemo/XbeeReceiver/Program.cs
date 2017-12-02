using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.GHIElectronics;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Net;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Text;

namespace XbeeReceiver
{
    public partial class Program
    {
        static MqttClient client;

        const string MQTT_BROKER_ADDRESS = "13.76.142.227";

        static int Counter = 0;
        bool IsXbee = true;


        private static void NetworkChange_NetworkAddressChanged(object sender, Microsoft.SPOT.EventArgs e)
        {
            Debug.Print("Network address changed");
        }

        private static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Debug.Print("Network availability: " + e.IsAvailable.ToString());
        }
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
              
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");
            XbeeInit();

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            try
            {
                var netif = wifiRS21.NetworkInterface;
                netif.Open();
                netif.EnableDhcp();
                netif.EnableDynamicDns();
                //netif.Join("Redmi", "123qweasd");
                //netif.Join("Kamvret", "123qweasd");
                netif.Join("WIFI_KELUARGA", "123qweasd");

                while (netif.IPAddress == "0.0.0.0")
                {
                    Debug.Print("Waiting for DHCP");
                    PrintToLCD("Waiting for DHCP");
                    Thread.Sleep(250);
                }
                Debug.Print("IP:" + netif.IPAddress.ToString());
                PrintToLCD("IP:" + netif.IPAddress.ToString());
            }
            catch (Exception ex) { PrintToLCD("Error join wifi"); return; }
            // create client instance
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

            // register to message received
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = "xbee_receiver_device";//Guid.NewGuid().ToString();
            client.Connect(clientId, "mifmasterz", "123qweasd");

            // subscribe to the topic "/home/temperature" with QoS 2
            client.Subscribe(new string[] { "mifmasterz/qnap/data" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });

        }
        void PrintToLCD(string message)
        {
            Debug.Print(message);

        }
        static void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            // handle message received
        }
        void XbeeInit()
        {

            xBeeAdapter.Configure(9600, GT.SocketInterfaces.SerialParity.None, GT.SocketInterfaces.SerialStopBits.One, 8, GT.SocketInterfaces.HardwareFlowControl.NotRequired);
            Debug.Print("port:" + xBeeAdapter.Port.PortName);
            xBeeAdapter.Port.Open();
            xBeeAdapter.Port.LineReceived += Port_LineReceived;

        }

        void Port_LineReceived(GT.SocketInterfaces.Serial sender, string line)
        {
            Debug.Print(DateTime.Now + " : " + line);
            try
            {
                var detail = Json.NETMF.JsonSerializer.DeserializeString(line) as Hashtable;
                //var detail = obj["Data"] as Hashtable;
                SensorData data = new SensorData() { temp = Convert.ToDouble(detail["temp"].ToString()), humid = Convert.ToDouble(detail["humid"].ToString()), light = Convert.ToDouble(detail["light"].ToString()) };
            }
            catch (Exception ex)
            {
                PrintToLCD(ex.Message);
            }
            submitData(line);

        }

        void submitData(string strValue)
        {

            // publish a message on "/home/temperature" topic with QoS 2
            client.Publish("mifmasterz/qnap/data", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

        }
    }


    public class SensorData
    {
        public double temp { get; set; }
        public double humid { get; set; }
        public double light { get; set; }
    }
}

