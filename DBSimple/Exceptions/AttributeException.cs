using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Exceptions
{
    public class AttributeException : Exception
    {
        public AttributeException()
        {
        }

        public AttributeException(string message)
            : base(message)
        {
        }

        public AttributeException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
