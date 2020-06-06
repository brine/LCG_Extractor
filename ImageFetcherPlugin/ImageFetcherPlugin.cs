
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using Octgn.Core.DataExtensionMethods;
using Octgn.Core.DataManagers;
using Octgn.Core.Plugin;
using System.Net;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ExtractorUtils;
using System.Linq;

namespace ImageFetcherPlugin
{

    public class ImageFetcher : IDeckBuilderPlugin 
    {
        public IEnumerable<IPluginMenuItem> MenuItems
        {
            get
            {
                return new List<IPluginMenuItem>{new PluginMenuItem()};
            }
        }

        public void OnLoad(GameManager games)
        {
        }

        public Guid Id
        {
            get
            {
                // All plugins are required to have a unique GUID
                // http://www.guidgenerator.com/online-guid-generator.aspx
                return Guid.Parse("43b72a57-dc96-4298-a02e-68e4f311ad58");
            }
        }

        public string Name => "LCG Image Fetcher";

        public Version Version => Version.Parse("5.0.0.0");

        public Version RequiredByOctgnVersion => Version.Parse("3.4.273.0");
    }

    public class PluginMenuItem : IPluginMenuItem
    {
        public string Name => "LCG Image Fetcher";

        public void OnClick(IDeckBuilderPluginController con)
        {
            var game = con.GetLoadedGame();
            if (game == null || !DBGenerator.ValidGame(game))
            {
                MessageBox.Show("This game does not support LCG Image Fetcher");
                return;
            }

            ImageFetcherWindow mainWindow = new ImageFetcherWindow(game);
            mainWindow.ShowDialog();
        }
    }
}
