using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple
{
    public class DBSimpleModel
    {
        public ConcurrentDictionary<string, Lazy<object>> Dict
        {
            get;
            private set;
        } = new ConcurrentDictionary<string, Lazy<object>>();

        protected T Get<T>([CallerMemberName] string propName = "")
        {
            Lazy<object> obj = null;
            if (Dict.TryGetValue(propName, out obj))
            {
                return (T)obj.Value;
            }
            else
            {
                return default(T);
            }
        }

        protected void Set<T>(T model, [CallerMemberName] string propName = "")
        {
            Dict[propName] = new Lazy<object>(() => model);
        }
    }
}
