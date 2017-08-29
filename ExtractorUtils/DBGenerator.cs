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
                    DbName = propdef.Attribute("db_name").Value,
                    OctgnName = propdef.Attribute("octgn_name").Value,
                    Type = (PropertyTypes) Enum.Parse(typeof (PropertyTypes),
                        propdef.Attribute("type") == null ? "String" : propdef.Attribute("type").Value)
                };
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
                    var value = jcard.Value<string>(prop.DbName);
                    if (value != null)
                    {
                        if (prop.Type == PropertyTypes.Bool)
                            value = (value == "True") ? "Yes" : "No";
                        else if (prop.Type == PropertyTypes.Rich)
                        {
                            value = MakeXMLSafe(value);
                            foreach (var symbol in Symbols)
                            {
                                value = value.Replace(symbol.Match, string.Format("<s value=\"{0}\">{1}</s>", symbol.Id, symbol.Name));
                            }
                        }
                        else
                            value = MakeXMLSafe(value);
                        card.Properties.Add(prop, value);
                    }
                }

                #region agotstuff
                //TODO: Make this a generic type

                var iconsProp = new Property()
                {
                    OctgnName = "Icons",
                    Type = PropertyTypes.String
                };
                string iconsValue = "";
                if (jcard.Value<string>("is_military") == "True")
                    iconsValue += "[military]";
                if (jcard.Value<string>("is_intrigue") == "True")
                    iconsValue += "[intrigue]";
                if (jcard.Value<string>("is_power") == "True")
                    iconsValue += "[power]";
                if (iconsValue != "")
                    card.Properties.Add(iconsProp, MakeXMLSafe(iconsValue));
                #endregion

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
            return (makeSafe);
        }
    }
}
