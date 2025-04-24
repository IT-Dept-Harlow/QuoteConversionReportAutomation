using System;
using System.Configuration;
using System.Windows.Forms;

namespace CrystalReportWrapper // Your wrapper's namespace
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Logger.Initialize();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Run the main form which handles the tray icon and pipe server
            Application.Run(new TrayApplicationContext());
        }
    }
}
