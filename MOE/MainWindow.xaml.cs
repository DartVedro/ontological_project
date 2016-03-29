using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.ComponentModel;
using System.Threading;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Xml.Linq;
using VDS.RDF.Query;

namespace MOE
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static bool loading = false;
        bool loaded = false;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void listBoxItem_DoubleClick(object sender, RoutedEventArgs e)
        {
            if(loaded)
            {
                string gameName = (sender as ListBoxItem).Content as string;
                BackgroundWorker worker = new BackgroundWorker();

                worker.DoWork += (_sender, _e) =>
                {
                    string game = (_e.Argument as string);
                    var result = Sparql(game);

                    _e.Result = result;
                };

                worker.RunWorkerCompleted += (_sender, _e) =>
                {
                    loading = false;
                    loaded = true;
                    LoadingGrid.Visibility = Visibility.Hidden;
                    MainContentGrid.IsEnabled = true;

                    var results = _e.Result as SparqlResultSet;

                    if (results == null || results.IsEmpty)
                    {
                        sorryBorder.Visibility = Visibility.Visible;
                        foundBorder.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        sorryBorder.Visibility = Visibility.Hidden;
                        foundBorder.Visibility = Visibility.Visible;
                        gameNameLabel.Content = gameName;
                        publisherLabel.Content = results.First().Value("publisher").ToString().Replace("@en", "");
                        
                        //Hyperlink
                        offSiteLabel.Inlines.Clear();
                        offSiteLabel.Inlines.Add(results.First().Value("offSite").ToString());
                        offSiteLabel.NavigateUri = new Uri(results.First().Value("offSite").ToString());
                        offSiteLabel.RequestNavigate += Hyperlink_RequestNavigate;
                        textBlockUri.Inlines.Add(offSiteLabel);
                        
                        dateLabel.Content = results.First().Value("date").ToString().Remove(10);
                        gameModesLabel.Content = "";
                        
                        HashSet<string> gameModes = new HashSet<string>();
                        foreach (var res in results)
                        {
                           
                            gameModes.Add(res.Value("gameModes").ToString().Replace("@en", ""));
                        }
                        foreach (string mode in gameModes)
                        {
                            gameModesLabel.Content += mode + "\n";
                        }
                        
                    }

                };

                loading = true;
                LoadingGrid.Visibility = Visibility.Visible;
                MainContentGrid.IsEnabled = false;
                worker.RunWorkerAsync(gameName);

                while (loading)
                {
                    Loader.Label(loadingDotsLabel);
                }
            }
        }

        string GetResponseString(HttpResponseMessage response)
        {
            var responseContent = response.Content;

            string responseString = responseContent.ReadAsStringAsync().Result;

            return responseString;
        }
        
        private void buttonFind_Click(object sender, RoutedEventArgs e)
        {
            FindGame();
        }

        private void textBoxGame_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key.Equals(Key.Enter))
                FindGame();
        }

        private void FindGame()
        {
            BackgroundWorker worker = new BackgroundWorker();

            worker.DoWork += (_sender, _e) =>
            {
                string game = (_e.Argument as string).Replace(" ", "%20");
                string url = "http://store.steampowered.com/search/?snr=1_4_4__12&term=" + game;

                HttpClient cl = new HttpClient();
                HttpResponseMessage response = cl.GetAsync(url).Result;

                string rs = GetResponseString(response);
                
                List<string> games = new List<string>();
                Thread.Sleep(2500);
                while (rs.IndexOf("<span class=\"title\">") >= 0)
                {
                    rs = rs.Substring(rs.IndexOf("<span class=\"title\">") + 20);
                    string title = rs.Substring(0, rs.IndexOf("<"));
                    games.Add(title);
                }
                _e.Result = games;
            };

            worker.RunWorkerCompleted += (_sender, _e) =>
            {
                loading = false;
                loaded = true;
                LoadingGrid.Visibility = Visibility.Hidden;
                MainContentGrid.IsEnabled = true;

                if ((_e.Result as List<string>).Count > 0)
                    listBoxGames.Items.Clear();

                foreach (string game in (_e.Result as List<string>))
                {
                    listBoxGames.Items.Add(game);
                }
            };

            loading = true;
            LoadingGrid.Visibility = Visibility.Visible;
            MainContentGrid.IsEnabled = false;
            worker.RunWorkerAsync(textBoxGame.Text);
            
            while (loading)
            {
                Loader.Label(loadingDotsLabel);
            }
            
        }

        private static SparqlResultSet Sparql(string game)
        {
            string query =
            "PREFIX wd: <http://www.wikidata.org/entity/>\n" +
            "PREFIX wdt: <http://www.wikidata.org/prop/direct/>\n" +
            "PREFIX wikibase: <http://wikiba.se/ontology#>\n" +
            "PREFIX p: <http://www.wikidata.org/prop/>\n" +
            "PREFIX v: <http://www.wikidata.org/prop/statement/>\n" +
            "PREFIX q: <http://www.wikidata.org/prop/qualifier/>\n" +
            "PREFIX rdfs: <http://www.w3.org/2000/01/rdf-schema#>\n" +

            "SELECT ?game ?publisher ?gameModes ?offSite ?date\n" +
            "WHERE\n" +
            "{\n" +
	            "?item wdt:P31 wd:Q7889 ;\n" +
                      "wdt:P404/rdfs:label ?gameModes;\n" +
                      "wdt:P577 ?date;\n" +
                      "rdfs:label ?game;\n" +
                      "wdt:P856 ?offSite;\n" +
                      "wdt:P123/rdfs:label ?publisher filter ( (lang(?publisher) = 'en') && (lang(?game) = 'en') && (lang(?gameModes) = 'en')).\n" +
                "FILTER(STRSTARTS(?game, '" + game + "')) .\n" +
	            "SERVICE wikibase:label { bd:serviceParam wikibase:language \"en\" }\n" +
            "}";

            var ep = new SparqlRemoteEndpoint(new Uri(@"https://query.wikidata.org/bigdata/namespace/wdq/sparql"));

            var qs = new SparqlParameterizedString(query);

            return ep.QueryWithResultSet(qs.ToString());
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri)
             );
            e.Handled = true;
        } 
    }
}
