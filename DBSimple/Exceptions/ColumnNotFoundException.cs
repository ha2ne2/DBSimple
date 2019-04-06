using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Exceptions
{
    public class ColumnNotFoundException : Exception
    {
        public ColumnNotFoundException()
        {
        }

        public ColumnNotFoundException(string message)
            : base(message)
        {
        }

        public ColumnNotFoundException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }
}
