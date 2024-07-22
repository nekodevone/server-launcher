using System.Text;

namespace ServerLauncher.Utility
{
	public static class EmptyExtensions
	{
		public static bool IsEmpty<T>(this IEnumerable<T> enumerable)
		{
			return !enumerable.Any();
		}

		public static bool IsNullOrEmpty<T>(this IEnumerable<T> enumerable)
		{
			return enumerable?.IsEmpty() ?? true;
		}

		public static bool IsEmpty(this Array array)
		{
			return array.Length <= 0;
		}

		public static bool IsNullOrEmpty(this Array array)
		{
			return array?.IsEmpty() ?? true;
		}

		public static bool IsEmpty<T>(this T[] array)
		{
			return array.Length <= 0;
		}

		public static bool IsNullOrEmpty<T>(this T[] array)
		{
			return array?.IsEmpty() ?? true;
		}

		public static bool IsEmpty<T>(this ICollection<T> collection)
		{
			return collection.Count <= 0;
		}

		public static bool IsNullOrEmpty<T>(this ICollection<T> collection)
		{
			return collection?.IsEmpty() ?? true;
		}
		

		public static bool IsEmpty(this StringBuilder stringBuilder)
		{
			return stringBuilder.Length <= 0;
		}
	}
}
