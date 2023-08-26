using FluentAssertions;
using System.Net.WebSockets;
using System.Reactive.Linq;
using Websocket.Client;
using Xunit.Abstractions;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Text;
using Loxone.Communicator;

namespace LxCommunicator.NET.Tests;

public class WebsocketClientTestsV3 : WebsocketClientTestsV3Base {

	private const string constIP = "192-168-50-50.504f94a181b0.dyndns.loxonecloud.com";
	private const string constPort = "80";
	private static string constUrl = $"ws://{constIP}:{constPort}/ws/rfc6455";
	private static readonly Uri WebsocketUrl = new Uri(constUrl);
	private readonly ITestOutputHelper _output;


	private static ConnectionConfiguration GetConfig() => new ConnectionConfiguration(
		"192-168-50-50.504f94a181b0.dyndns.loxonecloud.com",
		80,
		2,
		"098802e1-02b4-603c-ffffeee000d80cfd",
		"LxCommunicator.NET.Websocket"
	) {
	};

	public WebsocketClientTestsV3(ITestOutputHelper output) : base(output) {
	}

	[Fact]
	public async Task OnStarting_ShouldGetInfoResponse() {
		using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
		string received = null;
		var receivedEvent = new ManualResetEvent(false);

		var reconnectionHappenedEvent = new ManualResetEvent(false);

		client.ReconnectionHappened.Subscribe(xx => {
			reconnectionHappenedEvent.Set();
		});

		var subscription = client.MessageReceived.Subscribe(msg => {
			received = msg.MessageType == WebSocketMessageType.Text ? msg.Text : Encoding.UTF8.GetString(msg.Binary); // msg.Text;

			_output.WriteLine($"Received {received}");

			//00000000: 0306 0000 0000 0000                      ........


			receivedEvent.Set();
		});

		await client.Start();

		reconnectionHappenedEvent.WaitOne(TimeSpan.FromSeconds(30));

		client.Send("ping");

		receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

		Assert.NotNull(received);
	}

	[Fact]
	public async Task OnStarting_Subscription_Deleted() {
		using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
		string received = null;
		var receivedEvent = new ManualResetEvent(false);

		var subscription = client.MessageReceived.Subscribe(msg => {
			received = msg.MessageType == WebSocketMessageType.Text ? msg.Text : Encoding.UTF8.GetString(msg.Binary); // msg.Text;

			receivedEvent.Set();
		});

		subscription.Dispose();

		await client.Start();

		client.Send("ping");

		receivedEvent.WaitOne(TimeSpan.FromSeconds(5));

		received.Should().BeNull();
	}

	[Fact]
	public async Task SendMessageBeforeStart_ShouldWorkAfterStart() {
		using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
		string received = null;
		var receivedCount = 0;
		var receivedEvent = new ManualResetEvent(false);

		client.Send("ping");
		client.Send("ping");
		client.Send("ping");
		client.Send("ping");

		client
			.MessageReceived
			//.Where(x => x.Text.ToLower().Contains("pong"))
			.Subscribe(msg => {
				receivedCount++;
				received = msg.MessageType == WebSocketMessageType.Text ? msg.Text : Encoding.UTF8.GetString(msg.Binary); // msg.Text;

				if (receivedCount >= 7)
					receivedEvent.Set();
			});

		await client.Start();

		client.Send("ping");
		client.Send("ping");
		client.Send("ping");

		receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

		Assert.NotNull(received);
	}

	[Fact]
	public async Task SendBinaryMessage_ShouldWork() {
		using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
		string received = null;
		var receivedEvent = new ManualResetEvent(false);

		client.MessageReceived.Subscribe(msg => {
			var msgText = msg.Text ?? string.Empty;
			if (msgText.Contains("400")) {
				received = msgText;
				receivedEvent.Set();
			}
		});

		await client.Start();
		client.Send(new byte[] { 10, 14, 15, 16 });

		receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

		Assert.NotNull(received);
	}

	[Fact]
	public async Task Starting_MultipleTimes_ShouldWorkWithNoExceptions() {
		for (int i = 0; i < 3; i++) {
			using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
			await client.Start();
			await Task.Delay(i * 20);
		}
	}

	[Fact]
	public async Task DisabledReconnecting_ShouldWorkAsExpected() {
		using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
		var receivedCount = 0;
		var receivedEvent = new ManualResetEvent(false);

		client.IsReconnectionEnabled = false;
		client.ReconnectTimeout = TimeSpan.FromSeconds(2);

		client.MessageReceived.Subscribe(msg => {
			receivedCount++;
			if (receivedCount >= 2)
				receivedEvent.Set();
		});

		await client.Start();
		await Task.Delay(5000);
		await client.Stop(WebSocketCloseStatus.Empty, string.Empty);

		await Task.Delay(5000);

		await client.Start();
		await Task.Delay(1000);

		receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

		Assert.Equal(2, receivedCount);
	}

	[Fact]
	public async Task DisabledReconnecting_ShouldWorkAtRuntime() {
		using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
		var receivedCount = 0;

		client.IsReconnectionEnabled = true;
		client.ReconnectTimeout = TimeSpan.FromSeconds(5);

		client.MessageReceived.Subscribe(msg => {
			receivedCount++;
			if (receivedCount >= 2)
				client.IsReconnectionEnabled = false;
		});

		await client.Start();
		await Task.Delay(17000);

		Assert.Equal(2, receivedCount);
	}

	[Fact]
	public async Task OnClose_ShouldWorkCorrectly() {
		using IWebsocketClient client = new WebsocketClient(WebsocketUrl);
		client.ReconnectTimeout = TimeSpan.FromSeconds(5);

		string received = null;
		var receivedCount = 0;
		var receivedEvent = new ManualResetEvent(false);
		var disconnectionCount = 0;
		DisconnectionInfo disconnectionInfo = null;

		client.MessageReceived.Subscribe(msg => {
			receivedCount++;
			received = msg.Text;
		});

		client.DisconnectionHappened.Subscribe(x => {
			disconnectionCount++;
			disconnectionInfo = x;
		});

		await client.Start();

		_ = Task.Run(async () => {
			client.Send("ping");
			await Task.Delay(2000);
			var success = await client.Stop(WebSocketCloseStatus.InternalServerError, "server error 500");
			Assert.True(success);
			receivedEvent.Set();
		});

		receivedEvent.WaitOne(TimeSpan.FromSeconds(30));

		Assert.NotNull(received);
		Assert.Equal(2, receivedCount);

		var nativeClient = client.NativeClient;
		Assert.NotNull(nativeClient);
		Assert.Equal(1, disconnectionCount);
		Assert.Equal(DisconnectionType.ByUser, disconnectionInfo.Type);
		Assert.Equal(WebSocketCloseStatus.InternalServerError, disconnectionInfo.CloseStatus);
		Assert.Equal("server error 500", disconnectionInfo.CloseStatusDescription);
		Assert.Equal(WebSocketState.Closed, nativeClient.State);
		Assert.Equal(WebSocketCloseStatus.InternalServerError, nativeClient.CloseStatus);
		Assert.Equal("server error 500", nativeClient.CloseStatusDescription);

		// check that reconnection is disabled
		await Task.Delay(7000);
		Assert.Equal(1, receivedCount);
	}


	//private static void InitLogging(ITestOutputHelper output) {

	//	var config = new LoggingConfiguration();
	//	var consoleTarget = new ConsoleTarget {
	//		Name = "console",
	//		Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}",
	//	};


	//	var testTarget = new TestOutcomeTarget(output);

	//	//config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget, "*");
	//	config.AddRule(LogLevel.Trace, LogLevel.Fatal, testTarget, "*");
	//	LogManager.Configuration = config;

	//	//Log.Logger = new LoggerConfiguration()
	//	//	.MinimumLevel.Verbose()
	//	//	.WriteTo.TestOutput(output, LogEventLevel.Verbose)
	//	//	.CreateLogger();
	//}
}