﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using ExtractorUtils.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Globalization;
using Octgn.Core.DataExtensionMethods;
using Game = Octgn.DataNew.Entities.Game;
using System.IO;

namespace ExtractorUtils
{
    public class DBGenerator
    {
        public XDocument doc;
        public XDocument setGuidTable;
        public List<Card> cardList = new List<Card>();
        public List<Set> setList = new List<Set>();
        public List<Property> CardProperties = new List<Property>();
        public List<Size> CardSizes = new List<Size>();
        public List<Symbol> Symbols = new List<Symbol>();
        public List<ImageSource> ImageSources = new List<ImageSource>();
        public Guid gameGuid;

        public Octgn.DataNew.Entities.PropertyDef CardNumberProperty;

        public string cardNumberField;
        public string cardNameField;
        public string packIdField;
        public string cardPackIdField;
        public string cardIdField;
        public string cardImageField;

        private Game _game;
        public DBGenerator(Game game)
        {
            _game = game;
            var directory = Path.Combine(game.InstallPath, "Extractor");
            if (!Directory.Exists(directory)) return;
            
            doc = XDocument.Load(Path.Combine(directory, "config.xml"));
            setGuidTable = XDocument.Load(Path.Combine(directory, "setguids.xml"));
                        

            var gameData = doc.Document.Descendants("game").First();

            gameGuid = game.Id;
            CardNumberProperty = game.AllProperties().First(x => x.Name == gameData.Attribute("cardOctgnNumber").Value);
            cardNumberField = gameData.Attribute("cardNumber").Value;
            cardNameField = gameData.Attribute("cardName").Value;
            packIdField = gameData.Attribute("packId").Value;
            cardPackIdField = gameData.Attribute("cardPackId").Value;
            cardIdField = gameData.Attribute("cardId").Value;
            cardImageField = gameData.Attribute("cardImage").Value;

            // load image sources
            foreach (var imgdef in doc.Descendants("image"))
            {
                var imgsrc = new ImageSource()
                {
                    Name = imgdef.Attribute("name").Value,
                    Url = imgdef.Attribute("url").Value
                };
                ImageSources.Add(imgsrc);
            }

            // load card properties
            foreach (var propdef in doc.Descendants("property"))
            {
                var prop = new Property()
                {
                    OctgnName = propdef.Attribute("octgn_name").Value,
                    Run = new List<Run>(),
                    IsRich = propdef.Attribute("isRich") == null ? false : bool.Parse(propdef.Attribute("isRich").Value)
                };

                var items = new List<XElement>();
                items.Add(propdef);
                items.AddRange(propdef.Descendants("run"));

                foreach (var runitem in items)
                {
                    if (runitem.Attribute("value") == null) continue;

                    var run = new Run()
                    {
                        Value = runitem.Attribute("value").Value,
                        Capitalize = runitem.Attribute("capitalize") == null ? false : bool.Parse(runitem.Attribute("capitalize").Value),
                        Format = runitem.Attribute("format") == null ? "{0}" : runitem.Attribute("format").Value,
                        Delimiter = runitem.Attribute("delimiter") == null ? null : runitem.Attribute("delimiter").Value,
                        Replace = new Dictionary<string, string>(),
                        Type = (PropertyTypes) Enum.Parse(typeof (PropertyTypes),
                            runitem.Attribute("type") == null ? "STRING" : runitem.Attribute("type").Value.ToUpper())
                    };
                    foreach (var replace in runitem.Descendants("replace"))
                    {
                        run.Replace.Add(replace.Attribute("match").Value, replace.Attribute("value").Value);
                    }
                    prop.Run.Add(run);
                }
                
                CardProperties.Add(prop);
            }
            if (doc.Descendants("size") != null)
            {
                foreach (var sizedef in doc.Descendants("size"))
                {
                    var size = new Size()
                    {
                        Name = sizedef.Attribute("name").Value
                    };
                    foreach (var match in sizedef.Descendants("match"))
                    {
                        size.Match.Add(new Tuple<string, string>(match.Attribute("property").Value, match.Attribute("value").Value));
                    };
                    CardSizes.Add(size);
                }
            }
            if (doc.Descendants("symbol") != null)
            {
                foreach (var symboldef in doc.Descendants("symbol"))
                {
                    var symbol = new Symbol()
                    {
                        Name = symboldef.Attribute("name").Value,
                        Id = symboldef.Attribute("id").Value,
                        Match = symboldef.Attribute("match").Value
                    };
                    Symbols.Add(symbol);
                }
            }
            // load cards

            JArray jsonCards;
            using (var webclient = new WebClient() { Encoding = Encoding.UTF8 })
            {
                var jsonCardData = JsonConvert.DeserializeObject(webclient.DownloadString(gameData.Attribute("cardsUrl").Value));
                jsonCards = (jsonCardData is JArray) ? (JArray)jsonCardData : JsonConverter.ConvertCardJson(jsonCardData as JObject);
            };
                        
            foreach (var jcard in jsonCards)
            {
                var card = new Card()
                {
                    Name = jcard.Value<string>(cardNameField),
                    Pack = jcard.Value<string>(cardPackIdField),
                    Id = jcard.Value<string>(cardIdField),
                    Position = jcard.Value<string>(cardNumberField),
                    Image = jcard.Value<string>(cardImageField)
                };
                
                foreach (var prop in CardProperties)
                {
                    string value = "";
                    foreach (var run in prop.Run)
                    {
                        var valueList = new List<string>();
                        if (run.Type == PropertyTypes.STRING)
                        {
                            valueList.Add(run.Value);
                        }
                        else
                        {
                            var valuetoken = jcard.Value<object>(run.Value);
                            if (valuetoken == null) continue;
                            if (valuetoken is JArray)
                                valueList.AddRange((valuetoken as JArray).Select(x => ProcessPropertyValue(x.ToString(), prop, run)));
                            else
                                valueList.Add(ProcessPropertyValue(valuetoken.ToString(), prop, run));
                        }
                        value += String.Join(run.Delimiter, valueList.Where(x => !string.IsNullOrWhiteSpace(x)));

                    }

                    if (!string.IsNullOrWhiteSpace(value))
                        card.Properties.Add(prop, value);
                }
                
                foreach (var size in CardSizes)
                {
                    bool isMatch = true;
                    foreach (var match in size.Match)
                    {
                        var prop = card.Properties.FirstOrDefault(x => x.Key.OctgnName == match.Item1);
                        if (prop.Value != match.Item2)
                        {
                            isMatch = false;
                            break;
                        }
                    }
                    if (isMatch)
                    {
                        card.Size = size.Name;
                        break;
                    }
                }
                cardList.Add(card);
            }

            // load sets

            JArray jsonPacks;
            using (var webclient = new WebClient() { Encoding = Encoding.UTF8 })
            {
                var jsonPackData = JsonConvert.DeserializeObject(webclient.DownloadString(gameData.Attribute("packsUrl").Value));
                jsonPacks = (jsonPackData is JArray) ? (JArray)jsonPackData : ((JObject)jsonPackData).Descendants().First(x => x is JArray) as JArray;
            };
            
            foreach (var jset in jsonPacks)
            {
                //  if (jset.Value<string>("available") == "") continue;
                var setConfig = setGuidTable
                            .Descendants("set")
                            .FirstOrDefault(x => x.Attribute("id") != null && x.Attribute("id").Value == jset.Value<string>(packIdField));
                if (setConfig == null) continue;

                var cardGuidList = setConfig.Descendants("card");

                var set = new Set()
                {
                    Guid = setConfig.Attribute("value").Value,
                    Name = jset.Value<string>("name"),
                    SetCode = jset.Value<string>(packIdField),
                    SetNumber = setConfig.Attribute("number").Value,
                };
                set.Cards = new List<Card>(cardList.Where(x => x.Pack == set.SetCode));
                foreach (var card in set.Cards)
                {
                    card.Set = set;
                    if (card.Id == null)
                    {
                        var cardIdData = cardGuidList.FirstOrDefault(x => x.Attribute(cardNumberField) != null && x.Attribute(cardNumberField).Value == card.Position);
                        card.Id = (cardIdData == null) ? FindOctgnGuid(card) : cardIdData.Attribute(packIdField).Value;
                    }
                }
                setList.Add(set);
            }
        }

        private IEnumerable<Octgn.DataNew.Entities.Card> _cardsCache;
        public IEnumerable<Octgn.DataNew.Entities.Card> CardsCache
        {
            get
            {
                if (_cardsCache == null)
                    _cardsCache = _game.AllCards();
                return _cardsCache;
            }
        }
        private string FindOctgnGuid(Card card)
        {
            var octgnCard = CardsCache.FirstOrDefault(x => x.MatchesPropertyValue(CardNumberProperty, card.Position) && x.SetId.ToString() == card.Set.Guid);

            if (octgnCard != null)
            {
                return octgnCard.Id.ToString();
            }
            return Guid.NewGuid().ToString();
        }
    
        public string ProcessPropertyValue(string value, Property prop, Run run)
        {
            if (value != null)
            {
                value = string.Format(run.Format, MakeXMLSafe(value));
                foreach (var replace in run.Replace)
                {
                    value = value.Replace(replace.Key, replace.Value);
                }
                if (run.Capitalize)
                {
                    value = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value);
                }
                if (prop.IsRich)
                {
                    foreach (var symbol in Symbols)
                    {
                        value = value.Replace(symbol.Match, string.Format("<s value=\"{0}\">{1}</s>", symbol.Id, symbol.Name));
                    }
                }
            }
            return value;
        }


        public static bool ValidGame(Game g)
        {
            var directory = Path.Combine(g.InstallPath, "Extractor");
            if (!Directory.Exists(directory)) return false;

            var files = Directory.GetFiles(directory, "*.xml").Select(x => Path.GetFileName(x)).ToArray();

            if (!files.Contains("setguids.xml")) return false;
            if (!files.Contains("config.xml")) return false;

            return true;
        }

        public static string MakeXMLSafe(string makeSafe)
        {
            makeSafe = makeSafe.Replace("&", "&amp;");
            makeSafe = makeSafe.Replace("\n", "&#xd;&#xa;");
            makeSafe = makeSafe.Replace("<em>", "<i>");
            makeSafe = makeSafe.Replace("</em>", "</i>");
            return (makeSafe);
        }
    }
}
