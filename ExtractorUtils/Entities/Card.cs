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
        public Guid Id { get; set; }
        public string Size { get; set; }
        public string Pack { get; set; }
        public Set Set { get; set; }
        public string DbImageUrl { get; set; }
        public string CgImageUrl { get; set; }
        public Dictionary<Property, string> Properties { get; set; }

        public Card()
        {
            Properties = new Dictionary<Property, string>();
        }

        public string GetProperty(string propName)
        {
            return Properties.First(x => x.Key.OctgnName == propName).Value;
        }
    }
}
