using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExtractorUtils.Entities
{
    public enum PropertyTypes
    {
        Bool,
        Rich,
        String
    }

    public class Property
    {
        public string OctgnName { get; set; }
        public string DbName { get; set; }
        public PropertyTypes Type { get; set; }

        public Property()
        { }
    }
}
