using Octgn.Core.DataExtensionMethods;
using Octgn.DataNew.Entities;
using Octgn.Library;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media.Imaging;
using ExtractorUtils;
using Octgn.DataNew;
using System.Threading;
using System.Threading.Tasks;

namespace ImageFetcherPlugin
{
    /// <summary>
    /// Interaction logic for ImageFetcherWindow.xaml
    /// </summary>
    public partial class ImageFetcherWindow : Window
    {

        private CancellationTokenSource _cts;

        public IEnumerable<Card> cards;
        public DBGenerator database;

        public bool OverwriteBool = false;
        public int SelectedItemSource = 0;

        public ImageFetcherWindow()
        {
            //var game = DbContext.Get().GameById(Guid.Parse("bb0f02e7-2a6f-4ae3-84a2-c501b4176844")) ?? throw new Exception("Legend of the Five Rings is not installed!");
            var game = DbContext.Get().GameById(Guid.Parse("30C200C9-6C98-49A4-A293-106C06295C05")) ?? throw new Exception("Game of Thrones is not installed!");
            database = new DBGenerator(game);
            cards = game.AllCards();
            Initialize();
        }

        public ImageFetcherWindow(Game game)
        {
            database = new DBGenerator(game);
            cards = game.AllCards();
            Initialize();
        }

        public void Initialize()
        {
            this.InitializeComponent();

            DbComboBox.ItemsSource = database.ImageSources;

            this.Closing += CancelWorkers;
        }

        private async void GenerateButtonClicked(object sender, RoutedEventArgs e)
        {

            _cts = new CancellationTokenSource();
            var token = _cts.Token;
            var progressHandler = new Progress<WorkerItem>(workerItem =>
            {
                ProgressChanged(workerItem);
            });
            var progress = progressHandler as IProgress<WorkerItem>;

            ProgressBar.Maximum = cards.Count();
            DbComboBox.IsEnabled = false;
            OverwriteCheckbox.IsEnabled = false;
            GenerateBox.Visibility = Visibility.Collapsed;
            CancelBox.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                var i = 0;
                foreach (var card in cards)
                {
                    var workerItem = new WorkerItem() { Card = card, progress = i++ };
                    if (token.IsCancellationRequested)
                        break;
                    DoWork(workerItem);
                    Thread.Sleep(10);
                    progress.Report(workerItem);
                }
            });
            CurrentCard.Text = "DONE";
            WorkerCompleted();
        }


        private void DoWork(WorkerItem worker)
        {
            var dbcard = database.cardList.FirstOrDefault(x => x.Id == worker.Card.Id.ToString());
            if (dbcard == null) return;

            var cardset = worker.Card.GetSet();

            var garbage = Config.Instance.Paths.GraveyardPath;
            if (!Directory.Exists(garbage)) Directory.CreateDirectory(garbage);

            var imageUri = worker.Card.GetImageUri();

            var files =
                Directory.GetFiles(cardset.ImagePackUri, imageUri + ".*")
                    .Where(x => System.IO.Path.GetFileNameWithoutExtension(x).Equals(imageUri, StringComparison.InvariantCultureIgnoreCase))
                    .OrderBy(x => x.Length)
                    .ToArray();

            if (files.Length > 0 && OverwriteBool == false)
            {
                return;
            }


            var url = "";
            var newPath = "";

            url = string.Format(database.ImageSources[SelectedItemSource].Url, dbcard.Image, dbcard.Position, dbcard.Name, dbcard.Id, dbcard.Set.SetNumber, dbcard.Set.SetCode);
            newPath = System.IO.Path.Combine(cardset.ImagePackUri, imageUri);

            using (WebClient webClient = new WebClient())
            {
                try
                {
                    byte[] fileBytes = webClient.DownloadData(url);

                    string fileType = webClient.ResponseHeaders[HttpResponseHeader.ContentType];

                    if (fileType != null)
                    {
                        switch (fileType)
                        {
                            case "image/jpeg":
                                newPath += ".jpg";
                                foreach (var f in files.Select(x => new FileInfo(x)))
                                    f.MoveTo(System.IO.Path.Combine(garbage, f.Name));
                                System.IO.File.WriteAllBytes(newPath, fileBytes);
                                break;
                            case "image/gif":
                                newPath += ".gif";
                                foreach (var f in files.Select(x => new FileInfo(x)))
                                    f.MoveTo(System.IO.Path.Combine(garbage, f.Name));
                                System.IO.File.WriteAllBytes(newPath, fileBytes);
                                break;
                            case "image/png":
                                newPath += ".png";
                                foreach (var f in files.Select(x => new FileInfo(x)))
                                    f.MoveTo(System.IO.Path.Combine(garbage, f.Name));
                                System.IO.File.WriteAllBytes(newPath, fileBytes);
                                break;
                            default:
                                break;
                        }
                    }
                }
                catch
                {

                }
            }
        }

        private void ProgressChanged(WorkerItem worker)
        {
            ProgressBar.Value = worker.progress;
            CurrentCard.Text = worker.Card.Name;
            Stream imageStream = File.OpenRead(worker.Card.GetPicture());
            imageStream.Position = 0;

            var ret = new BitmapImage();
            ret.BeginInit();
            ret.CacheOption = BitmapCacheOption.OnLoad;
            ret.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            ret.StreamSource = imageStream;
            ret.EndInit();
            ret.Freeze();
            imageStream.Close();

            dbImage.Source = ret;

        }

        private void CancelWorkers(object sender, EventArgs e)
        {
            if (_cts != null)
            {
                _cts.Cancel();
                CurrentCard.Text = "Cancel";
                WorkerCompleted();
            }
        }

        private void WorkerCompleted()
        {
            DbComboBox.IsEnabled = true;
            OverwriteCheckbox.IsEnabled = true;
            GenerateBox.Visibility = Visibility.Visible;
            CancelBox.Visibility = Visibility.Collapsed;
        }

        private void Overwrite(object sender, RoutedEventArgs e)
        {
            OverwriteBool = (sender as CheckBox).IsChecked ?? false;
        }

        private void DatabaseSelector(object sender, RoutedEventArgs e)
        {
            SelectedItemSource = (sender as ComboBox).SelectedIndex;
        }
    }

    public class WorkerItem
    {
        public int progress { get; set; }
        public Card Card { get; set; }
    }
}
