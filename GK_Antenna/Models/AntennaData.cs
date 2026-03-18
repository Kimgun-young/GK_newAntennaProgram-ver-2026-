using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GK_Antenna.Models
{
    public class AntennaData    
    {
        public int antennaState { get; set; }
        public string antennaTrackingState { get; set; }
        public string antennaTxRfState { get; set; }
        public string antennaRxRfState { get; set; }
        public double antennaTemperature { get; set; }
        public double antennaVoltage { get; set; }
        public double antennaElectricity { get; set; }
        
    }

    public class MultiModeReceiverData
    {
        public double mmrBeaconValue { get; set; }
        public double mmrCnrPower { get; set; }
        public double mmrDetectionValue { get; set; }
        public double mmrDvbValue { get; set; }
        public double mmrFreq { get; set; }
        public double mmrRang { get; set; }
        public int mmrState { get; set; }
        public double mmrSym { get; set; }
        public bool mmrUTSwitch { get; set; }
        public string mmrWorkMode { get; set; }
    }
}
