
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
                // Add your menu items here.
                return new List<IPluginMenuItem>{new PluginMenuItem()};
            }
        }

        public void OnLoad(GameManager games)
        {
            // I'm showing a message box, but don't do this, unless it's for updates or something...but don't do it every time as it pisses people off.
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

        public string Name
        {
            get
            {
                // Display name of the plugin.
                return "LCG Image Fetcher";
            }
        }

        public Version Version
        {
            get
            {
                // Version of the plugin.
                // This code will pull the version from the assembly.
                return Assembly.GetCallingAssembly().GetName().Version;
            }
        }

        public Version RequiredByOctgnVersion
        {
            get
            {
                // Don't allow this plugin to be used in any version less than 3.0.12.58
                return Version.Parse("3.1.0.0");
            }
        }
    }

    public class PluginMenuItem : IPluginMenuItem
    {
        public string Name
        {
            get
            {
                return "LCG Image Fetcher";
            }
        }

        /// <summary>
        /// This happens when the menu item is clicked.
        /// </summary>
        /// <param name="con"></param>
        public void OnClick(IDeckBuilderPluginController con)
        {
            var game = con.GetLoadedGame();
            if (game == null || !DBGenerator.ValidGame(game))
            {
                MessageBox.Show("This game does not support LCG Image Fetcher");
                return;
            }

            ImageFetcherWindow mainWindow = new ImageFetcherWindow()
            {
                cards = game.AllCards(),
                database = new DBGenerator(game)
            };
            mainWindow.ShowDialog();
        }
    }
}
