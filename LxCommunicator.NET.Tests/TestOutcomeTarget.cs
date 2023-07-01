using FluentAssertions;
using System.Net.WebSockets;
using System.Reactive.Linq;
using Websocket.Client;
using Xunit.Abstractions;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Text;

namespace LxCommunicator.NET.Tests;
[Target("MyFirst")]
public sealed class TestOutcomeTarget : TargetWithLayout {
	private readonly ITestOutputHelper output;

	public TestOutcomeTarget(ITestOutputHelper output) {
		this.output = output;
	}

	protected override void Write(LogEventInfo logEvent) {
		string logMessage = this.Layout.Render(logEvent);

		if (this.output != null) {
			this.output.WriteLine(logMessage);
		}
	}
}