using ExtractorUtils;
using ImageFetcherPlugin;
using Octgn.DataNew;
using Octgn.DataNew.Entities;
using Octgn.Library;
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

namespace ImageFetcherPluginConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            Config.Instance = new Config();
            var games = DbContext.Get().Games.Where(x => DBGenerator.ValidGame(x));
            GamesList.ItemsSource = games;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (GamesList.SelectedItem is Game game)
            {
                
                ImageFetcherWindow imageFetcherWindow = new ImageFetcherWindow(game);
                imageFetcherWindow.ShowDialog();
            }
            
        }
    }
}
