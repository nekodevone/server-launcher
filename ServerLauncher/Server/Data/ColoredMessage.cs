namespace ServerLauncher.Server.Data
{
	public class ColoredMessage : ICloneable
	{
		public ColoredMessage(string text, ConsoleColor textColor = ConsoleColor.White)
		{
			Text = text;
			TextColor = textColor;
		}
	
		public string Text { get; set; }
		public ConsoleColor TextColor { get; set; }

		public int Length => Text?.Length ?? 0;

		public bool Equals(ColoredMessage other) => string.Equals(Text, other.Text) && TextColor == other.TextColor;

		public void Write()
		{
			lock (Utilities.Lock)
			{
				var lastColor = Console.ForegroundColor;

				Console.ForegroundColor = TextColor;
				Console.WriteLine(Text);
				Console.ForegroundColor = lastColor;
			}
		}
	
		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ColoredMessage)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = Text != null ? Text.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ TextColor.GetHashCode();
				return hashCode;
			}
		}

		public static bool operator ==(ColoredMessage firstMessage, ColoredMessage secondMessage)
		{
			if (ReferenceEquals(firstMessage, secondMessage))
				return true;

			if (ReferenceEquals(firstMessage, null) || ReferenceEquals(secondMessage, null))
				return false;

			return firstMessage.Equals(secondMessage);
		}

		public static bool operator !=(ColoredMessage firstMessage, ColoredMessage secondMessage)
		{
			return !(firstMessage == secondMessage);
		}

		public override string ToString() => Text;

		public ColoredMessage Clone()
		{
			return new ColoredMessage(Text?.Clone() as string, TextColor);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}
	}
}