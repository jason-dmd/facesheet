using System;
using System.IO;
using System.Windows.Forms;

namespace FacesheetParser
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// 
        static readonly string log = "../../log.txt";

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Import());
        }
        public static void Mlog(string msg, string lvl = "INFO", bool initialLog = false)
        {
            try
            {
                string now = DateTime.UtcNow.ToString();
                string finalMessage = string.Format("{0} | {1} | {2}", now, lvl, msg);

                if (initialLog)
                {
                    File.WriteAllText(log, finalMessage + Environment.NewLine);
                }
                else
                {
                    File.AppendAllText(log, finalMessage + Environment.NewLine);
                }
                if (lvl == "ERROR")
                {
                    MessageBox.Show(finalMessage);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format("ERROR: {0}", ex.ToString()));
            }
        }
    }
}
