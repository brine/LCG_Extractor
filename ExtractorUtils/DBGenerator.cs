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

namespace ExtractorUtils
{
    public class DBGenerator
    {
        public List<Card> cardList = new List<Card>();
        public List<Set> setList = new List<Set>();
        public List<Property> CardProperties = new List<Property>();
        public List<Size> CardSizes = new List<Size>();
        public List<Symbol> Symbols = new List<Symbol>();
        public string dbImageUrl;
        public string cgImageUrl;

        public DBGenerator()
        {
            var doc = XDocument.Parse(Properties.Resources.config);
            dbImageUrl = doc.Document.Descendants("dbImageUrl").First().Attribute("value").Value;
            cgImageUrl = doc.Document.Descendants("cgImageUrl").First().Attribute("value").Value;
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
            JArray jsonCards = (JArray)JsonConvert.DeserializeObject(
                new WebClient().DownloadString(
                    doc.Document.Descendants("cardsUrl").First().Attribute("value").Value));

            foreach (var jcard in jsonCards)
            {
                var card = new Card()
                {
                    Name = jcard.Value<string>("name"),
                    Pack = jcard.Value<string>("pack_code"),
                    DbImageUrl = jcard.Value<string>("code"),
                    CgImageUrl = jcard.Value<string>("position"),
                    Id = jcard.Value<string>("octgn_id") == null ? Guid.NewGuid() : Guid.Parse(jcard.Value<string>("octgn_id"))
                };
                foreach (var prop in CardProperties)
                {
                    var propertyValue = new StringBuilder();
                    foreach (var run in prop.Run)
                    {
                        if (run.Type == PropertyTypes.STRING)
                        {
                            var value = MakeXMLSafe(run.Value);
                            propertyValue.Append(value);
                        }
                        else
                        {
                            var value = jcard.Value<string>(run.Value);
                            if (value != null)
                            {
                                value = MakeXMLSafe(value);
                                foreach (var replace in run.Replace)
                                {
                                    value = value.Replace(replace.Key, replace.Value);
                                }
                                if (prop.IsRich)
                                {
                                    foreach (var symbol in Symbols)
                                    {
                                        value = value.Replace(symbol.Match, string.Format("<s value=\"{0}\">{1}</s>", symbol.Id, symbol.Name));
                                    }
                                }
                                propertyValue.Append(value);
                            }
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(propertyValue.ToString()))
                        card.Properties.Add(prop, propertyValue.ToString());
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
            JArray jsonPacks = (JArray)JsonConvert.DeserializeObject(
                new WebClient().DownloadString(
                    doc.Document.Descendants("packsUrl").First().Attribute("value").Value));

            var setGuidTable = XDocument.Parse(Properties.Resources.setguids);
            foreach (var jset in jsonPacks)
            {
                //  if (jset.Value<string>("available") == "") continue;
                var setConfig = setGuidTable.Descendants("cycle")
                        .First(x => x.Attribute("value").Value == jset.Value<string>("cycle_position"))
                        .Descendants("set")
                        .First(x => x.Attribute("name").Value == jset.Value<string>("position"));
                var set = new Set()
                {
                    Id = setConfig.Attribute("value").Value,
                    Name = jset.Value<string>("name"),
                    dbCode = jset.Value<string>("code"),
                    cgCode = "GT" + setConfig.Attribute("cgdb_id").Value,
                };
                set.Cards = new List<Card>(cardList.Where(x => x.Pack == set.dbCode));
                foreach (var card in set.Cards)
                {
                    card.Set = set;
                }
                setList.Add(set);
            }
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
