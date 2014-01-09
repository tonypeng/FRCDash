using System;

namespace Dashboard
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            //System.Threading.Thread.Sleep(1000);
            using (Dashboard game = new Dashboard())
            {
                game.Run();
            }
        }
    }
#endif
}

