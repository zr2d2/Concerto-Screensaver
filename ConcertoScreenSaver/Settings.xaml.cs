using System;
using System.Collections;
using System.Linq;
using System.Windows;
using System.Xml.Linq;

namespace ConcertoScreenSaver
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            lblVersion.Content = "Version " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
#if DEBUG
            lblVersion.Content = lblVersion.Content + "D";
#endif
        }

        private void btnLoad_Click(object sender, RoutedEventArgs e)
        {
            load();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            save();
        }

        public T Cast<T>(object obj, T Type)
        {
            return (T)obj;
        }

        private void load()
        {
            string url = ConcertoRoot.Text + Properties.Resources.sysinfo_query_string;
            XDocument SystemXML = new XDocument();
            try
            {
                SystemXML = XDocument.Load(url);
            }
            catch
            {
                MessageBox.Show("Unable to download feed data.  Verify url and internet connectivity");
            }
            var feeds = from feed in SystemXML.Descendants("feed")
                        select new
                        {
                            Name = feed.Element("name").Value,
                            Id = feed.Element("id").Value
                        };
            if (feeds.Count() > 0)
            {
                Hashtable feed_by_id = new Hashtable();
                foreach (var feed in feeds.ToArray())
                {
                    feed_by_id.Add(feed.Id, feed);
                }
                FeedBox.ItemsSource = feeds;
                FeedBox.DisplayMemberPath = "Name";
                FeedBox.SelectedValuePath = "Id";

                foreach (string feed_s in Properties.Settings.Default.Feeds.Split(','))
                {
                    if (feed_by_id.ContainsKey(feed_s))
                    {
                        FeedBox.SelectedItems.Add(feed_by_id[feed_s]);
                    }
                }
            }
        }
        private void save()
        {
            //Only save the feeds if there the list has been loaded
            if (FeedBox.Items.Count > 0)
            {
                var feeds = FeedBox.SelectedItems;
                string[] selected_feeds = new string[feeds.Count];
                int i = 0;
                foreach (var feed in feeds)
                {
                    var item = Cast(feed, new { Name = "", Id = "" });
                    selected_feeds[i] = item.Id;
                    i++;
                }
                Properties.Settings.Default.Feeds = String.Join(",", selected_feeds);
            }
            
            //Save everything
            Properties.Settings.Default.Save();

            //Shut down the preview window
            Application.Current.Shutdown();
        }
    }
    
}
