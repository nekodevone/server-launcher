using System.Text.RegularExpressions;
using ServerLauncher.Interfaces.Events;
using ServerLauncher.Server.Data;
using ServerLauncher.Server.Enums;
using ServerLauncher.Server.EventArgs;

namespace ServerLauncher.Server.Handlers;

public class OutputHandler
	{
		public static readonly Regex SmodRegex =
			new Regex(@"\[(DEBUG|INFO|WARN|ERROR)\] (\[.*?\]) (.*)", RegexOptions.Compiled | RegexOptions.Singleline);
		public static readonly char[] TrimChars = { '.', ' ', '\t', '!', '?', ',' };
		public static readonly char[] EventSplitChars = new char[] {':'};

		private readonly Server server;

		private enum OutputCodes : byte
		{
			//0x00 - 0x0F - reserved for colors

			RoundRestart = 0x10,
			IdleEnter = 0x11,
			IdleExit = 0x12,
			ExitActionReset = 0x13,
			ExitActionShutdown = 0x14,
			ExitActionSilentShutdown = 0x15,
			ExitActionRestart = 0x16,
			RoundEnd = 0x17
		}

		// Temporary measure to handle round ends until the game updates to use this
		private bool roundEndCodeUsed = false;

		public OutputHandler(Server server)
		{
			this.server = server;
		}

		public void HandleMessage(object source, MessageEventArgs message)
		{
			if (message.Message == null)
				return;
			
			/*
			if (message.Message != string.Empty)
			{
				// Parse the color byte
				message.Text = (ConsoleColor)message.Color;

				// Smod2 loggers pretty printing
				var match = SmodRegex.Match(message.Text);
				if (match.Success)
				{
					if (match.Groups.Count >= 3)
					{
						switch (match.Groups[1].Value.Trim())
						{
							case "DEBUG":
								Program.Logger.Debug(match);
								break;

							case "INFO":
								levelColor = ConsoleColor.Green;
								break;

							case "WARN":
								levelColor = ConsoleColor.DarkYellow;
								break;

							case "ERROR":
								levelColor = ConsoleColor.Red;
								break;
						}

						server.Write(
							new[]
							{
								new ColoredMessage($"[{match.Groups[1].Value}] ", levelColor),
								new ColoredMessage($"{match.Groups[2].Value} ", tagColor),
								new ColoredMessage(match.Groups[3].Value, msgColor)
							}, msgColor);

						// P.S. the format is [Info] [courtney.exampleplugin] Something interesting happened
						// That was just an example

						// This return should be here
						return;
					}
				}
				*/

				var lowerMessage = message.Message.ToLower();
				if (!server.SupportModFeatures.HasFlag(ModFeatures.CustomEvents))
				{
					switch (lowerMessage.Trim(TrimChars))
					{
						case "the round is about to restart! please wait":
							if (!roundEndCodeUsed)
								server.ForEachHandler<IEventServerRoundEnded>(roundEnd => roundEnd.OnServerRoundEnded());
							break;

						case "new round has been started":
							server.ForEachHandler<IEventServerRoundStarted>(roundStart => roundStart.OnServerRoundStarted());
							break;

						case "level loaded. creating match":
							server.ForEachHandler<IEventServerStarted>(serverStart => serverStart.OnServerStarted());
							break;

						case "server full":
							server.ForEachHandler<IEventServerFull>(serverFull => serverFull.OnServerFull());
							break;
					}
				}

			if (lowerMessage.StartsWith("multiadmin:"))
			{
				// 11 chars in "multiadmin:"
				var eventMessage = message.Message.Substring(11);

				// Split event and event data
				var eventSplit = eventMessage.Split(EventSplitChars, 2);

				var @event = eventSplit[0].ToLower();
				var eventData = eventSplit.Length > 1 ? eventSplit[1] : null; // Handle events with no data

				switch (@event)
				{
					case "round-end-event":
						if (!roundEndCodeUsed)
							server.ForEachHandler<IEventServerRoundEnded>(roundEnd => roundEnd.OnServerRoundEnded());
						break;

					case "round-start-event":
						server.ForEachHandler<IEventServerRoundStarted>(roundStart => roundStart.OnServerRoundStarted());
						break;

					case "server-start-event":
						server.ForEachHandler<IEventServerStarted>(serverStart => serverStart.OnServerStarted());
						break;

					case "server-full-event":
						server.ForEachHandler<IEventServerFull>(serverFull => serverFull.OnServerFull());
						break;

					case "set-supported-features":
						if (int.TryParse(eventData, out var supportedFeatures))
						{
							server.SupportModFeatures = (ModFeatures)supportedFeatures;
						}
						break;
				}

				// Don't print any MultiAdmin events
				return;
			}

			server.Log(message.Message);
		}

		public void HandleAction(object source, byte action)
		{
			switch ((OutputCodes)action)
			{
				// This seems to show up at the waiting for players event
				case OutputCodes.RoundRestart:
					server.IsLoading = false;
					server.ForEachHandler<IEventServerWaitingForPlayers>(waitingForPlayers => waitingForPlayers.OnServerWaitingForPlayers());
					break;

				case OutputCodes.IdleEnter:
					server.ForEachHandler<IEventServerIdleEntered>(idleEnter => idleEnter.OnServerIdleEntered());
					break;

				case OutputCodes.IdleExit:
					server.ForEachHandler<IEventIdleExited>(idleExit => idleExit.OnServerIdleExited());
					break;

				// Requests to reset the ExitAction status
				case OutputCodes.ExitActionReset:
					server.SetServerRequestedStatus(ServerStatusType.Running);
					break;

				// Requests the Shutdown ExitAction with the intent to restart at any time in the future
				case OutputCodes.ExitActionShutdown:
					server.SetServerRequestedStatus(ServerStatusType.ExitActionStop);
					break;

				// Requests the SilentShutdown ExitAction with the intent to restart at any time in the future
				case OutputCodes.ExitActionSilentShutdown:
					server.SetServerRequestedStatus(ServerStatusType.ExitActionStop);
					break;

				// Requests the Restart ExitAction status with the intent to restart at any time in the future
				case OutputCodes.ExitActionRestart:
					server.SetServerRequestedStatus(ServerStatusType.ExitActionRestart);
					break;

				case OutputCodes.RoundEnd:
					roundEndCodeUsed = true;
					server.ForEachHandler<IEventServerRoundEnded>(roundEnd => roundEnd.OnServerRoundEnded());
					break;

				default:
					Program.Logger.Debug(
						nameof(HandleAction),
						$"Received unknown output code ({action}), is MultiAdmin up to date? This error can probably be safely ignored.");
					break;
			}
		}
	}