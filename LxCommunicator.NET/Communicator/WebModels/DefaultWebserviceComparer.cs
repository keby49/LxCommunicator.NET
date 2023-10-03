using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Text;
using System.Threading;
using NLog;
using Org.BouncyCastle.Asn1.Ocsp;

namespace Loxone.Communicator {
	/// <summary>
	/// Used for determining whether or not a respond belongs to a request
	/// </summary>
	public class DefaultWebserviceComparer : IComparer<string> {
		/// <summary>
		/// The used comparer
		/// </summary>
		public static IComparer<string> Comparer = new DefaultWebserviceComparer();
		/// <summary>
		/// Initialises a new comparer
		/// </summary>
		private DefaultWebserviceComparer() { }
		/// <summary>
		/// Normalises a string
		/// </summary>
		/// <param name="value">the text that should be normalised</param>
		/// <returns>the normalised text</returns>
		private string Normalize(string value) {
			value = value.Trim().TrimStart('/');
			if (value.StartsWith("jdev", StringComparison.OrdinalIgnoreCase)) {
				value = value.Substring(1);
			}

			//value = value.Replace("+", "%2B");
			//value = Uri.EscapeDataString(value);

			return value;
		}
		/// <summary>
		/// Compares 2 texts
		/// </summary>
		/// <param name="x">Text 1</param>
		/// <param name="y">Text 2</param>
		/// <returns>Whether the texts match or not</returns>
		public int Compare(string x, string y) {
			x = Normalize(x);
			y = Normalize(y);
			return StringComparer.OrdinalIgnoreCase.Compare(x, y);
		}
	}
}
