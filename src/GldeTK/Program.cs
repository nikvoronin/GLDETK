using System;

namespace GldeTK
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            using (MainForm mainForm = new MainForm())
            {
                mainForm.Run();
            }
        }
    }
}
