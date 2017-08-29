using Newtonsoft.Json.Linq;
using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew.Entities;
using Octgn.Library;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using ExtractorUtils;

namespace ImageFetcherPlugin
{
    /// <summary>
    /// Interaction logic for ImageFetcherWindow.xaml
    /// </summary>
    public partial class ImageFetcherWindow : Window
    {
        private BackgroundWorker backgroundWorker = new BackgroundWorker();
        public IEnumerable<Card> cards;
        public DBGenerator database;

        public bool OverwriteBool = false;
        public bool UseThronesDb = true;

        public ImageFetcherWindow()
        {
            this.InitializeComponent();
            this.Closing += CancelWorkers;
            backgroundWorker.WorkerReportsProgress = true;
            backgroundWorker.WorkerSupportsCancellation = true;
            backgroundWorker.DoWork += DoWork;
            backgroundWorker.ProgressChanged += ProgressChanged;
            backgroundWorker.RunWorkerCompleted += BackgroundWorker_RunWorkerCompleted;

        }

        private void Generate(object sender, RoutedEventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                CurrentCard.Text = "Busy";
                return;
            }
            ProgressBar.Maximum = cards.Count();
            backgroundWorker.RunWorkerAsync();
        }


        void DoWork(object sender, DoWorkEventArgs e)
        {
            var i = 0;

            foreach (var card in cards)
            {
                if (backgroundWorker.CancellationPending) break;
                i++;
                var dbcard = database.cardList.FirstOrDefault(x => x.Id == card.Id);
                if (dbcard == null) continue;

                var cardset = card.GetSet();
                var garbage = Config.Instance.Paths.GraveyardPath;
                if (!Directory.Exists(garbage)) Directory.CreateDirectory(garbage);

                var imageUri = card.GetImageUri();

                var files =
                    Directory.GetFiles(cardset.ImagePackUri, imageUri + ".*")
                        .Where(x => System.IO.Path.GetFileNameWithoutExtension(x).Equals(imageUri, StringComparison.InvariantCultureIgnoreCase))
                        .OrderBy(x => x.Length)
                        .ToArray();
                
                if (files.Length > 0 && OverwriteBool == false)
                {
                    //skip overwrite if a saved image was located and overwrite is set to false 
                    backgroundWorker.ReportProgress(i, card);
                    continue;
                }
                
                foreach (var f in files.Select(x => new FileInfo(x)))
                {
                    f.MoveTo(System.IO.Path.Combine(garbage, f.Name));
                }
                
                var url = "";
                var newPath = "";
                

                if (UseThronesDb) // thronesdb
                {
                    url = database.dbImageUrl + dbcard.DbImageUrl + ".png";
                    newPath = System.IO.Path.Combine(cardset.ImagePackUri, imageUri + ".png");
                }
                else
                {
                    url = database.cgImageUrl + dbcard.Set.cgCode + "_" + dbcard.CgImageUrl + ".jpg";
                    newPath = System.IO.Path.Combine(cardset.ImagePackUri, imageUri + ".jpg");
                }
                

                using (WebClient webClient = new WebClient())
                {
                    try
                    {
                        webClient.DownloadFile(new Uri(url), newPath);
                    }
                    catch
                    {

                    }
                    
                }
                backgroundWorker.ReportProgress(i, card);
            }
        }

        private void ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            ProgressBar.Value = e.ProgressPercentage;
            CurrentCard.Text = (e.UserState as Card).Name;
            Stream imageStream = File.OpenRead((e.UserState as Card).GetPicture());

            var ret = new BitmapImage();
            ret.BeginInit();
            ret.CacheOption = BitmapCacheOption.OnLoad;
            ret.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            ret.StreamSource = imageStream;
            ret.EndInit();
            imageStream.Close();

            dbImage.Source = ret;

        }

        private void BackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            CurrentCard.Text = "DONE";
        }

        private void CancelWorkers(object sender, EventArgs e)
        {
            if (backgroundWorker.IsBusy)
            {
                CurrentCard.Text = "Cancel";
                backgroundWorker.CancelAsync();
            }
        }

        private void Overwrite(object sender, RoutedEventArgs e)
        {
            OverwriteBool = (sender as CheckBox).IsChecked ?? false;
        }

        private void DatabaseSelector(object sender, RoutedEventArgs e)
        {
            UseThronesDb = (sender as ComboBox).SelectedIndex == 0;
        }
    }
}
