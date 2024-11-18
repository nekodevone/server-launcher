using System.Text;
using ServerLauncher.Server.Data;
using ServerLauncher.Server.Handlers.Structures;

namespace ServerLauncher.Server.Handlers
{
    public class StringSections
    {
        public StringSections(StringSection[] sections)
        {
            Sections = sections;
        }

        public StringSection[] Sections { get; }

        public StringSection? GetSection(int index, out int sectionIndex)
        {
            sectionIndex = -1;

            for (int i = 0; i < Sections.Length; i++)
            {
                var stringSection = Sections[i];

                if (!stringSection.IsWithinSection(index))
                {
                    continue;
                }

                sectionIndex = i;
                return stringSection;
            }

            return null;
        }

        public StringSection? GetSection(int index)
        {
            foreach (var stringSection in Sections)
            {
                if (stringSection.IsWithinSection(index))
                    return stringSection;
            }

            return null;
        }

        public static StringSections FromString(string fullString, int sectionLength,
            ColoredMessage leftIndicator = null, ColoredMessage rightIndicator = null,
            ColoredMessage sectionBase = null)
        {
            var rightIndicatorLength = rightIndicator?.Length ?? 0;
            var totalIndicatorLength = (leftIndicator?.Length ?? 0) + rightIndicatorLength;

            if (fullString.Length > sectionLength && sectionLength <= totalIndicatorLength)
                throw new ArgumentException(
                    $"{nameof(sectionLength)} must be greater than the total length of {nameof(leftIndicator)} and {nameof(rightIndicator)}",
                    nameof(sectionLength));

            var sections = new List<StringSection>();

            if (string.IsNullOrEmpty(fullString))
                return new StringSections(sections.ToArray());

            // If the section base message is null, create a default one
            if (sectionBase == null)
                sectionBase = new ColoredMessage(null);

            // The starting index of the current section being created
            var sectionStartIndex = 0;

            // The text of the current section being created
            var curSecBuilder = new StringBuilder();

            for (int i = 0; i < fullString.Length; i++)
            {
                curSecBuilder.Append(fullString[i]);

                // If the section is less than the smallest possible section size, skip processing
                if (curSecBuilder.Length < sectionLength - totalIndicatorLength) continue;

                // Decide what the left indicator text should be accounting for the leftmost section
                var leftIndicatorSection = sections.Count > 0 ? leftIndicator : null;
                // Decide what the right indicator text should be accounting for the rightmost section
                var rightIndicatorSection =
                    i < fullString.Length - (1 + rightIndicatorLength) ? rightIndicator : null;

                // Check the section length against the final section length
                if (curSecBuilder.Length < sectionLength -
                    ((leftIndicatorSection?.Length ?? 0) + (rightIndicatorSection?.Length ?? 0)))
                {
                    continue;
                }

                // Copy the section base message and replace the text
                var section = sectionBase.Clone();
                section.Text = curSecBuilder.ToString();

                // Instantiate the section with the final parameters
                sections.Add(new StringSection(section, leftIndicatorSection, rightIndicatorSection,
                    sectionStartIndex, i));

                // Reset the current section being worked on
                curSecBuilder.Clear();
                sectionStartIndex = i + 1;
            }

            // If there's still text remaining in a section that hasn't been processed, add it as a section
            if (curSecBuilder.Length > 0)
            {
                // Only decide for the left indicator, as this last section will always be the rightmost section
                var leftIndicatorSection = sections.Count > 0 ? leftIndicator : null;

                // Copy the section base message and replace the text
                var section = sectionBase.Clone();
                section.Text = curSecBuilder.ToString();

                // Instantiate the section with the final parameters
                sections.Add(new StringSection(section, leftIndicatorSection, null, sectionStartIndex,
                    fullString.Length));
            }

            return new StringSections(sections.ToArray());
        }
    }
}