using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Controllers
{
    abstract class Controller
    {
        public bool IsActive { get; set; } = false;
        public abstract double CalcOutput(double timestep, double err);

        public double Next(double timestep, double set, double actual)
        {
            return CalcOutput(timestep, set - actual);
        }
    }

    interface IController
    {
        Controller Controller { get; set; }
    }
}
