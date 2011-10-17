using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace ConcertoScreenSaver
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, ref RECT rect);
        
        private HwndSource winWPFContent;
        private Main winPreview;

        private struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            string[] args = e.Args;
            if (args.Length > 0)
            {
                // Get the 2 character command line argument
                string arg = args[0].ToLower(CultureInfo.InvariantCulture).Trim().Substring(0, 2);
                switch (arg)
                {
                    case "/o":
                        //Show the screensaver picking dialog
                        System.Diagnostics.Process.Start("control.exe", "desk.cpl,screensaver,@screensaver");
                        this.Shutdown();
                        break;
                    case "/c":
                        // Show the options dialog
                        Settings settings = new Settings();
                        settings.Show();
                        break;
                    case "/p":
                            //Show a preview
                            winPreview = new Main(1);

                            //A handle to the preview window is passed in
                            Int32 previewHandle = Convert.ToInt32(args[1]);

                            IntPtr pPreviewHnd = new IntPtr(previewHandle);

                            //Use the RECT struct & user32 dll to learn the size of the preview window
                            RECT lpRect = new RECT();
                            bool bGetRect = GetClientRect(pPreviewHnd, ref lpRect);

                            //Make our window fit in it
                            HwndSourceParameters sourceParams = new HwndSourceParameters("sourceParams");
                            sourceParams.PositionX = 0;
                            sourceParams.PositionY = 0;
                            sourceParams.Height = lpRect.Bottom - lpRect.Top;
                            sourceParams.Width = lpRect.Right - lpRect.Left;
                            sourceParams.ParentWindow = pPreviewHnd;

                            //WindowStyles.WS_VISIBLE | WindowStyles.WS_CHILD | WindowStyles.WS_CLIPCHILDREN
                            sourceParams.WindowStyle = (int)(0x52000000);

                            //Start to bind them together
                            winWPFContent = new HwndSource(sourceParams);
                            winWPFContent.Disposed += new EventHandler(winWPFContent_Disposed); //Makes sure the preview window exits!!!

                            //For now, we hide elements that don't scale correctly
                            //Later, the mainGrid will be able to handle all this for us
                            winPreview.lblAuthor.Height = 0;
                            winPreview.lblFeed.Height = 0;
                            winPreview.lblTitle.Height = 0;
                            winPreview.imgCore.Margin = new Thickness(1);

                            //Go!
                            winWPFContent.RootVisual = winPreview.mainGrid;
                        break;
                    case "/s":
                        // Show screensaver form
                        ShowScreensaver();
                        break;
                    default:
                        System.Windows.Application.Current.Shutdown();
                        break;
                }
            }
            else
            {
                // If no arguments were passed in, show the screensaver
                ShowScreensaver();
            }
        }

        /*
         * When the window is closed, shut things down
         */ 
        void winWPFContent_Disposed(object sender, EventArgs e)
        {
            winPreview.Close();
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Shows the screen-saver by creating an instance on Main.xaml per screen.
        /// </summary>
        private void ShowScreensaver()
        {
            //creates window on primary screen
            Main primaryWindow = new Main(0);
            primaryWindow.WindowStartupLocation = WindowStartupLocation.Manual;
            System.Drawing.Rectangle location = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            primaryWindow.WindowState = WindowState.Maximized;

            //creates window on other screens
            foreach (System.Windows.Forms.Screen screen in System.Windows.Forms.Screen.AllScreens)
            {
                if (screen == System.Windows.Forms.Screen.PrimaryScreen)
                    continue;

                Main window = new Main(0);
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                location = screen.Bounds;

                //covers entire monitor
                window.Left = location.X - 7;
                window.Top = location.Y - 7;
                window.Width = location.Width + 15;
                window.Height = location.Height + 15;
            }

            //show non-primary screen windows
            foreach (Window window in System.Windows.Application.Current.Windows)
            {
                if (window != primaryWindow)
                    window.Show();
            }

            ///shows primary screen window last
            primaryWindow.Show();
        }

        /*
         * Log the exception information in the event log,
         * let the user know what happenned if they want
         */
        private void Application_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            /*
             * This hack is used to close things down if the preview window runs awry,
             * its a bug that is difficult to reproduce but annoying.
             */
            string error_message = e.Exception.ToString();
            e.Handled = true;
            if (error_message.Contains("Invalid window handle"))
            {
                this.Shutdown();
                System.Windows.Forms.Application.Exit();
            }
            else
            {
                string message = "The Concerto Screensaver encountered an error.  Would you like help us out and send the debug information?";
                string caption = "Error: Concerto Screensaver";
                MessageBoxButtons buttons = MessageBoxButtons.YesNo;
                DialogResult result;

                result = System.Windows.Forms.MessageBox.Show(message, caption, buttons);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    Error error_window = new Error(e);
                    error_window.Show();
                }
                else
                {
                    e.Handled = true;
                    this.Shutdown();
                    System.Windows.Forms.Application.Exit();
                }
            }   
        }
    }
}
