﻿namespace Loxone.Communicator.Events {
	public class DaytimerEntry {
		/// <summary>
		/// Number of current mode
		/// </summary>
		public int Mode { get; private set; }

		/// <summary>
		/// From-time in minutes since midnight
		/// </summary>
		public int From { get; private set; }

		/// <summary>
		/// To-time in minutes since midnight
		/// </summary>
		public int To { get; private set; }

		/// <summary>
		/// Trigger
		/// </summary>
		public int NeedActivate { get; private set; }

		/// <summary>
		/// Value (if analog daytimer)
		/// </summary>
		public double Value { get; private set; }

		/// <summary>
		/// Reads the next daytimerEntry of a binaryReader
		/// </summary>
		/// <param name="reader">The binaryReader that should be read of</param>
		/// <returns>The read daytimerEntry</returns>
		public static DaytimerEntry Parse(System.IO.BinaryReader reader) {
			DaytimerEntry entry = new DaytimerEntry {
				Mode = reader.ReadInt32(),
				From = reader.ReadInt32(),
				To = reader.ReadInt32(),
				NeedActivate = reader.ReadInt32(),
				Value = reader.ReadDouble()
			};
			return entry;
		}
	}
}