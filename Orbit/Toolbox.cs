using Orbit.Physics;
using OrbitLib;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit
{
    public static class Toolbox
    {
        static Random rng = new Random();

        public static Color TransformColor(Color color, double scalar)
        {
            int limit255(double x) => (int)Math.Min(255, x);
            return Color.FromArgb(limit255(color.A + scalar * 255), limit255(color.R + scalar * 255), limit255(color.G + scalar * 255), limit255(color.B + scalar * 255));
        }

        public static Color TransformColor(Color color, int a, int r, int g, int b)
        {
            int addlimit255(int x, int y) => Math.Min(255, x + y);
            return Color.FromArgb(addlimit255(color.A, a), addlimit255(color.R, r), addlimit255(color.G, g), addlimit255(color.B, b));
        }

        public static string RandomString(int length)
        {
            StringBuilder s = new StringBuilder();
            for (int i = 0; i < length; i++)
                s.Append((char)(rng.Next() % 93 + 33));
            return s.ToString();
        }

        /// <summary>
        /// Checks if the given testpoint is inside the bounds of a given polygon
        /// </summary>
        /// <param name="polygon">"Open" Polygon (lastpoint != firstpoint)</param>
        /// <param name="testPoint">Point to be checked</param>
        /// <returns>True if it is inside</returns>
        public static bool IsInsidePolygon(List<Vector> polygon, Vector testPoint)
        {
            int polygonLength = polygon.Count;
            int i = 0;
            bool inside = false;
            // x, y for tested point.
            double pointX = testPoint.X, pointY = testPoint.Y;
            // start / end point for the current polygon segment.
            double startX, startY, endX, endY;
            var endPoint = polygon[polygonLength - 1];
            endX = endPoint.X;
            endY = endPoint.Y;
            while (i < polygonLength)
            {
                startX = endX;
                startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.X;
                endY = endPoint.Y;
                //
                inside ^= (endY > pointY ^ startY > pointY) /* ? pointY inside [startY;endY] segment ? */
                          && /* if so, test if it is under the segment */
                          ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }
            return inside;
        }

        /// <summary>
        /// Gets the smallest rectangle that contains all points 
        /// </summary>
        /// <param name="points">Points to be contained</param>
        /// <returns>Rectangle with size and location</returns>
        public static System.Drawing.RectangleF GetBoundaryRect(IEnumerable<Vector> points)
        {
            float xmin = (float)points.Min(x => x.X);
            float ymin = (float)points.Min(x => x.Y);
            float width = (float)points.Max(x => x.X) - xmin;
            float height = (float)points.Max(x => x.Y) - ymin;
            return new System.Drawing.RectangleF(xmin, ymin, width, height);
        }

        /// <summary>
        /// Gets the smallest square that contains all points 
        /// </summary>
        /// <param name="points">Points to be contained</param>
        /// <returns>Rectangle with size and location</returns>
        public static System.Drawing.RectangleF GetBoundarySquare(IEnumerable<Vector> points)
        {
            var rect = GetBoundaryRect(points);
            var squareDim = Math.Max(rect.Width, rect.Height);
            if (squareDim != rect.Width)
            {
                // Increase width
                rect.X -= 0.5f * (squareDim - rect.Width);
                rect.Width = squareDim;
                return rect;
            }

            // Increase height
            rect.Y -= 0.5f * (squareDim - rect.Height);
            rect.Height = squareDim;
            return rect;
        }

        public static Vector Rotate(Vector vector, double orientation)
        {
            return new Vector(vector.X * Math.Cos(orientation) - vector.Y * Math.Sin(orientation),
                vector.X * Math.Sin(orientation) + vector.Y * Math.Cos(orientation));
        }


        /// <summary>
        /// Determines the area of the given polygon
        /// </summary>
        /// <param name="polygon">"Open" Polygon (lastpoint != firstpoint)</param>
        /// <returns>Area</returns>
        public static double CalcPolygonArea(List<Vector> polygon)
        {
            /*
             * [This method adds] the areas of the trapezoids defined by the polygon's edges dropped to the X-axis. When the program considers a bottom edge of a polygon, the calculation gives a negative area so the space between the polygon and the axis is subtracted, leaving the polygon's area.
             * The total calculated area is negative if the polygon is oriented clockwise [so the] function simply returns the absolute value.
             * This method gives strange results for non-simple polygons (where edges cross).
             */
            return Math.Abs(polygon.Take(polygon.Count - 1)
             .Select((p, i) => (polygon[i + 1].X - p.X) * (polygon[i + 1].Y + p.Y))
             .Sum() / 2);
        }

        /// <summary>
        /// Quickly calculates the center of a polygon using the boundary rect
        /// </summary>
        /// <param name="polygon">Open/closed polygon</param>
        /// <returns>Center vector</returns>
        public static Vector GetRectCenter(IEnumerable<Vector> polygon)
        {
            var rect = GetBoundaryRect(polygon);
            return new Vector(rect.Location) + new Vector(rect.Width / 2, rect.Height / 2);
        }

        static readonly Random Rng_ = new Random();
        /// <summary>
        /// Permutates the entries in the input set and returns them
        /// </summary>
        /// <typeparam name="T">Set value type</typeparam>
        /// <param name="list">Input set</param>
        /// <returns>Set with random order</returns>
        public static IOrderedEnumerable<T> RandomOrder<T>(IEnumerable<T> list)
        {
            return list.OrderBy(x => Rng_.Next());
        }

        /// <summary>
        /// Moves all points given by polygon by the displacement vector
        /// </summary>
        /// <param name="polygon">"Open" Polygon (lastpoint != firstpoint)</param>
        /// <param name="displacementVector">Movement vector</param>
        /// <returns>polygon displaced by the vector</returns>
        public static IEnumerable<Vector> DisplacePolygon(IEnumerable<Vector> polygon, Vector displacementVector)
        {
            return polygon.Select(x => x + displacementVector);
        }
    }
}
