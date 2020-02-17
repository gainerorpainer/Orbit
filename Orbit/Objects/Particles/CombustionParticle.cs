using Orbit.Physics;
using OrbitLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Objects.Particles
{
    class CombustionParticle : WorldObject, IParticle
    {
        public Particle Particle { get; set; }

        public CombustionParticle(Particle particle, double mass, Vector location, Vector vel)
        {
            Particle = particle;

            base.Body = new Physics.Body()
            {
                Mass = mass,
                Location = location,
                Velocity = vel,
                HasCollision = false
            };

            base.Drawables = new List<Drawing.Drawable>
            {
                new Drawing.ZoomableCircle()
                {
                    Brush = System.Drawing.Brushes.Orange,
                    IsZoomable = true,
                    Radius = 1,
                    Visible = true
                }
            };
        }
    }
}
