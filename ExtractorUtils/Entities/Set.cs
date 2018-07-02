using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractorUtils.Entities
{
    public class Set
    {
        public string Name { get; set; }
        public string Guid { get; set; }
        public string SetCode { get; set; }
        public string SetNumber { get; set; }
        public bool Included { get; set; }

        public List<Card> Cards { get; set; }

        public Set()
        {
            Cards = new List<Card>();
        }
    }
}
