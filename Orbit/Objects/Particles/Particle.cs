using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Objects.Particles
{
    class Particle
    {
        public double Creationtime { get; set; }
        public double MaxLifetime { get; set; }
        public double Lifetime(double now) => now - Creationtime;
        public bool IsDead(double now) => now - Creationtime > MaxLifetime;
    }

    interface IParticle
    {
        Particle Particle { get; set; }
    }

}
