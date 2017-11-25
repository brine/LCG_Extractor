using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractorUtils.Entities
{
    public enum PropertyTypes
    {
        STRING,
        PROPERTY
    }

    public class Run
    {
        public string Value { get; set; }
        public Dictionary<string, string> Replace { get; set; }
        public PropertyTypes Type { get; set; }
    }
    
    public class Property
    {
        public bool IsRich { get; set; }
        public string OctgnName { get; set; }
        public List<Run> Run { get; set; }

        public Property()
        { }
    }
}
