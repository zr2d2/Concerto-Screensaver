using System;
using System.Reflection;
using System.Windows;

namespace ConcertoScreenSaver
{
    /// <summary>
    /// Interaction logic for Error.xaml
    /// </summary>
    public partial class Error : Window
    {
        public Error(System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            InitializeComponent();
            string strVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();

            Assembly[] assems = AppDomain.CurrentDomain.GetAssemblies();
            string assemblies = "\n";
            foreach (Assembly assem in assems)
                assemblies += assem.ToString() + "\n";

            txtError.Text = "Program Version: " + strVersion + "\n" +
                            "Win32 Version: " + Environment.Version.ToString() + ", OS: " + Environment.OSVersion.ToString() + "\n"
                            + e.Exception.ToString() +
                            assemblies;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
