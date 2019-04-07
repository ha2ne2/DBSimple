using System;
using System.Linq;
using System.Reflection;
using Ha2ne2.DBSimple.Util;

namespace Ha2ne2.DBSimple
{
    public class HasManyAttribute : ORAttribute
    {
        private Lazy<Tuple<PropertyInfo, BelongsToAttribute>> InversePropAndAttr;

        public string InverseBelongsToPropertyName { get { return InversePropAndAttr.Value?.Item1.Name; } }
        public PropertyInfo InverseBelongsToProperty { get { return InversePropAndAttr.Value?.Item1; } }
        public BelongsToAttribute InverseBelongsToAttribute { get { return InversePropAndAttr.Value?.Item2; } }

        /// <summary>
        /// HasManyAttribute
        /// </summary>
        /// <param name="type">参照先のクラス</param>
        /// <param name="foreignKey">外部キーを格納するプロパティ名</param>
        /// <param name="referenceKey">通常外部キーは他テーブルの主キーとリンクするが、主キーでない列とリンクさせたい時にこの引数を指定する</param>
        public HasManyAttribute(Type type, string foreignKey, string referenceKey = "")
        {
            ReferenceType = type;
            ForeignKey = foreignKey;
            ReferenceKey = referenceKey;

            InversePropAndAttr = new Lazy<Tuple<PropertyInfo, BelongsToAttribute>>(() =>
                PropertyUtil.FindBelongsToProperty(ReferenceType, MyType, ForeignKey));
        }        
    }
}