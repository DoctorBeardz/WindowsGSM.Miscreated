using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using WindowsGSM.Functions;
using WindowsGSM.GameServer.Engine;
using WindowsGSM.GameServer.Query;

namespace WindowsGSM.Plugins
{
	public class Miscreated : SteamCMDAgent // SteamCMDAgent is used because Miscreated relies on SteamCMD for installation and update process
	{
		// - Plugin Details
		public Plugin Plugin = new Plugin
		{
			name = "WindowsGSM.Miscreated", // WindowsGSM.XXXX
			author = "Beard",
			description = "🧩 WindowsGSM plugin for supporting Miscreated Dedicated Server",
			version = "1.0",
			url = "https://github.com/DoctorBeardz/WindowsGSM.Miscreated", // Github repository link (Best practice)
			color = "#38CDD4" // Color Hex
		};

		// - Standard Constructor and properties
		public Miscreated(ServerConfig serverData) : base(serverData) => base.serverData = _serverData = serverData;
		private readonly ServerConfig _serverData; // Store server start metadata, such as start ip, port, start param, etc

		// - Settings properties for SteamCMD installer
		public override bool loginAnonymous => true; // Miscreated does not require a steam account to install the server, so loginAnonymous = false
		public override string AppId => "302200"; // Game server appId, Miscreated is 302200

		// - Game server Fixed variables
		public override string StartPath => "Bin64_dedicated/MiscreatedServer.exe"; // Game server start path, for Miscreated, it is StartServer64.bat
		public string FullName = "Miscreated Dedicated Server"; // Game server FullName
		public bool AllowsEmbedConsole = true;  // Does this server support output redirect?
		public int PortIncrements = 2; // This tells WindowsGSM how many ports should skip after installation
		public object QueryMethod = new A2S(); // Query method should be use on current server type. Accepted value: null or new A2S() or new FIVEM() or new UT3()

		// - Game server default values
		public string Port = "64090"; // Default port
		public string QueryPort = "64092"; // Default query port
		public string Defaultmap = "Islands"; // Default map name
		public string Maxplayers = "50"; // Default maxplayers
		public string Additional = "-sv_port 64090 +sv_maxplayers 50 +map islands -mis_gameserverid 100 +sv_servername MyServer"; // Additional server start parameter

		// - Create a default cfg for the game server after installation
		public async void CreateServerCFG() { }

		// - Start server function, return its Process to WindowsGSM
		public async Task<Process> Start()

		{
			// Prepare start parameter
			var param = new StringBuilder($"{_serverData.ServerParam} -port {_serverData.ServerPort}");

			// Prepare Process
			var p = new Process
			{
				StartInfo =
				{
					WorkingDirectory = ServerPath.GetServersServerFiles(_serverData.ServerID),
					FileName = ServerPath.GetServersServerFiles(_serverData.ServerID, StartPath),
					Arguments = param.ToString(),
					WindowStyle = ProcessWindowStyle.Minimized,
					UseShellExecute = false
				},
				EnableRaisingEvents = true
			};

			// Set up Redirect Input and Output to WindowsGSM Console if EmbedConsole is on
			if (AllowsEmbedConsole)
			{
				p.StartInfo.CreateNoWindow = true;
				p.StartInfo.RedirectStandardInput = true;
				p.StartInfo.RedirectStandardOutput = true;
				p.StartInfo.RedirectStandardError = true;
				var serverConsole = new ServerConsole(_serverData.ServerID);
				p.OutputDataReceived += serverConsole.AddOutput;
				p.ErrorDataReceived += serverConsole.AddOutput;

				// Start Process
				try
				{
					p.Start();
				}
				catch (Exception e)
				{
					Error = e.Message;
					return null; // return null if fail to start
				}

				p.BeginOutputReadLine();
				p.BeginErrorReadLine();
				return p;
			}

			// Start Process
			try
			{
				p.Start();
				return p;
			}
			catch (Exception e)
			{
				Error = e.Message;
				return null; // return null if fail to start
			}
		}

		// - Stop server function
		public async Task Stop(Process p)
		{
			await Task.Run(() =>
			{
				if (p.StartInfo.RedirectStandardInput)
				{
					// Send "quit" command to StandardInput stream if EmbedConsole is on
					p.StandardInput.WriteLine("quit");
				}
				else
				{
					// Send "quit" command to game server process MainWindow
					ServerConsole.SendMessageToMainWindow(p.MainWindowHandle, "quit");
				}
			});
		}
	}
}