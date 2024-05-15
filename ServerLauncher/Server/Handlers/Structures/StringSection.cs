using ServerLauncher.Server.Data;

namespace ServerLauncher.Server.Handlers.Structures;

public struct StringSection
{
    public StringSection(ColoredMessage text, ColoredMessage leftIndicator, ColoredMessage rightIndicator,
        int minIndex, int maxIndex)
    {
        Text = text;

        LeftIndicator = leftIndicator;
        RightIndicator = rightIndicator;

        MinIndex = minIndex;
        MaxIndex = maxIndex;
    }

    public ColoredMessage Text { get; }

    public ColoredMessage LeftIndicator { get; }
    
    public ColoredMessage RightIndicator { get; }

    public ColoredMessage[] Section => new ColoredMessage[] {LeftIndicator, Text, RightIndicator};

    public int MinIndex { get; }
    
    public int MaxIndex { get; }
    
    public bool IsWithinSection(int index)
    {
        return index >= MinIndex && index <= MaxIndex;
    }

    public int GetRelativeIndex(int index)
    {
        return index - MinIndex + (LeftIndicator?.Length ?? 0);
    }
}