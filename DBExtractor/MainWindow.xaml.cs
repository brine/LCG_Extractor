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

namespace DBExtractor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public DBGenerator database;

        public MainWindow()
        {
            this.InitializeComponent();
            database = new DBGenerator();
            
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
        }

        private void UpdateXml(object sender, RoutedEventArgs e)
        {
            if (SetsPanel.SelectedItem == null) return;
            SaveXml((Set)SetsPanel.SelectedItem);
        }

        private void SaveXml(Set set)
        {
            if (set == null) return;
            string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string saveDir = Path.Combine(dir, "Saved", set.Id);
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
            root.Attributes.Append(CreateAttribute(xml, "id", set.Id));
            root.Attributes.Append(CreateAttribute(xml, "gameId", "30c200c9-6c98-49a4-a293-106c06295c05"));
            root.Attributes.Append(CreateAttribute(xml, "version", "1.0.0.0"));
            root.Attributes.Append(CreateAttribute(xml, "gameVersion", "1.0.0.0"));
            xml.AppendChild(root);

            XmlNode cardsNode = xml.CreateElement("cards");
            root.AppendChild(cardsNode);


            foreach (var c in set.Cards)
            {
                XmlNode cardNode = xml.CreateElement("card");
                cardNode.Attributes.Append(CreateAttribute(xml, "name", c.Name));
                cardNode.Attributes.Append(CreateAttribute(xml, "id", c.Id.ToString()));
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
