using OpenPronounceSharp.Manager;
using OpenPronounceSharp.Model;
using System.Runtime.InteropServices;

namespace OpenPronounceSharp.WinApp
{
    internal static class Program
    {
        private const int SC_CLOSE = 0xF060;
        private const int MF_BYCOMMAND = 0x00000000;

        [DllImport("kernel32.dll")]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll")]
        private static extern bool FreeConsole();

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);

        [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
        static extern int RemoveMenu(IntPtr hMenu, int nPos, int flags);

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LoadLibraries();

            SettingManager.LoadConfig();

            Setting setting = SettingManager.Setting;

            bool showConsoleWindow = setting.ShowConsoleWindow;
            bool consoleWindowIsOpening = false;

            if (showConsoleWindow)
            {
                AllocConsole();

                consoleWindowIsOpening = true;

                IntPtr consoleWindow = FindWindow(null, Console.Title);

                if (consoleWindow != IntPtr.Zero)
                {
                    IntPtr systemMenu = GetSystemMenu(consoleWindow, IntPtr.Zero);
                    RemoveMenu(systemMenu, SC_CLOSE, MF_BYCOMMAND);
                }
            }                   

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new frmMain());

            if(consoleWindowIsOpening)
            {
                FreeConsole();
            }           
        }

        private static void LoadLibraries()
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes", "win-x64", "native");

                if (Directory.Exists(folder))
                {
                    var files = new DirectoryInfo(folder).GetFiles("*.dll");

                    foreach (var file in files)
                    {
                        NativeLibrary.Load(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }                      
    }
}