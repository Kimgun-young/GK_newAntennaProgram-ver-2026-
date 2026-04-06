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

    public class ImuData
    {
        public double imuAngleX { get; set; }
        public double imuAngleY { get; set; }
        public double imuAngleZ { get; set; }
        public double imuPitch { get; set; }
        public double imuRoll { get; set; }
        public string imuState { get; set; }
        public double imuTemperature { get; set; }
        public double imuYaw { get; set; }
    }

    public class RxArrayPanelData
    {
        public double directionGeographicPhi { get; set; }
        public double directionGeographicPolarityAngle { get; set; }
        public double directionGeographicTheta { get; set; }
        public double directionPhi { get; set; }
        public double directionPolarityAngle { get; set; }
        public string directionPolarityType { get; set; }
        public double directionRFFreq { get; set; }
        public double directionTheta { get; set; }
        public bool on { get; set; }
        public List<double> panelElectricity { get; set; }
        public List<double> panelTemperature { get; set; }
        public List<double> panelVoltage { get; set; }
    }

    public class GnssData
    {
        public double gpsAltitude { get; set; }
        public double gpsLatitude { get; set; }
        public double gpsLongitude { get; set; }
        public double gpsSpeed { get; set; }
        public long gpsTime { get; set; }
        public int state { get; set; }
    }

    public class TxArrayPanelData
    {
        public double directionGeographicPhi { get; set; }
        public double directionGeographicPolarityAngle { get; set; }
        public double directionGeographicTheta { get; set; }
        public double directionPhi { get; set; }
        public double directionPolarityAngle { get; set; }
        public string directionPolarityType { get; set; }
        public double directionRFFreq { get; set; }
        public double directionTheta { get; set; }
        public bool on { get; set; }
        public List<double> panelElectricity { get; set; }
        public List<double> panelTemperature { get; set; }
        public List<double> panelVoltage { get; set; }
    }

    public class FrequencyConverterData
    {
        public double fcElectricity { get; set; }
        public double fcIFAmplitude { get; set; }
        public double fcLockState { get; set; }
        public double fcRFAmplitude { get; set; }
        public double fcRxGain { get; set; }
        public double fcRxOsc { get; set; }
        public int fcState { get; set; }
        public double fcTemperature { get; set; }
        public double fcTxGain { get; set; }
        public double fcTxOsc { get; set; }
        public double fcVoltage { get; set; }
    }
}
