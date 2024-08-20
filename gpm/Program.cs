using gpm.View;
using PatzminiHD.CSLib.Input.Console;

namespace gpm
{
    internal class Program
    {
        static void Main(string[] args)
        {
            try
            {
                MainView mainView = new MainView();
                mainView.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Critical Exception occured:\n\n" + ex.Message);
            }
        }
    }
}
