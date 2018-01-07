using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Net;
using System.Xml;
using System.IO;
using System.Data;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Windows.Controls;
using System.Windows.Data;
using ExtractorUtils;
using ExtractorUtils.Entities;
using Octgn.DataNew;
using Octgn.DataNew.Entities;
using Set = ExtractorUtils.Entities.Set;
using System.Diagnostics;

namespace DBExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public List<DBGenerator> databases;

        public DBGenerator database;


        public MainWindow()
        {
            this.InitializeComponent();
            databases = new List<DBGenerator>();
            var games = DbContext.Get().Games.Where(x => DBGenerator.ValidGame(x));
            GamesList.ItemsSource = games;

        }

        private void GameSelector(object sender, SelectionChangedEventArgs e)
        {
            var game = (Game)(sender as ComboBox).SelectedItem;
            database = databases.FirstOrDefault(x => x.gameGuid == game.Id);
            if (database == null)
            {
                database = new DBGenerator(game);
                databases.Add(database);
            };

            SetsPanel.ItemsSource = database.setList;
        }

        private void UpdateDataGrid(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (e.NewValue is Set)
                OutputGrid.ItemsSource = ((Set)e.NewValue).Cards;
        }

        private void UpdateAllXml(object sender, RoutedEventArgs e)
        {
            foreach (var set in database.setList)
            {
                SaveXml(set);
            }
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string saveDir = Path.Combine(dir, "Saved", database.gameGuid.ToString());

            Process.Start(saveDir);
        }

        private void UpdateXml(object sender, RoutedEventArgs e)
        {
            if (SetsPanel.SelectedItem == null) return;
            SaveXml((Set)SetsPanel.SelectedItem);

            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string saveDir = Path.Combine(dir, "Saved", database.gameGuid.ToString(), (SetsPanel.SelectedItem as Set).Guid);

            Process.Start(saveDir);
        }

        private void SaveXml(ExtractorUtils.Entities.Set set)
        {
            if (set == null) return;
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string saveDir = Path.Combine(dir, "Saved", database.gameGuid.ToString(), set.Guid);
            string savePath = Path.Combine(saveDir, "set.xml");
            if (!Directory.Exists(saveDir))
            {
                Directory.CreateDirectory(saveDir);
            }
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }

            var xml = new XmlDocument();
            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "utf-8", "yes"));

            XmlNode root = xml.CreateElement("set");
            root.Attributes.Append(CreateAttribute(xml, "name", set.Name));
            root.Attributes.Append(CreateAttribute(xml, "id", set.Guid));
            root.Attributes.Append(CreateAttribute(xml, "gameId", database.gameGuid.ToString()));
            root.Attributes.Append(CreateAttribute(xml, "version", "1.0.0.0"));
            root.Attributes.Append(CreateAttribute(xml, "gameVersion", "1.0.0.0"));
            xml.AppendChild(root);

            XmlNode cardsNode = xml.CreateElement("cards");
            root.AppendChild(cardsNode);


            foreach (var c in set.Cards.OrderBy(x => x.Position, new AlphanumComparatorFast()))
            {
                XmlNode cardNode = xml.CreateElement("card");
                cardNode.Attributes.Append(CreateAttribute(xml, "name", c.Name));
                cardNode.Attributes.Append(CreateAttribute(xml, "id", c.Id));
                if (c.Size != null)
                {
                    cardNode.Attributes.Append(CreateAttribute(xml, "size", c.Size));
                }

                foreach (KeyValuePair<Property, string> kvi in c.Properties)
                {
                    XmlNode prop = xml.CreateElement("property");
                    prop.Attributes.Append(CreateAttribute(xml, "name", kvi.Key.OctgnName));
                    if (kvi.Key.IsRich)
                    {
                        var propdoc = new XmlDocument();
                        propdoc.LoadXml("<root>" + kvi.Value + "</root>");
                        foreach (XmlNode node in propdoc.FirstChild.ChildNodes)
                        {
                            prop.AppendChild(xml.ImportNode(node, true));
                        }
                    }
                    else
                    {
                        prop.Attributes.Append(CreateAttribute(xml, "value", kvi.Value));
                    }
                    cardNode.AppendChild(prop);
                }
                cardsNode.AppendChild(cardNode);
            }

            xml.Save(savePath);
        }

        private XmlAttribute CreateAttribute(XmlDocument doc, string name, string value)
        {
            XmlAttribute ret = doc.CreateAttribute(name);
            ret.Value = value;
            return (ret);
        }

    }
}
