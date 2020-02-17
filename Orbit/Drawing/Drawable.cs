using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Drawing
{

    abstract class Drawable
    {
        public bool Visible { get; set; } = true;
        public abstract void Draw(System.Drawing.Graphics g, float zoom, System.Drawing.PointF location, float orientation);
    }

    interface IDrawable
    {
        List<Drawable> Drawables { get; set; }
    }
}
