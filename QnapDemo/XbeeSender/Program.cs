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

using System.IO.Ports;
using System.Text;
using Microsoft.SPOT.Hardware;

namespace XbeeSender
{
    public partial class Program
    {
        static int Counter = 0;
        bool IsXbee = true;
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
            GT.Timer timer = new GT.Timer(3000); // every second (1000ms)
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void XbeeInit()
        {

            xBeeAdapter.Configure(9600, GT.SocketInterfaces.SerialParity.None, GT.SocketInterfaces.SerialStopBits.One, 8, GT.SocketInterfaces.HardwareFlowControl.NotRequired);
            Debug.Print("port:" + xBeeAdapter.Port.PortName);
            xBeeAdapter.Port.Open();
            xBeeAdapter.Port.LineReceived += Port_LineReceived;

        }

        void timer_Tick(GT.Timer timer)
        {
            Random rnd = new Random();
            var node = new SensorData() { humid = rnd.Next(100), temp = rnd.Next(100), light = rnd.Next(1000) };
            var json = Json.NETMF.JsonSerializer.SerializeObject(node);
            if (IsXbee)
            {
                xBeeAdapter.Port.WriteLine(json);
                Debug.Print("TRANSMIT DATA XBEE " + (Counter++));
            }
        }

        void Port_LineReceived(GT.SocketInterfaces.Serial sender, string line)
        {
            Debug.Print(DateTime.Now + " : " + line);
        }
    }


    public class SensorData
    {
        public double temp { get; set; }
        public double humid { get; set; }
        public double light { get; set; }
    }
}
