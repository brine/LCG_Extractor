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
        public string Id { get; set; }
        public string dbCode { get; set; }
        public string cgCode { get; set; }

        public List<Card> Cards { get; set; }

        public Set()
        {
            Cards = new List<Card>();
        }
    }
}
