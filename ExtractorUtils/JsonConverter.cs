using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExtractorUtils;

namespace ExtractorUtils
{
    public class JsonConverter
    {
        public static JArray ConvertCardJson(JObject jsonData)
        {
            var jarray = new JArray();

            foreach (var jcard in jsonData["records"])
            {
                foreach (var juniqueprops in jcard["pack_cards"])
                {
                    var card = new JObject();
                    foreach (JProperty prop in jcard)
                    {
                        if (prop.Name != "pack_cards")
                        {
                            card.Add(prop);
                        }
                    }
                    foreach (JProperty prop in juniqueprops)
                    {
                        if (prop.Name == "pack")
                        {
                            card.Add("pack_code", prop.Value.Value<string>("id"));
                        }
                        else
                        {
                            card.Add(prop);
                        }
                    }
                    jarray.Add(card);
                }
            }

            return jarray;
        }
        
    }
}
