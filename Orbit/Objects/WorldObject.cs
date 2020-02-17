using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit.Objects
{
    abstract class WorldObject : Physics.IBody, Drawing.IDrawable
    {
        private static int IdCounter = 0;
        public int Id { get; private set; } = IdCounter++;

        public Physics.Body Body { get; set; }
        public List<Drawing.Drawable> Drawables { get; set; }
        public Color ColorSheme { get; set; }
    }
}
