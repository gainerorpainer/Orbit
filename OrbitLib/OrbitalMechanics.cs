using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OrbitLib
{
    public class Apsis
    {
        public double Far { get; set; }
        public double Near { get; set; }
    }

    public static class OrbitalMechanics
    {
        public static Apsis CalcApsis(double mass1, double mass2, Vector velocity, Vector radius, double gravityconst)
        {
            double stdGravitParam = gravityconst * (mass1 + mass2);

            // Optimized: double orbitalEnergy = Math.Pow(velocity.Length, 2) / 2 - stdGravitParam / radius.Length;
            double orbitalEnergy = Math.Pow(velocity.Length / Math.Sqrt(2), 2) - stdGravitParam / radius.Length;
            double angularMomentum = Vector.CrossProduct(radius, velocity);

            double a = -stdGravitParam / (2 * orbitalEnergy);
            double e = Math.Sqrt(1 - Math.Pow(angularMomentum, 2) / (stdGravitParam * a));

            return new Apsis()
            {
                Near = a * (1 - e),
                Far = a * (1 + e)
            };
        }
    }
}
