using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Drawing
{
    class ZoomableCircle : Drawing.Drawable
    {
        public float Radius { get; set; } = 10;
        public bool IsZoomable { get; set; } = true;
        public Brush Brush { get; set; } = Brushes.White;

        public override void Draw(Graphics g, float zoom, PointF location, float orientation)
        {
            if (!IsZoomable)
                zoom = 1;

            g.FillEllipse(Brush, location.X - Radius / zoom, location.Y - Radius / zoom, (Radius * 2) / zoom, (Radius * 2) / zoom);
        }
    }
}
