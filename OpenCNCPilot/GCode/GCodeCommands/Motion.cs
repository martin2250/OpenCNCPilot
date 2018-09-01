using OpenCNCPilot.Util;
using System.Collections.Generic;

namespace OpenCNCPilot.GCode.GCodeCommands
{
	abstract class Motion : Command
	{
		public Vector3 Start;
		public Vector3 End;
		public double Feed;

		public Vector3 Delta
		{
			get
			{
				return End - Start;
			}
		}

		/// <summary>
		/// Total travel distance of tool
		/// </summary>
		public abstract double Length { get; }

		/// <summary>
		/// get intermediate point along the path
		/// </summary>
		/// <param name="ratio">ratio between intermediate point and end</param>
		/// <returns>intermediate point</returns>
		public abstract Vector3 Interpolate(double ratio);

		/// <summary>
		/// Split motion into smaller fragments, still following the same path
		/// </summary>
		/// <param name="length">the maximum allowed length per returned segment</param>
		/// <returns>collection of smaller motions that together form this motion</returns>
		public abstract IEnumerable<Motion> Split(double length);
	}
}
