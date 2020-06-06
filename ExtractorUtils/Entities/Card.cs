using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractorUtils.Entities
{
    public class Card
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public string Position { get; set; }
        public string Pack { get; set; }
        public string Image { get; set; }
        public string Size { get; set; }
        public Set Set { get; set; }
        public Dictionary<Property, string> Properties { get; set; }

        public Card()
        {
            Properties = new Dictionary<Property, string>();
        }

        public override string ToString()
        {
            return Name;
        }

        public string GetProperty(string propName)
        {
            return Properties.First(x => x.Key.OctgnName == propName).Value;
        }
    }
}
