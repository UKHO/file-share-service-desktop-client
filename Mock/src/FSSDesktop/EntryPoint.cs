using System;

namespace FSSDesktop
{
    internal class EntryPoint
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var application = new FSSDesktopApplication();
            application.Start();
        }
    }
}