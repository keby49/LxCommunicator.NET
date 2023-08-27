using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Loxone.Communicator {

	public class LoxoneMessageSystem<TData> : LoxoneMessageSystem {
		public LoxoneMessageSystem(LoxoneMessageSystemType loxoneMessageSystemType, TData data) : base(loxoneMessageSystemType) {
			this.Data = data;
		}

		public TData Data { get; set; }

	}
}