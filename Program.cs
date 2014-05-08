using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace jjget
{

    static class Program
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int SendMessage(IntPtr hWnd,
                int Msg, bool wParam, [MarshalAs(UnmanagedType.LPWStr)]string lParam);
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

        public static int SetCueText(TextBox tbctl,string txt)
        {
            return SendMessage(tbctl.Handle, 0x1501, false, txt);
        }
    }
}
