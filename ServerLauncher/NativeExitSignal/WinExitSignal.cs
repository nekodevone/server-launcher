using System.Runtime.InteropServices;

namespace ServerLauncher.NativeExitSignal
{
    public class WinExitSignal : IExitSignal
    {
        /// <summary>
        /// Событие, вызываемое при получении сигнала завершения работы.
        /// </summary>
        public event EventHandler Exit;

        /// <summary>
        /// Импорт метода из Kernel32.dll для установки обработчика событий консоли.
        /// </summary>
        /// <param name="handler">Делегат обработчика событий.</param>
        /// <param name="add">Флаг для добавления или удаления обработчика.</param>
        /// <returns>Успешность операции.</returns>
        [DllImport("Kernel32")]
        public static extern bool SetConsoleCtrlHandler(HandlerRoutine handler, bool add);

        /// <summary>
        /// Делегат, используемый в качестве обработчика событий для SetConsoleCtrlHandler.
        /// </summary>
        /// <param name="ctrlType">Тип сигнала управления.</param>
        /// <returns>Успешность обработки сигнала.</returns>
        public delegate bool HandlerRoutine(CtrlTypes ctrlType);

        /// <summary>
        /// Перечисление типов сигналов управления, отправляемых в обработчик.
        /// </summary>
        public enum CtrlTypes
        {
            CTRL_C_EVENT,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT,
            CTRL_SHUTDOWN_EVENT
        }

        /// <summary>
        /// Need this as a member variable to avoid it being garbage collected.
        /// </summary>
        private readonly HandlerRoutine mHr;

        public WinExitSignal()
        {
            mHr = ConsoleCtrlCheck;

            SetConsoleCtrlHandler(mHr, true);
        }

        /// <summary>
        /// Handle the ctrl types
        /// </summary>
        /// <param name="ctrlType"></param>
        /// <returns></returns>
        private bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CTRL_C_EVENT:
                case CtrlTypes.CTRL_BREAK_EVENT:
                case CtrlTypes.CTRL_CLOSE_EVENT:
                case CtrlTypes.CTRL_LOGOFF_EVENT:
                case CtrlTypes.CTRL_SHUTDOWN_EVENT:
                    Exit?.Invoke(this, EventArgs.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ctrlType), ctrlType, null);
            }

            return true;
        }
    }
}