namespace K400P_FnLock_Background
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool onlyInstance = false;
            Mutex mutex = new Mutex(true, "9e9bbd09-debd-4581-80f6-bdccdce1bafd", out onlyInstance);
            if (!onlyInstance) return;
            ApplicationConfiguration.Initialize();
            Application.Run(new TrayApp());
            GC.KeepAlive(mutex);
        }
    }
}