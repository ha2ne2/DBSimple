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
        /// <summary>
        /// この属性が付与されたプロパティ
        /// </summary>
        public PropertyInfo Property { get; internal protected set; }

        /// <summary>
        /// この属性が付与されたプロパティの属するクラス
        /// </summary>
        public Type MyType { get; internal protected set; }

        /// <summary>
        /// 参照先のクラス
        /// </summary>
        public Type ReferenceType { get; protected set; }

        /// <summary>
        /// 外部キーを格納するプロパティ名
        /// </summary>
        public string ForeignKey { get; protected set; }

        /// <summary>
        /// 外部キーの参照先のプロパティ名
        /// 通常外部キーは他テーブルの主キーとリンクするが、
        /// 主キーでない列とリンクさせたい時にこの引数を指定する
        /// </summary>
        public string ReferenceKey { get; protected set; }
    }
}
