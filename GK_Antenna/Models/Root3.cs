using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GK_Antenna.Models
{
    public class Root3
    {
        public int manual_gps { get; set; }
        public double manual_height { get; set; }
        public double manual_latitude { get; set; }
        public double manual_longitude { get; set; }
        public string openamipHost { get; set; }
        public string openamipMask { get; set; }
        public int openamipPort { get; set; }
        public double rollFactor { get; set; }
        public string serverHost { get; set; }
        public string serverMask { get; set; }
        public int serverPort { get; set; }
        public int trackMode { get; set; }
        public int workMode { get; set; }
        public string appVersion { get; set; }
        public DeviceParamRange deviceParamRange { get; set; }
        public string firmwareDate { get; set; }
        public string firmwareType { get; set; }
        public string firmwareVersion { get; set; }
        public string model { get; set; }
        public string serial { get; set; }
    }


    public class DeviceParamRange
    {
        public double phiMax { get; set; }
        public double phiMin { get; set; }
        public double pitchMax { get; set; }
        public double pitchMin { get; set; }
        public double rxAngleMax { get; set; }
        public double rxAngleMin { get; set; }
        public List<string> rxDirectionPolarityType { get; set; }
        public double rxFreqMax { get; set; }
        public double rxFreqMin { get; set; }
        public List<double> rxOscList { get; set; }
        public double rxOscMax { get; set; }
        public double rxOscMin { get; set; }
        public bool showCurrent { get; set; }
        public bool showRxPolAngle { get; set; }
        public bool showTemperature { get; set; }
        public bool showTxPolAngle { get; set; }
        public bool showVoltage { get; set; }
        public List<double> switchOscRxFreqList { get; set; }
        public List<double> switchOscTxFreqList { get; set; }
        public double symMaxDVB { get; set; }
        public double symMaxDetection { get; set; }
        public double symMin { get; set; }
        public double txAngleMax { get; set; }
        public double txAngleMin { get; set; }
        public List<string> txDirectionPolarityType { get; set; }
        public double txFreqMax { get; set; }
        public double txFreqMin { get; set; }
        public List<double> txOscList { get; set; }
    }
}