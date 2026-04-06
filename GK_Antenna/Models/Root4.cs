using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GK_Antenna.Models
{
    public class Root4
    {
        public int beacon_mode { get; set; }
        public int issue_mode { get; set; }
        public int rang { get; set; }
        public double rx_freq { get; set; }
        public int rx_gain { get; set; }
        public double rx_osc { get; set; }
        public double rx_phi { get; set; }
        public double rx_pol { get; set; }
        public string rx_polarity_type { get; set; }
        public double rx_theta { get; set; }
        public double sat_longitude { get; set; }
        public string sat_name { get; set; }
        public double sym { get; set; }
        public double tx_freq { get; set; }
        public int tx_gain { get; set; }
        public double tx_osc { get; set; }
        public double tx_phi { get; set; }
        public double tx_pol { get; set; }
        public string tx_polarity_type { get; set; }
        public double tx_theta { get; set; }
        public string workMode { get; set; }
    }
}
