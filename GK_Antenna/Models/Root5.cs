using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GK_Antenna.Models
{
    public class Root5
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
    }
}
