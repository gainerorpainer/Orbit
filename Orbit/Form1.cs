using OrbitLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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

        readonly List<Clickable> Clickables = new List<Clickable>();


        readonly Controllers.PidController FlightController = new Controllers.PidController(1, 0.25, 0.1)
        {
            Clamp = 1.0
        };

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
        }

        private void PhysicsFrame()
        {
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

                // Calc apsis
                Apsis = OrbitalMechanics.CalcApsis(MyWorld.Rocket.Body.Mass, World.EARTHMASS,
                    MyWorld.Rocket.Body.Velocity,
                    MyWorld.Rocket.Body.Location,
                    World.GRAVITYCONST);
                Apsis.Near -= World.EARTHRADIUS;
                Apsis.Far -= World.EARTHRADIUS;
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
