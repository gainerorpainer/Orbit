using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Drawing
{
    class Arrow : Drawable
    {
        public float Size { get; set; }

        public override void Draw(Graphics g, float zoom, PointF location, float orientation)
        {
            Pen pen = new Pen(Brushes.White, Size / zoom)
            {
                EndCap = System.Drawing.Drawing2D.LineCap.ArrowAnchor
            };

            PointF endPoint = new PointF(location.X + (float)Math.Cos(orientation) * 6f * Size / zoom, location.Y + (float)Math.Sin(orientation) * 6f * Size / zoom);
            g.DrawLine(pen, location, endPoint);
        }
    }
}
