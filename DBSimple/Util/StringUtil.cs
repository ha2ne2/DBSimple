using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Util
{
    public static class StringUtil
    {
        /// <summary>
        /// 引数の文字列の内、Nullか空じゃない最初の文字列を返します。
        /// </summary>
        /// <param name="strings"></param>
        /// <returns></returns>
        public static string EmptyOr(params string[] strings)
        {
            foreach (string str in strings)
            {
                if (str.IsNotEmpty())
                    return str;
            }

            return string.Empty;
        }
    }
}
