using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Battery
    {
        public double frequency { get; set; }
        public double r_ohm{ get; set; }
        public double x_ohm{ get; set; } 
        public double voltage{ get; set; }
        public double temperature_celsius{ get; set; }
        public double range_ohm{ get; set; }
    }

}
