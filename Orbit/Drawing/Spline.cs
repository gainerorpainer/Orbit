using OrbitLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Drawing
{
    class Spline : Drawable
    {
        public List<Vector> Points { get; set; }
        public Pen Pen { get; set; } = Pens.White;
        public override void Draw(Graphics g, float zoom, PointF location, float orientation)
        {
            if (Points.Count > 1)
                g.DrawLines(Pen, Points.Select(x => x.ToPointF()).ToArray());
        }
    }
}
