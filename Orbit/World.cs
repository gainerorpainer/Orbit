using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using Orbit.Physics;
using Orbit.Objects;
using OrbitLib;

namespace Orbit
{
    class World
    {
        /// <summary>
        /// m
        /// </summary>
        public const double EARTHRADIUS = 6.371e6;
        /// <summary>
        /// N/m^2/Kg^2
        /// </summary>
        public const double GRAVITYCONST = 6.67430e-7;//6.67430e-11;
        /// <summary>
        /// Kg
        /// </summary>
        public const double EARTHMASS = 5.972e23;// 5.972e24;
        /// <summary>
        /// Nm
        /// </summary>
        public const double TORQUE_ROCKET = 1000;
        /// <summary>
        /// N
        /// </summary>
        public const double THRUST_ROCKET = 3000000;
        /// <summary>
        /// N
        /// </summary>
        public const double THRUST_BOOSTER = 18000000;
        readonly Random Rng = new Random();

        // Earth
        public Planet Earth { get; set; } = new Planet()
        {
            ColorSheme = Color.Blue,
            Body = new Body()
            {
                Location = new Vector(0, 0),
                Mass = EARTHMASS,
                IsRigid = true,
                CollisionRadius = EARTHRADIUS
            },
            Drawables = new List<Drawing.Drawable>()
            {
                new Drawing.ZoomableCircle()
                {
                    Radius = (float)EARTHRADIUS,
                    Brush = Brushes.Blue,
                    IsZoomable = false
                }
            }
        };

        // Rocket
        public Rocket Rocket { get; set; } = new Rocket()
        {
            ColorSheme = Color.Red,
            Body = new Body()
            {
                Location = new Vector(0, EARTHRADIUS),
                Velocity = new Vector(0, 0),
                //Location = new Vector(0, EARTHRADIUS + 100e3),
                //Velocity = new Vector(-Math.Sqrt(GRAVITYCONST * (EARTHMASS / (EARTHRADIUS + 100e3))), 0),
                Orientation = 1.0 / 2.0 * Math.PI,
                Mass = 1000,
                AngularMomentum = 1000,
                CollisionRadius = 10,
                CalcTrajectory = true
            },
            Drawables = new List<Drawing.Drawable>()
            {
                new Drawing.ZoomableCircle()
                {
                    Radius = 10,
                    Brush = Brushes.Red
                },
                new Drawing.Arrow()
                {
                    Size = 5
                }
            }
        };

        // Space Station
        public Station Station { get; set; } = new Station()
        {
            ColorSheme = Color.Green,
            Body = new Body()
            {
                Location = new Vector(0, -(EARTHRADIUS + 10000e3)),
                Velocity = new Vector(Math.Sqrt(GRAVITYCONST * (EARTHMASS / (EARTHRADIUS + 10000e3))), 0),
                Mass = 10000,
                CollisionRadius = 10,
                CalcTrajectory = true
            },
            Drawables = new List<Drawing.Drawable>()
            {
                new Drawing.ZoomableCircle()
                {
                    Radius = 10,
                    Brush = Brushes.Green
                }
            }
        };


        // List of all objects
        List<WorldObject> Objects { get; set; } = new List<WorldObject>();

        // List of all collision Events
        DoubleKeyDictionary<int, Action<World, double>> CollisionEvents { get; set; }
        public double UserZoom { get; set; } = 1;
        public double GlobalTime { get; set; } = 0;

        public World()
        {
            Objects.Add(Earth);
            Objects.Add(Rocket);
            Objects.Add(Station);

            CollisionEvents = new DoubleKeyDictionary<int, Action<World, double>>()
            {
                {Rocket.Id, Station.Id, Orbit.CollisionEvents.On_Collision_Rocket_Spacestation },
                {Rocket.Id, Earth.Id, Orbit.CollisionEvents.On_Collision_Rocket_Earth }
            };
        }

        // One Step
        public void PhysicsNext(double turn, double thrust, double timestep)
        {
            if (double.IsInfinity(timestep) || (timestep == 0))
                return;

            GlobalTime += timestep;

            // Check lifetime for each particle
            Objects.RemoveAll(x => (x as Objects.Particles.IParticle)?.Particle.IsDead(GlobalTime) ?? false);

            foreach (var body in Objects.Select(x => x.Body))
                ResetForcesAndTorque(body);

            // Apply forces & Torque due to user steer
            UserInput(turn, thrust);

            // Add environmental forces
            foreach (var obj in Objects)
            {
                var body = obj.Body;

                // Calc gravity
                if (!body.IsRigid)
                    AddGravity(body);
            }

            // Apply changes due to physics
            foreach (var obj in Objects)
                CalcPhysics(timestep, obj.Body);

            // Check Collision (each with each)
            List<WorldObject> collisionObjects = Objects.Where(x => x.Body.HasCollision).ToList();
            for (int i = 0; i < collisionObjects.Count; i++)
                for (int j = i + 1; j < collisionObjects.Count; j++)
                    CheckCollision(collisionObjects[i], collisionObjects[j]);

            // Add Particles
            if (thrust > 0)
            {
                AddCombustionParticles(thrust);
            }
        }

        private void AddCombustionParticles(double thrust)
        {
            // Start at the rocket
            // Calc opposite direction
            int NUMPARTICLES = Rocket.BoostersEnabled ? 5 : 1;
            for (int i = 0; i < NUMPARTICLES; i++)
            {
                // Thrust is the probability with that a particle is spawned
                if (Rng.NextDouble() >= thrust)
                    continue;

                // Randomize physical properties
                double rngModifier = 0.95 + Rng.NextDouble() / 10.0;
                const double VELOCITYCOMBUSTIONPARTICLE = 100000;
                var velParticle = Rocket.Body.Velocity + Toolbox.Rotate(new Vector(VELOCITYCOMBUSTIONPARTICLE * rngModifier, 0), Rocket.Body.Orientation + Math.PI * rngModifier);

                Objects.Add(new Objects.Particles.CombustionParticle(new Orbit.Objects.Particles.Particle()
                {
                    Creationtime = GlobalTime,
                    MaxLifetime = Rng.NextDouble() * 3
                }, 1, Rocket.Body.Location, velParticle));
            }
        }

        private void UserInput(double turn, double thrust)
        {
            // Clamp commands
            turn = Math.Max(Math.Min(turn, 1), -1);
            thrust = Math.Max(Math.Min(thrust, 1), -1);

            Rocket.Body.Torque = TORQUE_ROCKET * turn;
            Rocket.Body.Force = Toolbox.Rotate(new Vector(THRUST_ROCKET * thrust, 0), Rocket.Body.Orientation);
            if (Rocket.BoostersEnabled)
                Rocket.Body.Force += Toolbox.Rotate(new Vector(THRUST_BOOSTER * thrust, 0), Rocket.Body.Orientation);
        }

        private void ResetForcesAndTorque(Body body)
        {
            body.Force = Vector.NULL;
            body.Torque = 0;
        }

        private void CheckCollision(WorldObject obj1, WorldObject obj2)
        {
            // Check if event is attached
            double hitRadius = obj1.Body.HitRadius(obj2.Body);
            if (hitRadius > 0)
            {
                // Physical collision
                // Assumption: soft contact
                // Make a weighted average
                var vSum = (obj1.Body.Mass * obj1.Body.Velocity + obj2.Body.Mass * obj2.Body.Velocity) / (obj1.Body.Mass + obj2.Body.Mass);

                if (CollisionEvents.TryGetValue(obj1.Id, obj2.Id, out Action<World, double> f))
                {
                    // Calc how hard the hit is in terms of energy
                    var e1 = Math.Pow((vSum - obj1.Body.Velocity).Length, 2) * 0.5 * obj1.Body.Mass;
                    var e2 = Math.Pow((vSum - obj2.Body.Velocity).Length, 2) * 0.5 * obj2.Body.Mass;
                    var eSum = e1 + e2;

                    // Call event
                    f(this, eSum);
                }

                // Apply speeds & forces
                obj1.Body.Velocity = obj2.Body.Velocity = vSum;

                // Push away
                double dir = (obj2.Body.Location - obj1.Body.Location).Angle;
                var pushLength = hitRadius * new Vector(Math.Cos(dir), Math.Sin(dir));
                if (!obj1.Body.IsRigid)
                    obj1.Body.Location += -pushLength;
                if (!obj2.Body.IsRigid)
                    obj2.Body.Location += pushLength;
            }
        }

        private static void CalcPhysics(double timestep, Body body)
        {
            // Calc acc
            Vector acc = new Vector(body.Force.X, body.Force.Y) / body.Mass;

            // Integrating acc will yield dV
            Vector dV = acc * timestep;
            // (use middle point approx)
            body.Velocity += dV;

            // Integrating dV will yield dS 
            Vector dS = body.Velocity * timestep;
            body.Location += dS;

            // Calc angular acc
            double angAcc = body.Torque / body.AngularMomentum;

            // Integrate gives speed
            double dAngV = angAcc * timestep;
            body.RotationSpeed += dAngV;

            // Integrate gives angle
            double dAngle = body.RotationSpeed * timestep;
            body.Orientation += dAngle;

            // At this point, orientation can overflow, so limit its magnitude
            if (body.Orientation > (2 * Math.PI))
                body.Orientation -= 2 * Math.PI;
            else if (body.Orientation <= -(2 * Math.PI))
                body.Orientation += 2 * Math.PI;
        }

        private void AddGravity(Body body)
        {
            // First, find direction to earth
            Vector vec = Earth.Body.Location - body.Location;

            // Then, calc magnitude
            double force = GRAVITYCONST * ((body.Mass * Earth.Body.Mass) / Math.Pow(vec.Length, 2));

            // Then apply
            body.Force += force * new Vector(Math.Cos(vec.Angle), Math.Sin(vec.Angle));
        }

        public void Draw(Graphics g, Size screen)
        {
            // Zoom such that all objects are in view
            float boundRadius = (float)Math.Max(Rocket.Body.Location.Length, Station.Body.Location.Length);
            var worldBounds = new RectangleF(-boundRadius, -boundRadius, boundRadius * 2, boundRadius * 2);
            float zoom = 0.9f * Math.Min(screen.Width / worldBounds.Width, screen.Height / worldBounds.Height);

            // Apply user zoom
            zoom *= (float)UserZoom;

            g.TranslateTransform(screen.Width / 2, screen.Height / 2);
            // Also, flip y axis
            g.ScaleTransform(zoom, -zoom);
            g.TranslateTransform(-(float)Rocket.Body.Location.X, -(float)Rocket.Body.Location.Y);

            foreach (var obj in Objects)
            {
                foreach (var drawable in obj.Drawables)
                {
                    drawable.Draw(g, zoom, obj.Body.Location.ToPointF(), (float)obj.Body.Orientation);
                }
            }

            g.ResetTransform();
        }

        public void RecalcTrajectories(double timestep)
        {

            foreach (var obj in Objects.Where(x => x.Body.CalcTrajectory == true))
            {
                // Remove all trajectories
                obj.Drawables.RemoveAll(x => x is Drawing.Spline);

                Body body = obj.Body;


                // Calc Physics iterations
                const int ITERATIONS = 5000;

                // Make a value copy
                Body clone = new Body()
                {
                    Force = body.Force,
                    Location = body.Location,
                    Mass = body.Mass,
                    Velocity = body.Velocity
                };

                List<Vector> points = new List<Vector>() { clone.Location };
                for (int i = 0; i < ITERATIONS; i++)
                {
                    ResetForcesAndTorque(clone);
                    AddGravity(clone);
                    CalcPhysics(timestep * 8, clone);

                    // Check bounds
                    if (clone.Location.Length > (EARTHRADIUS * 100))
                        break;

                    // Store location
                    points.Add(clone.Location);
                }

                obj.Drawables.Add(new Drawing.Spline()
                {
                    Points = points,
                    Pen = new Pen(Toolbox.TransformColor(obj.ColorSheme, 0.3))
                });
            }
        }
    }
}
