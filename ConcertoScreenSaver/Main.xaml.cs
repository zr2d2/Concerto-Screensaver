using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace ConcertoScreenSaver
{
    struct content
    {
        public string title;
        public string url;
        public string author;
        public string id;
        public string feed;
        public string hash;
    }
    /// <summary>
    /// Interaction logic for Main.xaml
    /// </summary>
    public partial class Main : Window
    {
        public ArrayList contents = new ArrayList();
        public int next_content;
        BitmapImage next_image;
        public Storyboard fadeOut;
        public Storyboard fadeIn;
        public string base_path;
        public int display_mode;

        public bool offline_state;
        
        public Main(int mode)
        {
            InitializeComponent();
            if (Environment.UserDomainName == "NT AUTHORITY")
            {
                WebRequest.DefaultWebProxy = null;
            }
            display_mode = mode;
            offline_state = false;
            fadeIn = (Storyboard)TryFindResource("fadeIn"); 
            fadeOut = (Storyboard)TryFindResource("fadeOut");
            Loaded += new RoutedEventHandler(OnLoaded);
            //Previews shouldn't hide the cursor
            if (display_mode == 1)
            {
                this.Cursor = Cursors.Arrow;
            }
            StartMain();
        }

        void OnLoaded(object sender, EventArgs e)
        {
            if(display_mode == 0){ //The preview window doesn't need handler
                KeyDown += new KeyEventHandler(Main_KeyDown);
                /*
                * This DEBUG stuff disables the code that detects mouse movement,
                * it would close the program automatically otherwise.
                */ 
#if !DEBUG
                this.Topmost = true;
                MouseMove += new MouseEventHandler(Main_MouseMove);
                MouseDown += new MouseButtonEventHandler(Main_MouseDown);
#endif      
            }
        }
 
        /*
         * Handles the main content rotation by downloading
         * an image if necessary and saving it to cache.
         * Fires img_Loaded when the image is ready,
         * and img_Error if the image isn't going to work.
         */ 
        private void Cycle(object sender, EventArgs e)
        {
            Random random = new Random();
            if (contents.Count <= 0)
            {
                no_content("Everything");
                return;
            }
            int id = random.Next(contents.Count);
            content item = (content)contents[id];

            BitmapImage img = new BitmapImage();
            string cache_path = base_path + item.hash + ".jpg";
            if (File.Exists(cache_path))
            {
                img.BeginInit();
                img.UriSource = new Uri(cache_path, UriKind.RelativeOrAbsolute);
                img.EndInit();
                next_image = img;
                next_content = id;
                img_Loaded();
            }
            else
            {
                WebClient downloader = new WebClient();
                try
                {
                    downloader.DownloadFile(item.url, cache_path);
                    img.BeginInit();
                    img.UriSource = new Uri(cache_path, UriKind.RelativeOrAbsolute);
                    img.EndInit();
                    next_image = img;
                    next_content = id;
                    img_Loaded();
                }
                catch
                {
                    offline(true);
                    img_Error(id);
                }
            }   
        }
        /*
         * Handles an error downloading or decoding an image,
         * for now we just move that content item from the array.
         * Content won't be shown again until the screensaver is restarted
         */ 
        private void img_Error(int id)
        {
            contents.RemoveAt(id); //Mark that content as bad
            Cycle(null, EventArgs.Empty);
        }
        
        /*
         * With the image loaded, the fadeOut event is bound
         */ 
        private void img_Loaded()
        {
            /*
             * The image has been loaded into next_image, so we start the fade out sequence. 
             */
            fadeOut.Begin(imgCore);
        }
        
        /*
         * After the fade out animation is complete, the new image is swapped in
         * and then faded in.
         */
        private void img_Switch(object sender, EventArgs e)
        {
            imgCore.Source = next_image;
            content item = (content)contents[next_content];
            lblTitle.Content = item.title;
            lblAuthor.Content = item.author;
            lblFeed.Content = item.feed;
            
            fadeIn.Begin(imgCore);
        }

        /*
         * Parse the Concerto RSS feed and add items to the content array.
         */ 
        private void ParseRSS(string url)
        {
            XDocument FeedXML = new XDocument();

            string xml_cache_path = base_path + StringToHashString(url) + ".xml";

            try
            {
                /*
                 * Try to load the latest and greatest data from Concerto, and cache it locally so
                 * that next time we are 'offline', the Concertos can still work.
                 */
                FeedXML = XDocument.Load(url);

                if (FeedXML.Descendants("item").Count() > 0)
                {
                    FeedXML.Save(xml_cache_path);
                }
                offline(false);
            }
            catch
            {
                /*
                 * So the internet isn't working right now, or the Concertos are having problems.
                 * I think its more likely the internet, since Concerto is a heavily 'fortified'
                 * piece of software.  Especially the API that we use for this screensaver, that
                 * thing never breaks.
                 */
                offline(true);
                if (File.Exists(xml_cache_path))
                {
                    try
                    {
                        FeedXML = XDocument.Load(xml_cache_path);
                    }
                    catch
                    {
                        no_cache();
                        return;
                    }
                }
                else
                {
                    /*
                     * You're offline and you don't have a cached copy of this feed. 
                     */
                    no_cache();
                    return;
                }
            }

            /*
             * Parse the feed 
             */

            var channels = from channel in FeedXML.Descendants("channel")
                           select new
                           {
                               title = channel.Element("title") != null ? channel.Element("title").Value : ""
                           };
            if(channels.Count() < 1){
                //Invalid feed.  Couldn't get title.
                return;
            }
            var feed_title = channels.First().title;
            lblTitle.Content = "Loading " + feed_title;
            
            var items = from feed in FeedXML.Descendants("item")
                        select new
                        {
                            title = feed.Element("title") != null ? feed.Element("title").Value : "",
                            author = feed.Element("author") != null ? feed.Element("author").Value : "",
                            id = feed.Element("guid") != null ? feed.Element("guid").Value : "",
                            url = feed.Element("enclosure") != null ? feed.Element("enclosure").Attribute("url").Value : ""
                        };
            foreach (var item in items.ToArray())
            {
                content itm = new content();
                itm.title = item.title;
                itm.author = item.author;
                itm.id = item.id;
                itm.url = item.url;
                itm.hash = StringToHashString(itm.id);
                itm.feed = feed_title;

                //Only use items with an enclosure
                if (itm.url.Length > 0)
                {
                    contents.Add(itm);
                }
            }            
            if (contents.Count <= 0)
            {
                no_content(feed_title);
                return;
            }
            if (offline_state)
            {
                lblTitle.Content = "Loaded " + feed_title  + " from cache.";
            }
            else
            {
                lblTitle.Content = "Loaded " + feed_title;
            }
        }

        private void no_cache()
        {
            lblTitle.Content = "Unable to find local cache.";
        }

        void offline(bool status)
        {
            offline_state = status;
            if (status)
            {
                /*
                 * Load up something to indicate there isn't a connection
                 * between the client and the server
                 */
                lblTitle.Content = "Unable to connect to the internet.";
            }
        }

        void no_content(string feedname)
        {
            /*
             * It might be appropriate to show something on the screen
             * indicating there is no content, so people know whats going on.
             */
            lblTitle.Content = feedname + " looks empty.";
        }
        
        /*
         * Pretty standard functions to catch events that should close a screensaver.
         * I actually didn't know the screensaver was incharge of this.
         */ 
        void Main_KeyDown(object sender, KeyEventArgs e)
        {
#if DEBUG
            /*
             * Allows a debug build to trigger an unhandled exception by pushing the X key
             */ 
            if (e.Key == Key.X)
            {
                throw new Exception("Key pressed");
                return;
            }
            else if (e.Key == Key.D)
            {
                Window debug_stats = new debug_status();
                this.Cursor = Cursors.Arrow;
                debug_stats.Show();
                return;
            }
#endif
            Application.Current.Shutdown();
        }

        void Main_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Application.Current.Shutdown();
        }

        bool _isActive;
        Point _mousePosition;
        void Main_MouseMove(object sender, MouseEventArgs e)
        {
            Point currentPosition = e.GetPosition(this);

            // Set IsActive and MouseLocation only the first time this event is called.
            if (_isActive == false)
            {
                _mousePosition = currentPosition;
                _isActive = true;
            }
            else
            {
                // If the mouse has moved significantly since first call, close.
                if ((Math.Abs(_mousePosition.X - currentPosition.X) > 50) ||
                    (Math.Abs(_mousePosition.Y - currentPosition.Y) > 50))
                {
                    Application.Current.Shutdown();
                }
            }
        }
        
        /*
         * Welcome screensaver.
         */ 
        private void StartMain()
        {
            //Create the cache directory if needed
            string base_cache = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (String.IsNullOrEmpty(base_cache))
            {
                base_cache = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            }
            base_path = base_cache + @"\Concerto\";

            if (!Directory.Exists(base_path))
            {
                Directory.CreateDirectory(base_path);
            }
            //Holds XML hashes for cache cleanup
            List<string> xml_hashes = new List<string>();

            foreach (string feed_s in Properties.Settings.Default.Feeds.Split(','))
            {
                string url = Properties.Settings.Default.ConcertoRoot + Properties.Resources.feed_query_string + feed_s;
                xml_hashes.Add(StringToHashString(url));
                ParseRSS(url);
            }
            if (display_mode == 0) //Don't clear the cache for a preview!
            {
                /*
                 *  With the RSS loaded, we'll clean the cache up in another thread.
                 *  The xml_hashes were already generated, back when the feeds were processed.
                 */
                List<string> image_hashes = new List<string>();
                foreach (content item in contents)
                {
                    image_hashes.Add(item.hash);
                }
                UsesCacheClean cache_thread = CacheClean;
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, cache_thread, image_hashes, "*.jpg");
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, cache_thread, xml_hashes, "*.xml");
            }
            
            //If this is a preview, start the cycling without waiting for WPF to be ready
            if (display_mode == 1)
            {
                StartCycle();
            }
        }

        /*
         * Given a list of known files and their extensions,
         * remove all the ones that don't belong.
         */ 
        delegate void UsesCacheClean(List<string> hashes, string pattern);
        void CacheClean(List<string> hashes, string pattern)
        {
            DirectoryInfo cache_dir = new DirectoryInfo(base_path);
            FileInfo[] files = cache_dir.GetFiles(pattern);
            foreach (FileInfo file in files)
            {
                string name = Path.GetFileNameWithoutExtension(file.Name);
                if (!hashes.Contains(name) && !file.IsReadOnly)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch
                    {
                        //Give up, try this again later.
                    }
                }
            }
        }

        /*
         * Generate a hash of a string
         */ 
        static string StringToHashString(string input)
        {
            byte[] tmpSource = ASCIIEncoding.ASCII.GetBytes(input);
            byte[] tmpHash = new MD5CryptoServiceProvider().ComputeHash(tmpSource);
            int i;
            StringBuilder sOutput = new StringBuilder(tmpHash.Length);
            for (i = 0; i < tmpHash.Length; i++)
            {
                sOutput.Append(tmpHash[i].ToString("X2"));
            }
            return sOutput.ToString();
        }

        /*
         * Wait for all the graphics to be loaded and rendered before
         * starting the cycle and loop sequences.
         */ 
        private void WPF_Ready(object sender, EventArgs e)
        {
            if (display_mode == 0) //The real screensaver uses this fancy logic
            {
                System.Threading.Thread.Sleep(2000);
                StartCycle();
            }
        }

        /*
         * Kicks off the content cycling.
         */ 
        private void StartCycle()
        {
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(Cycle);
            dispatcherTimer.Interval = new TimeSpan(0, 0, Properties.Settings.Default.SlideTime); //Rotate every N seconds
            dispatcherTimer.Start();

            //Manually trigger the first cycle after 2 seconds
            Cycle(null, EventArgs.Empty);
        }
    }
}
