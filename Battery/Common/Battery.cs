using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class Battery
    {
        double frequency;
        double r_ohm; // resistance of battery in ohms, real part of impedance
        double x_ohm; // reactance of battery in ohms, imaginary part of impedance
        double voltage;
        double temperature_celsius;
        double range_ohm;
    }

}
