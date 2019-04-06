using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple
{
    public class ORAttribute : Attribute
    {
        public PropertyInfo Property { get; internal protected set; }
    }
}
