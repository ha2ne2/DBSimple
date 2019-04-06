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
        internal ConcurrentDictionary<string, Lazy<object>> LazyLoaderDict
        {
            get;
            private set;
        } = new ConcurrentDictionary<string, Lazy<object>>();

        protected T Get<T>([CallerMemberName] string propName = "")
        {
            Lazy<object> obj = null;
            if (LazyLoaderDict.TryGetValue(propName, out obj))
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
            LazyLoaderDict[propName] = new Lazy<object>(() => model);
        }
    }
}
