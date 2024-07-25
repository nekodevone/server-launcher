using System.Text.RegularExpressions;
using ServerLauncher.Logger;
using ServerLauncher.Server.Enums;
using ServerLauncher.Server.EventArgs;
using ServerLauncher.Server.Handlers.Enums;

namespace ServerLauncher.Server.Handlers;

public class OutputHandler
	{
		public static readonly Regex SmodRegex =
			new Regex(@"\[(DEBUG|INFO|WARN|ERROR)\] (\[.*?\]) (.*)", RegexOptions.Compiled | RegexOptions.Singleline);
		
		public static readonly char[] TrimChars = { '.', ' ', '\t', '!', '?', ',' };
		
		public static readonly char[] EventSplitChars = new char[] {':'};
		
		public OutputHandler(Server server)
		{
			_server = server;
		}
		
		// Temporary measure to handle round ends until the game updates to use this
		private const bool RoundEndCodeUsed = false;

		private readonly Server _server;

		public void HandleMessage(object source, MessageEventArgs message)
		{
			if (string.IsNullOrEmpty(message.Message))
			{
				return;
			}

			// Smod2 loggers pretty printing
			var match = SmodRegex.Match(message.Message);

			if (match.Success)
			{
				if (match.Groups.Count >= 3)
				{
					var logColor = match.Groups[1].Value.Trim() switch
					{
						"DEBUG" => ConsoleColor.DarkGray,
						"INFO" => ConsoleColor.Green,
						"WARN" => ConsoleColor.Yellow,
						"ERROR" => ConsoleColor.Red,
						_ => ConsoleColor.White
					};

					Log.Info(message.Message, _server.Id, color: logColor);

					// This return should be here
					return;
				}
			}

			var lowerMessage = message.Message.ToLower();
			if (!_server.SupportModFeatures.HasFlag(ModFeatures.CustomEvents))
			{
				switch (lowerMessage.Trim(TrimChars))
				{
					case "the round is about to restart! please wait":
						if (!RoundEndCodeUsed)
							ServerEvents.OnRoundEnded();
						break;

					case "new round has been started":
						ServerEvents.OnRoundStarted();
						break;

					case "level loaded. creating match":
						ServerEvents.OnStarted();
						break;

					case "server full":
						ServerEvents.OnFull();
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
						if (!RoundEndCodeUsed)
							ServerEvents.OnRoundEnded();
						break;

					case "round-start-event":
						ServerEvents.OnRoundStarted();
						break;

					case "server-start-event":
						ServerEvents.OnStarted();
						break;

					case "server-full-event":
						ServerEvents.OnFull();
						break;

					case "set-supported-features":
						if (int.TryParse(eventData, out var supportedFeatures))
						{
							_server.SupportModFeatures = (ModFeatures)supportedFeatures;
						}
						break;
				}

				// Don't print any MultiAdmin events
				return;
			}
			
			_server.Log(message.Message);
		}

		public void HandleAction(object source, byte action)
		{
			switch ((OutputCodes)action)
			{
				// This seems to show up at the waiting for players event
				case OutputCodes.RoundRestart:
					_server.IsLoading = false;
					ServerEvents.OnWaitingForPlayers();
					break;

				case OutputCodes.IdleEnter:
					ServerEvents.OnIdleEntered();
					break;

				case OutputCodes.IdleExit:
					ServerEvents.OnIdleExited();
					break;

				// Requests to reset the ExitAction status
				case OutputCodes.ExitActionReset:
					_server.SetServerRequestedStatus(ServerStatusType.Running);
					break;

				// Requests the Shutdown ExitAction with the intent to restart at any time in the future
				case OutputCodes.ExitActionShutdown:
					_server.SetServerRequestedStatus(ServerStatusType.ExitActionStop);
					break;

				// Requests the SilentShutdown ExitAction with the intent to restart at any time in the future
				case OutputCodes.ExitActionSilentShutdown:
					_server.SetServerRequestedStatus(ServerStatusType.ExitActionStop);
					break;

				// Requests the Restart ExitAction status with the intent to restart at any time in the future
				case OutputCodes.ExitActionRestart:
					_server.SetServerRequestedStatus(ServerStatusType.ExitActionRestart);
					break;

				// case OutputCodes.RoundEnd:
				// 	roundEndCodeUsed = true;
				// 	server.ForEachHandler<IEventServerRoundEnded>(roundEnd => roundEnd.OnServerRoundEnded());
				// 	break;

				default:
					Log.Debug(
						nameof(HandleAction),
						$"Received unknown output code ({action}), is MultiAdmin up to date? This error can probably be safely ignored.");
					break;
			}
		}
	}