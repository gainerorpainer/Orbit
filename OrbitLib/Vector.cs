using System;
using System.Diagnostics;
using System.Drawing;

namespace OrbitLib
{
    public struct Vector
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Length => Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
        public double Angle => Math.Atan2(Y, X);

        public static readonly Vector NULL = new Vector();

        public Vector(double x, double y)
            : this()
        {
            X = x;
            Y = y;
        }

        public Vector(Point p)
            : this()
        {
            X = p.X;
            Y = p.Y;
        }

        public Vector(PointF p)
            : this()
        {
            X = p.X;
            Y = p.Y;
        }

        public override string ToString()
        {
            return $"{X:F2};{Y:F2}";
        }

        public static double CrossProduct(Vector v1, Vector v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        public static Vector operator *(double scalar, Vector v2)
        {
            return new Vector(scalar * v2.X, scalar * v2.Y);
        }


        public static Vector operator *(Vector v1, double scalar)
        {
            return scalar * v1;
        }

        public static Vector operator /(Vector v1, double scalar)
        {
            return new Vector(v1.X / scalar, v1.Y / scalar);
        }

        public static Vector operator +(Vector p1, Vector p2)
        {
            return new Vector(p1.X + p2.X, p1.Y + p2.Y);
        }

        public static Vector operator -(Vector p1, Vector p2)
        {
            return new Vector(p1.X - p2.X, p1.Y - p2.Y);
        }

        public static Vector operator -(Vector p1)
        {
            return NULL - p1;
        }

        public override bool Equals(object obj)
        {
            if (obj is Vector vect2)
                return (X == vect2.X) && (Y == vect2.Y);
            else
                return false;
        }

        public override int GetHashCode()
        {
            return X.GetHashCode() + 43 * Y.GetHashCode();
        }

        public static bool operator ==(Vector left, Vector right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Vector left, Vector right)
        {
            return !(left == right);
        }

        public Point ToPoint()
        {
            return new Point((int)X, (int)Y);
        }

        public PointF ToPointF()
        {
            return new PointF((float)X, (float)Y);
        }
    }
}