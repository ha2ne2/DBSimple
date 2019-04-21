using System;
using System.Reflection;
using Ha2ne2.DBSimple.Util;

namespace Ha2ne2.DBSimple
{
    public class BelongsToAttribute : ORAttribute
    {
        private Lazy<Tuple<PropertyInfo, HasManyAttribute>> InversePropAndAttr;

        public string InverseHasManyPropertyName { get { return InversePropAndAttr.Value?.Item1.Name; } }
        public PropertyInfo InverseHasManyProperty { get { return InversePropAndAttr.Value?.Item1; } }
        public HasManyAttribute InverseHasManyAttribute { get { return InversePropAndAttr.Value?.Item2; } }

        /// <summary>
        /// BelongsToAttribute
        /// </summary>
        /// <param name="type">参照先のクラス</param>
        /// <param name="foreignKey">外部キーを格納するプロパティ名</param>
        /// <param name="referenceKey">通常外部キーは他テーブルの主キーとリンクするが、主キーでない列とリンクさせたい時にこの引数を指定する</param>
        public BelongsToAttribute(Type referenceType, string foreignKey, string referenceKey = "")
        {
            ReferenceType = referenceType;
            ForeignKey = foreignKey;
            ReferenceKey = referenceKey;

            InversePropAndAttr = new Lazy<Tuple<PropertyInfo, HasManyAttribute>>(() =>
                PropertyUtil.FindHasManyProperty(ReferenceType, MyType, ForeignKey));
        }
    }
}
