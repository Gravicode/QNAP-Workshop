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

using Microsoft.SPOT.Hardware;
using System.Text;
using GHI.Processor;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using Gadgeteer.Modules.GHIElectronics;

namespace LoraSender
{

    public partial class Program
    {
        static int Counter = 0;
        //LORA setting..
        private static SimpleSerial _loraSerial;
        private static string[] _dataInLora;
        static OutputPort _restPort = new OutputPort(GHI.Pins.FEZRaptor.Socket1.Pin3, true);
      

        Font font = Resources.GetFont(Resources.FontResources.NinaB);
        Bitmap lcd = new Bitmap(SystemMetrics.ScreenWidth, SystemMetrics.ScreenHeight);
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {

            Display.Populate(Display.GHIDisplay.DisplayT35);

            if (Display.Save())
                PowerState.RebootDevice(false);

         

            //LORA init
            _loraSerial = new SimpleSerial(GHI.Pins.FEZRaptor.Socket1.SerialPortName, 57600);
            _loraSerial.Open();
            _loraSerial.DataReceived += _loraSerial_DataReceived;
            //reset lora
            _restPort.Write(false);
            Thread.Sleep(1000);
            _restPort.Write(true);
            Thread.Sleep(1000);
            //setup lora for point to point
            //get lora version
           
            
            _loraSerial.WriteLine("sys get ver");
            Thread.Sleep(1000);
            //pause join
            _loraSerial.WriteLine("mac pause");
            Thread.Sleep(1000);
            //set antena power
            _loraSerial.WriteLine("radio set pwr 14");
            Thread.Sleep(1000);

            new Thread(SendData).Start();

        }

        void SendData()
        {
            //loop forever
            for (; ; )
            {
                
                //get data from oximeter
                //var xx = new DataSensor(){ humid=10, light=5, temp=11};
                var RefreshedSensor = new DataSensor() {  humid = tempHumidSI70.TakeMeasurement().RelativeHumidity, light = lightSense.GetIlluminance(), temp = tempHumidSI70.TakeMeasurement().Temperature };
                string data = Json.NETMF.JsonSerializer.SerializeObject(RefreshedSensor);
                byte[] b = Encoding.UTF8.GetBytes(data);
                string hex = "radio tx " + ToHexString(b, 0, b.Length); // TX payload needs to be HEX
                //send data via lora
                _loraSerial.WriteLine(hex);
                //update display
                
                PrintToLog("DATA TRANSMIT:" + (Counter++));
                //delay 5 sec
                Thread.Sleep(5000);
            }
        }

        void PrintToLog(string message)
        {
            Debug.Print(message);
            lcd.Clear();
            lcd.DrawText(message, font, Colors.White, 0, 0);
            lcd.Flush();
        }

        void _loraSerial_DataReceived(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            //just for debug
            _dataInLora = _loraSerial.Deserialize();
            for (int index = 0; index < _dataInLora.Length; index++)
            {
                //Debug.Print(_dataInLora[index]);
                PrintToLog(_dataInLora[index]);
            }
        }
        //convert byte to hex
        public static string ToHexString(byte[] value, int index, int length)
        {
            char[] c = new char[length * 3];
            byte b;

            for (int y = 0, x = 0; y < length; ++y, ++x)
            {
                b = (byte)(value[index + y] >> 4);
                c[x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
                b = (byte)(value[index + y] & 0xF);
                c[++x] = (char)(b > 9 ? b + 0x37 : b + 0x30);
            }
            return new string(c, 0, c.Length - 1);
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