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

public class WebsocketClientTestsV3Base {

	private const string constIP = "192-168-50-50.504f94a181b0.dyndns.loxonecloud.com";
	private const string constPort = "80";
	private static string constUrl = $"ws://{constIP}:{constPort}/ws/rfc6455";
	private static readonly Uri WebsocketUrl = new Uri(constUrl);
	protected readonly ITestOutputHelper OutputHelper;

	//protected ConnectionConfiguration GetConfig() => new ConnectionConfiguration(
	//	"testminiserver.loxone.com",
	//	7777,
	//	2,
	//	"098802e1-02b4-603c-ffffeee000d80cfd",
	//	"LxCommunicator.NET.Websocket") { };

	protected LoxoneUser GetUser() => new LoxoneUser {
		UserName = "lan",
		UserPassword = "JQ9Hsa9tP5xtnW",
	};

	protected ConnectionConfiguration GetConfig() => new ConnectionConfiguration(
		"192-168-50-50.504f94a181b0.dyndns.loxonecloud.com",
		80,
		2,
		"098802e1-02b4-603c-ffffeee000d80cfd",
		"LxCommunicator.NET.Websocket"
	) {
	};

	protected HttpWebserviceClient GetHttpClient() {
		return new HttpWebserviceClient(GetConfig());
	}

	protected LoxoneWebsocketClient GetLoxoneWebsocketClient() {
		return new LoxoneWebsocketClient(this.GetConfig());
	}


	protected LoxoneClient GetLoxoneClient(Action<LoxoneClientConfiguration> changeConfig = null) {
		var connection = this.GetConfig();

		var resultConfig = new LoxoneClientConfiguration {
			ConnectionConfiguration = connection,
			LoxoneUser = this.GetUser(),
		};

		if (changeConfig != null) {
			changeConfig(resultConfig);
		}

		var result = new LoxoneClient(resultConfig);

		return result;
	}


	public WebsocketClientTestsV3Base(ITestOutputHelper output, bool logIntoTest = true) {
		OutputHelper = output;
		OutputHelper.WriteLine("Test");
		InitLogging(OutputHelper, logIntoTest);
	}

	private static void InitLogging(ITestOutputHelper output, bool logIntoTest = true) {

		if (logIntoTest == false) {
			return;
		}

		var config = new LoggingConfiguration();
		var consoleTarget = new ConsoleTarget {
			Name = "console",
			Layout = "${longdate}|${level:uppercase=true}|${logger}|${message}",
		};

		string loggers = "*";
		loggers = "Websocket.Client.WebsocketClient";


		var testTarget = new TestOutcomeTarget(output);

		//config.AddRule(LogLevel.Debug, LogLevel.Fatal, consoleTarget, "*");
		config.AddRule(LogLevel.Trace, LogLevel.Fatal, testTarget, loggers);
		LogManager.Configuration = config;

		//Log.Logger = new LoggerConfiguration()
		//	.MinimumLevel.Verbose()
		//	.WriteTo.TestOutput(output, LogEventLevel.Verbose)
		//	.CreateLogger();
	}
}