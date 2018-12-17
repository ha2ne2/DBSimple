using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Util
{
    public static class CommonUtil
    {

        /// <summary>
        /// 関数を引数にとり、その関数をメモ化した関数を返す関数。
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="fn"></param>
        /// <returns></returns>
        public static Func<T, T2, TResult> Memoize<T,T2,TResult>(Func<T,T2,TResult> fn)
        {
            Dictionary<Tuple<T, T2>, TResult> cache = new Dictionary<Tuple<T, T2>, TResult>();
            return (T t, T2 t2) => {
                TResult result;
                var tuple = new Tuple<T, T2>(t, t2);
                if (!cache.TryGetValue(tuple, out result))
                {
                    result = fn(t, t2);
                    cache[tuple] = result;
                }
                return result;
            };
        }


        public static void MeasureTime(string title, string body, int indent, Action fn)
        {
            MethodBase caller = new StackTrace().GetFrame(2).GetMethod();
            string callerClassName = caller.ReflectedType.Name;
            string callerName = callerClassName + "." + caller.Name;

            var sw = new Stopwatch();
            sw.Start();
            fn();
            sw.Stop();

            Debug.WriteLine(string.Format(
                "{0}[{1,-10}] [{2,-30}] ({3,3}ms) {4}",
                new string(' ', indent*2),
                title,
                callerName,
                sw.ElapsedMilliseconds,
                body));
        }

        public static T MeasureTime<T>(string title, string body, int indent, Func<T> fn)
        {
            MethodBase caller = new StackTrace().GetFrame(2).GetMethod();
            string callerClassName = caller.ReflectedType.Name;
            string callerName = callerClassName + "." + caller.Name;

            var sw = new Stopwatch();
            sw.Start();
            var result = fn();
            sw.Stop();

            Debug.WriteLine(string.Format(
                "{0}[{1,-10}] [{2,-30}] ({3,3}ms) {4}",
                new string(' ', indent * 2),
                title,
                callerName,
                sw.ElapsedMilliseconds,
                body));

            return result;
        }

        private static Func<T> Measurize<T>(Func<T> fn)
        {
            return () =>
            {
                var sw = new Stopwatch();
                sw.Start();
                var result = fn();
                sw.Stop();
                Console.WriteLine(string.Format("{0} : {1}", fn.ToString().PadRight(20), sw.Elapsed));
                return result;
            };
        }
    }
}
