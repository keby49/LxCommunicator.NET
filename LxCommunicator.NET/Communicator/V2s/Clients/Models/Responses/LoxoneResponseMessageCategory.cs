﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Loxone.Communicator {
	public enum LoxoneResponseMessageCategory {
		Uknown,
		Systems,
		Data,
		EventStates,
	}
}