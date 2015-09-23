using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using Sm4shCommand.Classes;

namespace Sm4shCommand
{
    static class Program
    {
        static Program()
        {
            Application.EnableVisualStyles();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            RegisterFileAssociations();

            if (args.Length >= 1)
            {
                if (args[0].EndsWith(".wrkspc", StringComparison.InvariantCultureIgnoreCase))
                {
                    Runtime.Instance.Manager.WorkspaceRoot = args[0];
                    Application.Run(Runtime.Instance);
                    return;
                }
            }

            Application.Run(Runtime.Instance);
        }

        private static void RegisterFileAssociations()
        {
            /***********************************/
            /**** Key1: Create ".abc" entry ****/
            /***********************************/
            Microsoft.Win32.RegistryKey key1 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true);

            key1.CreateSubKey("Classes");
            key1 = key1.OpenSubKey("Classes", true);

            key1.CreateSubKey(".wrkspc");
            key1 = key1.OpenSubKey(".wrkspc", true);
            key1.SetValue("", "Workspace"); // Set default key value

            key1.Close();

            /*******************************************************/
            /**** Key2: Create "DemoKeyValue\DefaultIcon" entry ****/
            /*******************************************************/
            Microsoft.Win32.RegistryKey key2 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true);

            key2.CreateSubKey("Classes");
            key2 = key2.OpenSubKey("Classes", true);

            key2.CreateSubKey("Workspace");
            key2 = key2.OpenSubKey("Workspace", true);

            key2.Close();

            /**************************************************************/
            /**** Key3: Create "DemoKeyValue\shell\open\command" entry ****/
            /**************************************************************/
            Microsoft.Win32.RegistryKey key3 = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("Software", true);

            key3.CreateSubKey("Classes");
            key3 = key3.OpenSubKey("Classes", true);

            key3.CreateSubKey("Workspace");
            key3 = key3.OpenSubKey("Workspace", true);

            key3.CreateSubKey("shell");
            key3 = key3.OpenSubKey("shell", true);

            key3.CreateSubKey("open");
            key3 = key3.OpenSubKey("open", true);

            key3.CreateSubKey("command");
            key3 = key3.OpenSubKey("command", true);
            key3.SetValue("", "\"" + Application.ExecutablePath + "\"" + " \"%1\""); // Set default key value

            key3.Close();
            SHChangeNotify(0x8000000, 0x1000, IntPtr.Zero, IntPtr.Zero);
        }
        [System.Runtime.InteropServices.DllImport("Shell32.dll")]
        private static extern int SHChangeNotify(int eventId, int flags, IntPtr item1, IntPtr item2);
    }
}
