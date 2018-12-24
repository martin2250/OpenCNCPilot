using System;
using System.Windows.Media.Media3D;

namespace OpenCNCPilot.Util
{
	public struct Vector2 : IEquatable<Vector2>
	{

		private double x;

		private double y;

		public Vector2(double x, double y)
		{
			// Pre-initialisation initialisation
			// Implemented because a struct's variables always have to be set in the constructor before moving control
			this.x = 0;
			this.y = 0;

			// Initialisation
			X = x;
			Y = y;
		}

		public double X
		{
			get { return x; }
			set { x = value; }
		}

		public double Y
		{
			get { return y; }
			set { y = value; }
		}

		public static Vector2 operator +(Vector2 v1, Vector2 v2)
		{
			return
			(
				new Vector2
					(
						v1.X + v2.X,
						v1.Y + v2.Y
					)
			);
		}

		public static Vector2 operator -(Vector2 v1, Vector2 v2)
		{
			return
			(
				new Vector2
					(
						v1.X - v2.X,
						v1.Y - v2.Y
					)
			);
		}

		public static Vector2 operator *(Vector2 v1, double s2)
		{
			return
			(
				new Vector2
				(
					v1.X * s2,
					v1.Y * s2
				)
			);
		}

		public static Vector2 operator *(double s1, Vector2 v2)
		{
			return v2 * s1;
		}

		public static Vector2 operator /(Vector2 v1, double s2)
		{
			return
			(
				new Vector2
					(
						v1.X / s2,
						v1.Y / s2
					)
			);
		}

		public static Vector2 operator -(Vector2 v1)
		{
			return
			(
				new Vector2
					(
						-v1.X,
						-v1.Y
					)
			);
		}

		public static bool operator ==(Vector2 v1, Vector2 v2)
		{
			return
			(
				Math.Abs(v1.X - v2.X) <= EqualityTolerence &&
				Math.Abs(v1.Y - v2.Y) <= EqualityTolerence
			);
		}

		public static bool operator !=(Vector2 v1, Vector2 v2)
		{
			return !(v1 == v2);
		}

		public bool Equals(Vector2 other)
		{
			return other == this;
		}

		public override bool Equals(object other)
		{
			// Check object other is a Vector3 object
			if (other is Vector2)
			{
				// Convert object to Vector3
				Vector2 otherVector = (Vector2)other;

				// Check for equality
				return otherVector == this;
			}
			else
			{
				return false;
			}
		}

		public override int GetHashCode()
		{
			return
			(
				(int)((X + Y) % Int32.MaxValue)
			);
		}

		public Point3D ToPoint3D(double z)
		{
			return new Point3D(X, Y, z);
		}

		public double Magnitude { get { return Math.Sqrt(X * X + Y * Y); } }

		public const double EqualityTolerence = double.Epsilon;
	}
}
