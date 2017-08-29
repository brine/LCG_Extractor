using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractorUtils.Entities
{
    public class Size
    {
        public string Name { get; set; }
        public List<Tuple<string, string>> Match { get; set; }
        
        public Size()
        {
            Match = new List<Tuple<string, string>>();
        }
    }
}
