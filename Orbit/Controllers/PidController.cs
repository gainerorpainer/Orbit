using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Controllers
{
    class PidController : Controller
    {
        public double P { get; set; }
        public double I { get; set; }
        public double D { get; set; }

        public double? Clamp { get; set; }

        private double previousError = 0;
        private double integral = 0;

        public PidController(double p, double i, double d)
        {
            P = p;
            I = i;
            D = d;
        }

        public void Reset()
        {
            previousError = integral = 0;
        }

        public override double CalcOutput(double timestep, double err)
        {
            integral += err * timestep;

            // Anti windup
            if (Clamp.HasValue)
            {
                if (Math.Abs(integral) > Clamp.Value)
                {
                    integral = Math.Sign(integral) * Clamp.Value;
                }
            }

            double derrivate = (err - previousError) / timestep;
            previousError = err;
            return (P * err) + (I * integral) + (D * derrivate);


        }
    }
}
