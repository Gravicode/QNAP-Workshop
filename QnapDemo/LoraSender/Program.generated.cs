//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace LoraSender {
    using Gadgeteer;
    using GTM = Gadgeteer.Modules;
    
    
    public partial class Program : Gadgeteer.Program {
        
        /// <summary>The GasSense module using socket 2 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.GasSense gasSense;
        
        /// <summary>The TempHumid SI70 module using socket 4 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.TempHumidSI70 tempHumidSI70;
        
        /// <summary>The LightSense module using socket 18 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.LightSense lightSense;
        
        /// <summary>The Display T35 module using sockets 15, 16, 17 and 14 of the mainboard.</summary>
        private Gadgeteer.Modules.GHIElectronics.DisplayT35 displayT35;
        
        /// <summary>This property provides access to the Mainboard API. This is normally not necessary for an end user program.</summary>
        protected new static GHIElectronics.Gadgeteer.FEZRaptor Mainboard {
            get {
                return ((GHIElectronics.Gadgeteer.FEZRaptor)(Gadgeteer.Program.Mainboard));
            }
            set {
                Gadgeteer.Program.Mainboard = value;
            }
        }
        
        /// <summary>This method runs automatically when the device is powered, and calls ProgramStarted.</summary>
        public static void Main() {
            // Important to initialize the Mainboard first
            Program.Mainboard = new GHIElectronics.Gadgeteer.FEZRaptor();
            Program p = new Program();
            p.InitializeModules();
            p.ProgramStarted();
            // Starts Dispatcher
            p.Run();
        }
        
        private void InitializeModules() {
            this.gasSense = new GTM.GHIElectronics.GasSense(2);
            this.tempHumidSI70 = new GTM.GHIElectronics.TempHumidSI70(4);
            this.lightSense = new GTM.GHIElectronics.LightSense(18);
            this.displayT35 = new GTM.GHIElectronics.DisplayT35(15, 16, 17, 14);
        }
    }
}
