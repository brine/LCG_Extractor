using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using ExtractorUtils.Entities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Globalization;
using Octgn.DataNew;
using Octgn.Core.DataExtensionMethods;

namespace ExtractorUtils
{
    public class DBGenerator
    {
        public XDocument doc;
        public List<Card> cardList = new List<Card>();
        public List<Set> setList = new List<Set>();
        public List<Property> CardProperties = new List<Property>();
        public List<Size> CardSizes = new List<Size>();
        public List<Symbol> Symbols = new List<Symbol>();
        public List<ImageSource> ImageSources = new List<ImageSource>();
        public Guid gameGuid;
        public string cardNumberField;
        public string octgnCardNumberField;
        public string cardNameField;
        public string packIdField;
        public string cardIdField;
        public string cardImageField;

        public DBGenerator()
        {
            doc = XDocument.Parse(Properties.Resources.config);
            var gameData = doc.Document.Descendants("game").First();
            
            gameGuid = Guid.Parse(gameData.Attribute("gameGuid").Value);
            octgnCardNumberField = gameData.Attribute("octgnCardNumber").Value;
            cardNumberField = gameData.Attribute("cardNumber").Value;
            cardNameField = gameData.Attribute("cardName").Value;
            packIdField = gameData.Attribute("packId").Value;
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
                    IsRich = propdef.Attribute("isRich") == null ? false : bool.Parse(propdef.Attribute("isRich").Value),
                    Capitalize = propdef.Attribute("capitalize") == null ? false : bool.Parse(propdef.Attribute("capitalize").Value),
                    Delimiter = propdef.Attribute("delimiter") == null ? null : propdef.Attribute("delimiter").Value 
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
                    Pack = jcard.Value<string>(packIdField),
                    Id = jcard.Value<string>(cardIdField),
                    Position = jcard.Value<string>(cardNumberField),
                    Image = jcard.Value<string>(cardImageField)
                };
                
                foreach (var prop in CardProperties)
                {
                    var valueList = new List<string>();
                    foreach (var run in prop.Run)
                    {
                        if (run.Type == PropertyTypes.STRING)
                        {
                            valueList.Add(ProcessPropertyValue(run.Value, prop, run));
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
                    }

                    var value = String.Join(prop.Delimiter, valueList.Where(x => !string.IsNullOrWhiteSpace(x)));
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
            
            var setGuidTable = XDocument.Parse(Properties.Resources.setguids);

            foreach (var jset in jsonPacks)
            {
                //  if (jset.Value<string>("available") == "") continue;
                var setConfig = setGuidTable
                            .Descendants("set")
                            .First(x =>
                            (x.Attribute("id") != null && x.Attribute("id").Value == jset.Value<string>("id"))
                            ||
                            (x.Attribute("position") != null && x.Attribute("position").Value == jset.Value<string>("position")
                            &&
                            x.Attribute("cycle") != null && x.Attribute("cycle").Value == jset.Value<string>("cycle_position")));

                var cardGuidList = setConfig.Descendants("card");

                var set = new Set()
                {
                    Id = setConfig.Attribute("value").Value,
                    Name = jset.Value<string>("name"),
                    dbCode = jset.Value<string>("code") ?? jset.Value<string>("id"),
                    cgCode = "GT" + setConfig.Attribute("cgdb_id").Value,
                };
                set.Cards = new List<Card>(cardList.Where(x => x.Pack == set.dbCode));
                foreach (var card in set.Cards)
                {
                    card.Set = set;
                    if (card.Id == null)
                    {
                        var cardIdData = cardGuidList.FirstOrDefault(x => x.Attribute("position") != null && x.Attribute("position").Value == card.Position);
                        card.Id = (cardIdData == null) ? FindOctgnGuid(card) : cardIdData.Attribute("id").Value;
                    }
                }
                setList.Add(set);
            }
        }
        
        private string FindOctgnGuid(Card card)
        {

            var octgnCard = DbContext.Get().GameById(gameGuid).AllCards()
                        .FirstOrDefault(x => x.Properties[""].Properties
                        .First(y => y.Key.Name == octgnCardNumberField).Value.ToString() == card.Position
                        && x.SetId.ToString() == card.Set.Id);
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
                value = MakeXMLSafe(value);
                foreach (var replace in run.Replace)
                {
                    value = value.Replace(replace.Key, replace.Value);
                }
                if (prop.Capitalize)
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
