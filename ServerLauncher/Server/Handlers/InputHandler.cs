using System.Text;
using ServerLauncher.Server.Data;
using ServerLauncher.Server.Handlers.Structures;

namespace ServerLauncher.Server.Handlers;

public static class InputHandler
{
    private static readonly char[] Separator = {' '};

    public static readonly ColoredMessage BaseSection = new ColoredMessage(null, ConsoleColor.White);

    public static readonly ColoredMessage InputPrefix = new ColoredMessage("> ", ConsoleColor.Yellow);
    public static readonly ColoredMessage LeftSideIndicator = new ColoredMessage("...", ConsoleColor.Yellow);
    public static readonly ColoredMessage RightSideIndicator = new ColoredMessage("...", ConsoleColor.Yellow);

    public static int InputPrefixLength => InputPrefix?.Length ?? 0;

    public static int LeftSideIndicatorLength => LeftSideIndicator?.Length ?? 0;
    public static int RightSideIndicatorLength => RightSideIndicator?.Length ?? 0;

    public static int TotalIndicatorLength => LeftSideIndicatorLength + RightSideIndicatorLength;

    public static int SectionBufferWidth
    {
        get
        {
            try
            {
                return Console.BufferWidth - (1 + InputPrefixLength);
            }
            catch (Exception e)
            {
                Program.Logger.Error(nameof(SectionBufferWidth), e.Message);
                return 0;
            }
        }
    }

    public static string CurrentMessage { get; private set; }
    public static ColoredMessage[] CurrentInput { get; private set; } = {InputPrefix};
    public static int CurrentCursor { get; private set; }

    public static async void Write(Server server, CancellationToken cancellationToken)
    {
        try
        {
            var prevMessages = new ShiftingList(25);

            while (server.IsRunning && !server.IsStopping)
            {
                if (Program.Headless)
                {
                    break;
                }

                var message = await GetInputLineNew(server, cancellationToken, prevMessages);;

                if (string.IsNullOrEmpty(message)) continue;

                server.Log($">>> {message}", ConsoleColor.DarkMagenta);

                var separatorIndex = message.IndexOfAny(Separator);
                var commandName = (separatorIndex < 0 ? message : message.Substring(0, separatorIndex)).ToLower().Trim();
                
                if (commandName == string.Empty) continue;

                var callServer = true;
                
                server.Commands.TryGetValue(commandName, out var command);
                
                if (command != null)
                {
                    try
                    {
                        // Use double quotation marks to escape a quotation mark
                        command.Execute(separatorIndex < 0 || separatorIndex + 1 >= message.Length ? Array.Empty<string>() : Utilities.StringToArgs(message, separatorIndex + 1, escapeChar: '\"', quoteChar: '\"'));
                    }
                    catch (Exception e)
                    {
                        server.Log($"Error in command \"{commandName}\":{Environment.NewLine}{e}");
                    }

                    callServer = command.IsPassToGame;
                }

                if (callServer) server.SendMessage(message);
            }

            ResetInputParams();
        }
        catch (TaskCanceledException)
        {
            // Exit the Task immediately if cancelled
        }
    }

    /// <summary>
    /// Waits until <see cref="Console.KeyAvailable"/> returns true.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to check for cancellation.</param>
    /// <exception cref="TaskCanceledException">The task has been canceled.</exception>
    public static async Task WaitForKey(CancellationToken cancellationToken)
    {
        while (!Console.KeyAvailable)
        {
            await Task.Delay(10, cancellationToken);
        }
    }

    public static async Task<string> GetInputLineOld(Server server, CancellationToken cancellationToken)
    {
        StringBuilder message = new StringBuilder();
        while (true)
        {
            await WaitForKey(cancellationToken);

            var key = Console.ReadKey();

            switch (key.Key)
            {
                case ConsoleKey.Backspace:
                    if (message.Length != 0)
                        message.Remove(message.Length - 1, 1);
                    break;

                case ConsoleKey.Enter:
                    return message.ToString();

                default:
                    message.Append(key.KeyChar);
                    break;
            }
        }
    }

    public static async Task<string> GetInputLineNew(Server server, CancellationToken cancellationToken, ShiftingList prevMessages)
    {
        var curMessage = string.Empty;
        var message = string.Empty;
        
        var messageCursor = 0;
        var prevMessageCursor = -1;
        
        StringSections curSections = null;
        
        var lastSectionIndex = -1;
        var exitLoop = false;
        
        while (!exitLoop)
        {
            #region Key Press Handling

            await WaitForKey(cancellationToken);

            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Backspace:
                    if (messageCursor > 0 && message != string.Empty)
                        message = message.Remove(--messageCursor, 1);

                    break;

                case ConsoleKey.Delete:
                    if (messageCursor >= 0 && messageCursor < message.Length)
                        message = message.Remove(messageCursor, 1);

                    break;

                case ConsoleKey.Enter:
                    exitLoop = true;
                    break;

                case ConsoleKey.UpArrow:
                    prevMessageCursor++;
                    if (prevMessageCursor >= prevMessages.Count)
                        prevMessageCursor = prevMessages.Count - 1;

                    message = prevMessageCursor < 0 ? curMessage : prevMessages[prevMessageCursor];

                    break;

                case ConsoleKey.DownArrow:
                    prevMessageCursor--;
                    if (prevMessageCursor < -1)
                        prevMessageCursor = -1;

                    message = prevMessageCursor < 0 ? curMessage : prevMessages[prevMessageCursor];

                    break;

                case ConsoleKey.LeftArrow:
                    messageCursor--;
                    break;

                case ConsoleKey.RightArrow:
                    messageCursor++;
                    break;

                case ConsoleKey.Home:
                    messageCursor = 0;
                    break;

                case ConsoleKey.End:
                    messageCursor = message.Length;
                    break;

                case ConsoleKey.PageUp:
                    messageCursor -= SectionBufferWidth - TotalIndicatorLength;
                    break;

                case ConsoleKey.PageDown:
                    messageCursor += SectionBufferWidth - TotalIndicatorLength;
                    break;

                default:
                    message = message.Insert(messageCursor++, key.KeyChar.ToString());
                    break;
            }

            #endregion

            if (prevMessageCursor < 0)
                curMessage = message;

            // If the input is done and should exit the loop, break from the while loop
            if (exitLoop)
                break;

            if (messageCursor < 0)
                messageCursor = 0;
            else if (messageCursor > message.Length)
                messageCursor = message.Length;

            #region Input Printing Management

            // If the message has changed, re-write it to the console
            if (CurrentMessage != message)
            {
                if (message.Length > SectionBufferWidth && SectionBufferWidth - TotalIndicatorLength > 0)
                {
                    curSections = GetStringSections(message);

                    StringSection? curSection =
                        curSections.GetSection(IndexMinusOne(messageCursor), out int sectionIndex);

                    if (curSection != null)
                    {
                        lastSectionIndex = sectionIndex;

                        SetCurrentInput(curSection.Value.Section);
                        CurrentCursor = curSection.Value.GetRelativeIndex(messageCursor);
                        WriteInputAndSetCursor(true);
                    }
                    else
                    {
                        server.Error("Error while processing input string: curSection is null!");
                    }
                }
                else
                {
                    curSections = null;

                    SetCurrentInput(message);
                    CurrentCursor = messageCursor;

                    WriteInputAndSetCursor(true);
                }
            }
            else if (CurrentCursor != messageCursor)
            {
                try
                {
                    // If the message length is longer than the buffer width (being cut into sections), re-write the message
                    if (curSections != null)
                    {
                        var curSection =
                            curSections.GetSection(IndexMinusOne(messageCursor), out var sectionIndex);

                        if (curSection != null)
                        {
                            CurrentCursor = curSection.Value.GetRelativeIndex(messageCursor);

                            // If the cursor index is in a different section from the last section, fully re-draw it
                            if (lastSectionIndex != sectionIndex)
                            {
                                lastSectionIndex = sectionIndex;

                                SetCurrentInput(curSection.Value.Section);

                                WriteInputAndSetCursor(true);
                            }

                            // Otherwise, if only the relative cursor index has changed, set only the cursor
                            else
                            {
                                SetCursor();
                            }
                        }
                        else
                        {
                            server.Error("Error while processing input string: curSection is null!");
                        }
                    }
                    else
                    {
                        CurrentCursor = messageCursor;
                        SetCursor();
                    }
                }
                catch (Exception e)
                {
                    Program.Logger.Error(nameof(Write), e.Message);

                    CurrentCursor = messageCursor;
                    SetCursor();
                }
            }

            CurrentMessage = message;

            #endregion
        }

        // Reset the current input parameters
        ResetInputParams();

        if (!string.IsNullOrEmpty(message))
            prevMessages.Add(message);

        return message;
    }

    public static void ResetInputParams()
    {
        CurrentMessage = null;
        SetCurrentInput();
        CurrentCursor = 0;
    }

    public static void SetCurrentInput(params ColoredMessage[] coloredMessages)
    {
        var message = new List<ColoredMessage> {InputPrefix};

        if (coloredMessages != null)
            message.AddRange(coloredMessages);

        CurrentInput = message.ToArray();
    }

    public static void SetCurrentInput(string message)
    {
        var baseSection = BaseSection?.Clone();

        if (baseSection == null)
            baseSection = new ColoredMessage(message);
        else
            baseSection.Text = message;

        SetCurrentInput(baseSection);
    }

    private static StringSections GetStringSections(string message)
    {
        return StringSections.FromString(message, SectionBufferWidth, LeftSideIndicator, RightSideIndicator,
            BaseSection);
    }

    private static int IndexMinusOne(int index)
    {
        // Get the current section that the cursor is in (-1 so that the text before the cursor is displayed at an indicator)
        return Math.Max(index - 1, 0);
    }

    #region Console Management Methods

    public static void SetCursor(int messageCursor)
    {
        lock (Utilities.Lock)
        {
            if (Program.Headless) return;

            try
            {
                Console.CursorLeft = messageCursor + InputPrefixLength;
            }
            catch (Exception e)
            {
                Program.Logger.Error(nameof(SetCursor), e.Message);
            }
        }
    }

    public static void SetCursor()
    {
        SetCursor(CurrentCursor);
    }

    public static void WriteInput(ColoredMessage[] messages, bool clearConsoleLine = false)
    {
        lock (Utilities.Lock)
        {
            if (Program.Headless) return;

            foreach (var message in messages)
            {
                message.Write();
            }

            CurrentInput = messages;
        }
    }

    public static void WriteInput(bool clearConsoleLine = false)
    {
        WriteInput(CurrentInput, clearConsoleLine);
    }

    public static void WriteInputAndSetCursor(bool clearConsoleLine = false)
    {
        lock (Utilities.Lock)
        {
            WriteInput(clearConsoleLine);
            SetCursor();
        }
    }

    #endregion
}