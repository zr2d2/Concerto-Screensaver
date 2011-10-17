using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System;
using System.IO;

namespace ConcertoScreenSaver
{
    /// <summary>
    /// Interaction logic for debug_status.xaml
    /// </summary>
    public partial class debug_status : Window
    {
        public debug_status()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //Extract the server name with a regex
            Match match = Regex.Match(Properties.Settings.Default.ConcertoRoot.ToLower(), "://(.*)/");
            string server = match.Groups[1].Value;

            lblVersion.Content = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            lblRemoteServer.Content = Properties.Settings.Default.ConcertoRoot;
            lblFeedPath.Content = Properties.Resources.feed_query_string;
            lblSysinfoPath.Content = Properties.Resources.sysinfo_query_string;
            if (dns_test(server))
            {
                lblDNSLookup.Content = "OK";
            } else {
                lblDNSLookup.Content = "FAIL";
            }
            ping_test(server);
            lblCacheLocation.Content = cache_location();
            lblCurUser.Content = current_user();
            
        }
        private bool dns_test(string server)
        {
            try
            {
                IPAddress[] ips = Dns.GetHostAddresses(server);
                if (ips.Length >= 1)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch
            {
               return false;
            }
        }
        private void ping_test(string server)
        {
            Ping pingSender = new Ping();
            PingOptions options = new PingOptions();

            options.DontFragment = true;

            string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            int timeout = 120;
            PingReply reply = pingSender.Send(server, timeout, buffer, options);
            if (reply.Status == IPStatus.Success)
            {
                lblPingStats.Content = "OK - " + reply.RoundtripTime.ToString();
            }
            else
            {
                lblPingStats.Content = "FAIL - " + reply.Status.ToString();
            }
        }

        private string cache_location()
        {
            string base_cache = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (String.IsNullOrEmpty(base_cache)){
                base_cache = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
                
            string base_path = base_cache + @"\Concerto\";
            if (Directory.Exists(base_path))
            {
                return  "OK: " + base_path;
            }
            else
            {
                return "Missing: " + base_path;
            }
        }

        private string current_user()
        {
            return Environment.UserDomainName + @"/" + Environment.UserName;
        }
    }
}
