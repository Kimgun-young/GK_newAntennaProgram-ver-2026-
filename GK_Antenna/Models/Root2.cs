using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GK_Antenna.Models
{
    public class Root2
    {
        public AntennaData antennaData { get; set; }

        public MultiModeReceiverData multiModeReceiverData { get; set; }

        public ImuData imuData { get; set; }

        public RxArrayPanelData rxArrayPanelData { get; set; }
    }
}
