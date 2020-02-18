using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Orbit
{
    class CollisionEvents
    {
        public static void On_Collision_Rocket_Spacestation(World world, double energy)
        {
            MessageBox.Show("YOU WIN!");
        }

        public static void On_Collision_Rocket_Earth(World world, double energy)
        {
        }
    }
}
