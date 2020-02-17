using OrbitLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Physics
{
    class Body
    {
        public Vector Force { get; set; } = new Vector();
        public Vector Velocity { get; set; } = new Vector();
        public Vector Location { get; set; } = new Vector();
        public double Torque { get; set; }
        public double RotationSpeed { get; set; }
        public double Orientation { get; set; }
        public double Mass { get; set; }
        public double AngularMomentum { get; set; }
        public bool IsRigid { get; set; }
        public double CollisionRadius { get; set; }
        public bool HasCollision { get; set; } = true;
        public bool CalcTrajectory { get; set; }

        public double HitRadius(Body other)
        {
            if (!HasCollision || !other.HasCollision)
                return double.NegativeInfinity;

            return (other.CollisionRadius + CollisionRadius) - (other.Location - Location).Length;
        }
    }

    interface IBody
    {
        Body Body { get; set; }
    }
}
