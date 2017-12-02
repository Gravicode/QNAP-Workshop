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

using GHI.Processor;
using Microsoft.SPOT.Hardware;
using GHI.Glide;
using System.Text;
using GHI.Glide.Geom;
using Json.NETMF;
using GHI.Glide.UI;
using GHI.SQLite;
using Microsoft.SPOT.Net.NetworkInformation;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Net;


namespace LoraReceiver
{

    public partial class Program
    {
        static int Counter = 0;
        static int MsgCounter = 0;
        //lora init
        private static SimpleSerial _loraSerial;
        private static string[] _dataInLora;
        //lora reset pin
        static OutputPort _restPort = new OutputPort(GHI.Pins.FEZSpiderII.Socket11.Pin3, true);
        private static string rx;
        //database
        static MqttClient client;

        const string MQTT_BROKER_ADDRESS = "192.168.7.2";//"13.76.142.227";
        Database myDatabase = null;
        // This method is run when the mainboard is powered up or reset.   



        private static void NetworkChange_NetworkAddressChanged(object sender, Microsoft.SPOT.EventArgs e)
        {
            Debug.Print("Network address changed");
        }

        private static void NetworkChange_NetworkAvailabilityChanged(object sender, NetworkAvailabilityEventArgs e)
        {
            Debug.Print("Network availability: " + e.IsAvailable.ToString());
        }
        void ProgramStarted()
        {

            NetworkChange.NetworkAvailabilityChanged += NetworkChange_NetworkAvailabilityChanged;
            NetworkChange.NetworkAddressChanged += NetworkChange_NetworkAddressChanged;
            try
            {
                var netif = wifiRS21.NetworkInterface;
                netif.Open();
                netif.EnableDhcp();
                netif.EnableDynamicDns();
                netif.Join("QNAP-Meja7", "qiotmeja7");

                //netif.Join("Redmi", "123qweasd");
                //netif.Join("Kamvret", "123qweasd");
                //netif.Join("WIFI_KELUARGA", "123qweasd");

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
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS), 21883, false, null, null, MqttSslProtocols.None);

            // register to message received
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = "loragateway_1512181778";//"lora_receiver_device";//Guid.NewGuid().ToString();
            client.Connect(clientId, "535d205e-ec57-459d-9961-ffc2639866a9", "r:d8ef429894396eabbfb06beaf5d71d86");

            // subscribe to the topic "/home/temperature" with QoS 2
            client.Subscribe(new string[] { "qiot/things/admin/loragateway/temp", "qiot/things/admin/loragateway/humid", "qiot/things/admin/loragateway/light", "qiot/things/admin/loragateway/relay" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            /*
            client = new MqttClient(IPAddress.Parse(MQTT_BROKER_ADDRESS));

            // register to message received
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;

            string clientId = "lora_receiver_device";//Guid.NewGuid().ToString();
            client.Connect(clientId,"mifmasterz","123qweasd");

            // subscribe to the topic "/home/temperature" with QoS 2
            client.Subscribe(new string[] { "mifmasterz/qnap/data" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            */
            //The network is now ready to use.

            // Create a database in memory,
            // file system is possible however!
            myDatabase = new GHI.SQLite.Database();
            myDatabase.ExecuteNonQuery("CREATE Table Sensor" +
            " (Time TEXT, Temp DOUBLE,Humid DOUBLE,Light DOUBLE)");
            //reset database n display
            /*
            BtnReset.TapEvent += (object sender) =>
            {
                Counter = 0;
                myDatabase.ExecuteNonQuery("DELETE FROM Sensor");
                GvData.Clear();
                GvData.Invalidate();
            };*/

            //reset lora
            _restPort.Write(false);
            Thread.Sleep(1000);
            _restPort.Write(true);
            Thread.Sleep(1000);


            _loraSerial = new SimpleSerial(GHI.Pins.FEZSpiderII.Socket11.SerialPortName, 57600);
            _loraSerial.Open();
            _loraSerial.DataReceived += _loraSerial_DataReceived;

            //_loraSerial.WriteLine("sys factoryRESET");
            //Thread.Sleep(1000);
            //get version
            _loraSerial.WriteLine("sys get ver");
            Thread.Sleep(1000);
            //pause join
            _loraSerial.WriteLine("mac pause");
            Thread.Sleep(1500);
            //antena power
            _loraSerial.WriteLine("radio set pwr 14");
            Thread.Sleep(1500);
            //set device to receive
            _loraSerial.WriteLine("radio rx 0"); //set module to RX
            PrintToLCD("setup is completed");

            //myDatabase.Dispose();

        }
        /*
        void submitData(string strValue)
        {

            // publish a message on "/home/temperature" topic with QoS 2
           client.Publish("mifmasterz/qnap/data", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

        }*/

        static void submitData(int category, string strValue)
        {
            // publish a message on "/home/temperature" topic with QoS 2
            switch (category)
            {
                case 1:
                    client.Publish("qiot/things/admin/loragateway/temp", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

                    break;
                case 2:
                    client.Publish("qiot/things/admin/loragateway/humid", Encoding.UTF8.GetBytes(strValue), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);

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
            // handle message received
        }
        //convert hex to string
        string HexStringToString(string hexString)
        {
            if (hexString == null || (hexString.Length & 1) == 1)
            {
                throw new ArgumentException();
            }
            var sb = new StringBuilder();
            for (var i = 0; i < hexString.Length; i += 2)
            {
                var hexChar = hexString.Substring(i, 2);
                sb.Append((char)Convert.ToByte(hexChar));
            }
            return sb.ToString();
        }
        //convert hex to ascii
        private string HexString2Ascii(string hexString)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i <= hexString.Length - 2; i += 2)
            {
                int x = Int32.Parse(hexString.Substring(i, 2));
                sb.Append(new string(new char[] { (char)x }));
            }
            return sb.ToString();
        }
        //lora data received
        void _loraSerial_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            _dataInLora = _loraSerial.Deserialize();
            for (int index = 0; index < _dataInLora.Length; index++)
            {
                rx = _dataInLora[index];
                //if error
                if (_dataInLora[index].Length > 7)
                {
                    if (rx.Substring(0, 9) == "radio_err")
                    {
                        Debug.Print("!!!!!!!!!!!!! Radio Error !!!!!!!!!!!!!!");
                        PrintToLCD("Radio Error");

                        _restPort.Write(false);
                        Thread.Sleep(1000);
                        _restPort.Write(true);
                        Thread.Sleep(1000);
                        _loraSerial.WriteLine("mac pause");
                        Thread.Sleep(1000);
                        _loraSerial.WriteLine("radio rx 0");
                        return;

                    }
                    //if receive data
                    if (rx.Substring(0, 8) == "radio_rx")
                    {
                        string hex = _dataInLora[index].Substring(10);

                        Mainboard.SetDebugLED(true);
                        Thread.Sleep(500);
                        Mainboard.SetDebugLED(false);

                        Debug.Print(hex);
                        Debug.Print(Unpack(hex));
                        //update display

                        insertToDb(Unpack(hex));
                        Thread.Sleep(100);
                        // set module to RX
                        _loraSerial.WriteLine("radio rx 0");
                    }
                }
            }

        }
        //extract hex to string
        public static string Unpack(string input)
        {
            byte[] b = new byte[input.Length / 2];

            for (int i = 0; i < input.Length; i += 2)
            {
                b[i / 2] = (byte)((FromHex(input[i]) << 4) | FromHex(input[i + 1]));
            }
            return new string(Encoding.UTF8.GetChars(b));
        }
        public static int FromHex(char digit)
        {
            if ('0' <= digit && digit <= '9')
            {
                return (int)(digit - '0');
            }

            if ('a' <= digit && digit <= 'f')
                return (int)(digit - 'a' + 10);

            if ('A' <= digit && digit <= 'F')
                return (int)(digit - 'A' + 10);

            throw new ArgumentException("digit");
        }
        void PrintToLCD(string message)
        {
            Debug.Print(message);
            characterDisplay.Clear();
            characterDisplay.Print(message);
            characterDisplay.BacklightEnabled = true;
        }


        void insertToDb(string message)
        {
            //String[] origin_names = null;
            //ArrayList tabledata = null;
            //cek message
            if (message != null && message.Length > 0)
            {
                try
                {

                    if (message == "Radio Error") return;
                    var detail = Json.NETMF.JsonSerializer.DeserializeString(message) as Hashtable;
                    //var detail = obj["Data"] as Hashtable;
                    DataSensor data = new DataSensor() { temp = Convert.ToDouble(detail["temp"].ToString()), humid = Convert.ToDouble(detail["humid"].ToString()), light = Convert.ToDouble(detail["light"].ToString()) };
                    //update display
                    submitData(1, "{\"value\":" + data.temp + "}");
                    submitData(2, "{\"value\":" + data.humid + "}");
                    submitData(3, "{\"value\":" + data.light + "}");
                    //submitData(message);
                    MsgCounter++;
                    PrintToLCD("DATA REC:"+ MsgCounter);
                    var TimeStr = DateTime.Now.ToString("dd/MM/yy HH:mm");
                    //insert to db

                    Counter++;


                    //add rows to table
                    myDatabase.ExecuteNonQuery("INSERT INTO Sensor (Time, Temp,Humid,Light)" +
                    " VALUES ('" + TimeStr + "' , " + data.temp + ", " + data.humid + ", " + data.light + ")");
                    if (Counter > 5)
                    {
                        //reset
                        Counter = 0;
                        myDatabase.ExecuteNonQuery("DELETE FROM Sensor");

                    }
                    /*
                    // Process SQL query and save returned records in SQLiteDataTable
                    ResultSet result = myDatabase.ExecuteQuery("SELECT * FROM Sensor");
                    // Get a copy of columns orign names example
                    origin_names = result.ColumnNames;
                    // Get a copy of table data example
                    tabledata = result.Data;
                    String fields = "Fields: ";
                    for (int i = 0; i < result.ColumnCount; i++)
                    {
                        fields += result.ColumnNames[i] + " |";
                    }
                    Debug.Print(fields);
                    object obj;
                    String row = "";
                    for (int j = 0; j < result.RowCount; j++)
                    {
                        row = j.ToString() + " ";
                        for (int i = 0; i < result.ColumnCount; i++)
                        {
                            obj = result[j, i];
                            if (obj == null)
                                row += "N/A";
                            else
                                row += obj.ToString();
                            row += " |";
                        }
                        Debug.Print(row);
                    }
                    */

                }
                catch (Exception ex)
                {
                    PrintToLCD(ex.Message);
                }
            }
        }





    }

    public static class ByteExt
    {
        private static char[] _hexCharacterTable = new char[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

#if MF_FRAMEWORK_VERSION_V4_1
    public static string ToHexString(byte[] array, string delimiter = "-")
#else
        public static string ToHexString(this byte[] array, string delimiter = "-")
#endif
        {
            if (array.Length > 0)
            {
                // it's faster to concatenate inside a char array than to
                // use string concatenation
                char[] delimeterArray = delimiter.ToCharArray();
                char[] chars = new char[array.Length * 2 + delimeterArray.Length * (array.Length - 1)];

                int j = 0;
                for (int i = 0; i < array.Length; i++)
                {
                    chars[j++] = (char)_hexCharacterTable[(array[i] & 0xF0) >> 4];
                    chars[j++] = (char)_hexCharacterTable[array[i] & 0x0F];

                    if (i != array.Length - 1)
                    {
                        foreach (char c in delimeterArray)
                        {
                            chars[j++] = c;
                        }

                    }
                }

                return new string(chars);
            }
            else
            {
                return string.Empty;
            }
        }
    }


    #region Model Classes

    public class DataSensor
    {
        public double temp { set; get; }
        public double humid { set; get; }
        public double light { set; get; }


    }
    #endregion
}
