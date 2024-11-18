namespace ServerLauncher.Utility
{
    public static class Utilities
    {
        public static string DateTime => System.DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss");

        private static string TimeStamp
        {
            get
            {
                var now = System.DateTime.Now;
                return $"[{now.Hour:00}:{now.Minute:00}:{now.Second:00}]";
            }
        }

        /// <summary>
        /// Добавляет метку времени к предоставленному сообщению
        /// </summary>
        /// <param name="message">Сообщение</param>
        public static string AddTimeStampToMessage(string message)
        {
            return string.IsNullOrEmpty(message) ? message : $"{TimeStamp} {message}";
        }
    }
}