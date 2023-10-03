using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Text;

namespace Loxone.Communicator {

	/// <summary>
	/// A container for a response received by the miniserver.
	/// </summary>
	public class LoxoneResponseMessageWithContainer : LoxoneResponseMessage{
		public LoxoneResponseMessageWithContainer(ReceivedRawLoxoneWebSocketMessage receivedMessage, LoxoneMessageLoadContainer container) 
			: base(receivedMessage, LoxoneResponseMessageCategory.Data, LoxoneDataFormat.ContentWithControl) {
			this.Container = container ?? throw new ArgumentNullException(nameof(container));
		}

		public LoxoneMessageLoadContainer Container { get; }

		
		public LoxoneMessageLoadContentWitControl<T> TryGetAsWebserviceContent<T>() {
			string s = this.TryGetContentAsString();
			if(s == null) {
				return default(LoxoneMessageLoadContentWitControl<T>);
			}
			var container = LoxoneContentHelper.ParseWebserviceContainer<T>(s);
			return container?.Response;
		}
	}
}