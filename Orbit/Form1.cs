using OrbitLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace Orbit
{
    public partial class Form1 : Form
    {
        const int DESIREDFRAMERATE = 60;
        private const double TIMESTEP = 1.0 / DESIREDFRAMERATE;

        readonly World MyWorld = new World();

        readonly System.Diagnostics.Stopwatch lastFrame = System.Diagnostics.Stopwatch.StartNew();
        readonly System.Diagnostics.Stopwatch frameTimer = System.Diagnostics.Stopwatch.StartNew();

        int Framerate { get; set; }
        int FrameCounter { get; set; } = 0;
        double MinDistance { get; set; } = double.PositiveInfinity;
        double MaxDistance { get; set; } = 0;
        Apsis Apsis { get; set; } = new Apsis();

        double TurnCommand { get; set; }
        double ThrustCommand { get; set; }
        int TimeRate { get; set; } = 1;
        public bool AutoFly { get; private set; } = false;

        readonly List<Clickable> Clickables = new List<Clickable>();


        readonly Controllers.PidController FlightController = new Controllers.PidController(1, 0.25, 0.1)
        {
            Clamp = 1.0
        };
        readonly Controllers.PidController DockController = new Controllers.PidController(1, 0.25, 0.1);

        public bool Retrograde = false;

        public Form1()
        {
            InitializeComponent();

            // Add complex keyevents
            Keyevents.AddDebouncedEvent(Key.OemPlus, () => MyWorld.UserZoom *= 2);
            Keyevents.AddDebouncedEvent(Key.OemMinus, () => MyWorld.UserZoom /= 2);
            Keyevents.AddDebouncedEvent(Key.OemPlus, () => TimeRate *= 2, control: true);
            Keyevents.AddDebouncedEvent(Key.OemMinus, () => TimeRate = TimeRate > 1 ? TimeRate / 2 : 1, control: true);
            Keyevents.AddDebouncedEvent(Key.Left, () => TurnCommand = 1, () => TurnCommand = 0);
            Keyevents.AddDebouncedEvent(Key.Right, () => TurnCommand = -1, () => TurnCommand = 0);
            Keyevents.AddDebouncedEvent(Key.Up, () => ThrustCommand = 1, () => ThrustCommand = 0);
            Keyevents.AddToggleEvent(Key.B, () => MyWorld.Rocket.BoostersEnabled = true, () => MyWorld.Rocket.BoostersEnabled = false);

            // Add buttons
            Clickable progradeButton = new Clickable()
            {
                Label = "Prograde",
                Rectangle = new Rectangle(0, 0, 40, 40),
                OnUnClick = () =>
                {
                    FlightController.IsActive = false;
                    TurnCommand = 0;
                },
                Toggle = true
            };
            Clickable retrogradeButton = new Clickable()
            {
                Label = "Retrograde",
                Rectangle = new Rectangle(0, 40, 40, 40),
                OnUnClick = () =>
                {
                    FlightController.IsActive = false;
                    TurnCommand = 0;
                },
                Toggle = true
            };
            progradeButton.OnClick = () =>
            {
                retrogradeButton.UnClick();

                FlightController.IsActive = true;
                Retrograde = false;
                FlightController.Reset();
            };
            retrogradeButton.OnClick = () =>
            {
                progradeButton.UnClick();

                FlightController.IsActive = true;
                Retrograde = true;
                FlightController.Reset();
            };

            Clickables.Add(progradeButton);
            Clickables.Add(retrogradeButton);
            Clickables.Add(new Clickable()
            {
                Label = "AUTO",
                Rectangle = new Rectangle(0, 80, 40, 40),
                OnClick = () =>
                {
                    AutoFly = true;
                },
                OnUnClick = () =>
                {
                    AutoFly = false;
                    stage = 0;
                },
                Toggle = true
            });
        }

        private void PhysicsFrame()
        {
            // Calc apsis
            Apsis = OrbitalMechanics.CalcApsis(MyWorld.Rocket.Body.Mass, World.EARTHMASS,
                MyWorld.Rocket.Body.Velocity,
                MyWorld.Rocket.Body.Location,
                World.GRAVITYCONST);

            if (AutoFly)
            {
                Autopilot();
            }

            // Calc controllers (will override user inputs)
            if (FlightController.IsActive)
            {
                // First, check rotation speed
                const double ROTLIMIT = Math.PI;
                if (Math.Abs(MyWorld.Rocket.Body.RotationSpeed) > ROTLIMIT)
                {
                    // Simply slowdown
                    TurnCommand = -Math.Sign(MyWorld.Rocket.Body.RotationSpeed);
                    FlightController.Reset();
                }
                else
                {
                    // Calc guidance error where abs(err)<=180°
                    double guidanceError = Limit180Deg(MyWorld.Rocket.Body.Velocity.Angle - MyWorld.Rocket.Body.Orientation);

                    if (Retrograde)
                        if (guidanceError > 0)
                            guidanceError -= Math.PI;
                        else
                            guidanceError += Math.PI;

                    // Calc turn signal based on error
                    double desiredRotSpeed = guidanceError / 3.0;

                    // Never exceed abs > 1
                    TurnCommand = FlightController.Next(TIMESTEP, desiredRotSpeed, MyWorld.Rocket.Body.RotationSpeed);
                }
            }

            MyWorld.PhysicsNext(TurnCommand, ThrustCommand, TIMESTEP);

            // Make apsis user friedly
            Apsis.Near -= World.EARTHRADIUS;
            Apsis.Far -= World.EARTHRADIUS;
        }


        int stage = 0;
        string stageDescription = "Init";
        private void Autopilot()
        {
            // state
            switch (stage)
            {
                case 0:
                    // Go prograde
                    Clickables[0].Click();
                    stage = 1;
                    stageDescription = "Wait for prograde...";
                    TimeRate = 1;
                    break;

                case 1:
                    // Check prograde
                    if ((Math.Abs(Limit180Deg(MyWorld.Rocket.Body.Orientation - MyWorld.Rocket.Body.Velocity.Angle)) <= 0.2)
                        &&
                        (Math.Abs(MyWorld.Rocket.Body.RotationSpeed) <= 0.1))
                    {
                        stage = 2;
                        stageDescription = "Raising apoapsis";
                        TimeRate = 1;
                    }
                    break;

                case 2:
                    // Fire until apoapsis is good
                    ThrustCommand = 0.75;
                    if (Apsis.Far >= (MyWorld.Station.Body.Location.Length * 0.9))
                    {
                        stage = 3;
                        stageDescription = "Wait for apoapsis...";
                        TimeRate = 1;
                    }
                    break;

                case 3:
                    ThrustCommand = 0;
                    // Check height
                    if (MyWorld.Rocket.Body.Location.Length >= (Apsis.Far * 0.999))
                    {
                        stage = 4;
                        stageDescription = "Circularization burn";
                        TimeRate = 1;
                    }
                    break;

                case 4:
                    ThrustCommand = 1;
                    // Check near -> far
                    if (Apsis.Near >= (Apsis.Far * 0.95))
                    {
                        stage = 5;
                        stageDescription = "Wait for phase...";
                        TimeRate = 1;
                    }
                    break;

                case 5:
                    ThrustCommand = 0;
                    // Check phase
                    if (Math.Abs(MyWorld.Station.Body.Location.Angle - MyWorld.Rocket.Body.Location.Angle) <= 0.187417)
                    {
                        stage = 6;
                        stageDescription = "Raising apoapsis";
                        TimeRate = 1;
                    }
                    break;

                case 6:
                    // Fire until apoapsis is good
                    ThrustCommand = 0.75;
                    if (Apsis.Far >= (MyWorld.Station.Body.Location.Length))
                    {
                        stage = 7;
                        stageDescription = "Wait for apoapsis...";
                        TimeRate = 1;
                    }
                    break;

                case 7:
                    // Check height
                    ThrustCommand = 0;
                    if (MyWorld.Rocket.Body.Location.Length >= MyWorld.Station.Body.Location.Length)
                    {
                        stage = 8;
                        stageDescription = "Circularization burn";
                        TimeRate = 1;
                    }
                    break;

                case 8:
                    ThrustCommand = 0.5;
                    // Check circle
                    if ((Math.Abs(Apsis.Far - Apsis.Near) <= 30000) || (Math.Max(Apsis.Near, Apsis.Far) >= MyWorld.Station.Body.Location.Length))
                    {
                        stage = 9;
                        stageDescription = "Docking mode";
                        TimeRate = 1;
                    }
                    break;

                case 9:
                    ThrustCommand = 0;
                    Clickables[0].UnClick();

                    stage = 10;
                    TimeRate = 1;
                    break;

                case 10:
                    // Calc guidance error where abs(err)<=180°
                    double guidanceError = Limit180Deg((MyWorld.Station.Body.Location - MyWorld.Rocket.Body.Location).Angle - MyWorld.Rocket.Body.Orientation);

                    //// Give Thrust based on guidance error
                    //ThrustCommand = 0.25 - guidanceError;
                    //ThrustCommand = Math.Max(0, ThrustCommand);

                    // Calc turn signal based on error
                    double desiredRotSpeed = guidanceError / 3.0;

                    // Never exceed abs > 1
                    TurnCommand = DockController.Next(TIMESTEP, desiredRotSpeed, MyWorld.Rocket.Body.RotationSpeed);
                    break;

                default:
                    break;
            }
        }

        private double Limit180Deg(double v)
        {
            if (v > Math.PI)
                return -(2 * Math.PI - v);
            else if (v <= -Math.PI)
                return -(-2 * Math.PI - v);
            else return v;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            pictureBox1.Refresh();

            // Check how much time was waited since last frame
            // Wait such that framerate matches
            int sleep = (int)((1000.0 / (DESIREDFRAMERATE * TimeRate)) - (double)lastFrame.ElapsedMilliseconds);
            lastFrame.Restart();
            if (sleep > 0)
                Thread.Sleep(sleep);
        }

        private void pictureBox1_MouseClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            // Check hitbox
            foreach (var button in Clickables)
                if (button.Rectangle.Contains(e.Location))
                    button.HandleClickEvent();
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // update framerate each x millis
            FrameCounter++;
            if (frameTimer.ElapsedMilliseconds > 250)
            {
                Framerate = FrameCounter * 4;

                frameTimer.Restart();
                FrameCounter = 0;

                // Recalc Trajectories (ALWAYS CONST TIMESTEP!)
                MyWorld.RecalcTrajectories(TIMESTEP);
            }

            // Check complex key events
            Keyevents.CheckKeys();

            for (int i = 0; i < TimeRate; i++)
                PhysicsFrame();

            DrawFrame(e.Graphics);
        }

        private void DrawFrame(Graphics g)
        {
            g.Clear(Color.Black);

            MyWorld.Draw(g, pictureBox1.Size);

            // Draw framerate
            g.DrawString("fps=" + Framerate, SystemFonts.DefaultFont, Brushes.White, 50, 0);

            // Draw Timerate
            g.DrawString("Timerate=" + TimeRate, SystemFonts.DefaultFont, Brushes.White, 50, SystemFonts.DefaultFont.Size * 2f);

            // Draw stats
            g.DrawString(
                $"f={MyWorld.Rocket.Body.Force:F2}, loc={MyWorld.Rocket.Body.Location:F2}, height={MyWorld.Rocket.Body.Location.Length - World.EARTHRADIUS:F2}, vel={MyWorld.Rocket.Body.Velocity:F2}, boost={MyWorld.Rocket.BoostersEnabled}",
                SystemFonts.DefaultFont, Brushes.White, 0, pictureBox1.Height - 20);
            g.DrawString(
                $"orient={MyWorld.Rocket.Body.Orientation:F2}, rot={MyWorld.Rocket.Body.RotationSpeed:F2}",
                SystemFonts.DefaultFont, Brushes.White, 0, pictureBox1.Height - 40);

            // Draw more stats
            double dist = (MyWorld.Station.Body.Location - MyWorld.Rocket.Body.Location).Length;
            if (dist < MinDistance)
                MinDistance = dist;
            if (dist > MaxDistance)
                MaxDistance = dist;
            g.DrawString(
                $"distance={dist:F2} (min={MinDistance:F2} | max={MaxDistance:F2}), Apsis={Apsis.Far:F2} ; {Apsis.Near:F2}",
                SystemFonts.DefaultFont, Brushes.White, 0, pictureBox1.Height - 60);

            // And More
            g.DrawString(
                $"Thrust={ThrustCommand:F2}, Turn={TurnCommand:F2}",
                SystemFonts.DefaultFont, Brushes.White, 0, pictureBox1.Height - 80);

            g.DrawString($"stage={stage} ({stageDescription})",
                SystemFonts.DefaultFont, Brushes.White, 0, pictureBox1.Height - 100);



            // Draw buttons
            foreach (Clickable button in Clickables)
            {
                g.DrawRectangle(Pens.Orange, button.Rectangle);
                if (button.ToggleState == true)
                    g.FillRectangle(new SolidBrush(Color.FromArgb(100, 255, 0xA0, 00)), button.Rectangle);
                g.DrawString(button.Label, SystemFonts.DefaultFont, Brushes.LightPink, button.Rectangle);
            }
        }
    }
}
