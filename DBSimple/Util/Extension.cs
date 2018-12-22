using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Util
{
    public static class Extension
    {
        /// <summary>
        /// 文字列内の連続した空白や改行文字を1つの空白に置換します。
        /// </summary>
        public static string Shrink(this string input)
        {
            int len = input.Length,
                index = 0,
                i = 0;
            var src = input.ToCharArray();
            bool skip = false;
            char ch;
            for (; i < len; i++)
            {
                ch = src[i];
                switch (ch)
                {
                case '\r':
                case '\n':
                    if (skip) continue;
                    src[index++] = ' ';
                    skip = true;
                    continue;
                case '\u0020':
                case '\u00A0':
                case '\u1680':
                case '\u2000':
                case '\u2001':
                case '\u2002':
                case '\u2003':
                case '\u2004':
                case '\u2005':
                case '\u2006':
                case '\u2007':
                case '\u2008':
                case '\u2009':
                case '\u200A':
                case '\u202F':
                case '\u205F':
                case '\u3000':
                case '\u2028':
                case '\u2029':
                case '\u0009':
                case '\u000B':
                case '\u000C':
                case '\u0085':
                    if (skip) continue;
                    src[index++] = ch;
                    skip = true;
                    continue;
                default:
                    skip = false;
                    src[index++] = ch;
                    continue;
                }
            }

            return new string(src, 0, index);
        }

        public static string JoinToString<T>(this IEnumerable<T> source, string separator)
        {
            return string.Join(separator, source);
        }

        public static bool IsEmpty<T>(this IEnumerable<T> source)
        {
            return source == null || !source.Any();
        }

        public static bool IsNotEmpty<T>(this IEnumerable<T> source)
        {
            return source != null && source.Any();
        }

        public static bool IsEmpty(this string source)
        {
            return string.IsNullOrEmpty(source);
        }

        public static bool IsNotEmpty(this string source)
        {
            return !string.IsNullOrEmpty(source);
        }
    }
}
