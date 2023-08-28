using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Loxone.Communicator {

	public class LoxoneMessageSystem : LoxoneMessage {
		public LoxoneMessageSystem(LoxoneMessageSystemType loxoneMessageSystemType) : base(LoxoneMessageType.Systems) {
			this.LoxoneMessageSystemType = loxoneMessageSystemType;
		}

		[JsonProperty(Order = 30)]
		public LoxoneMessageSystemType LoxoneMessageSystemType { get; set; }
	}
}