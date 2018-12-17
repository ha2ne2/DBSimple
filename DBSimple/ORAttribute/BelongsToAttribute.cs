using System;
using System.Reflection;
using Ha2ne2.DBSimple.Util;

namespace Ha2ne2.DBSimple
{
    public class BelongsToAttribute : ORAttribute
    {
        public Type ParentType { get; protected set; }
        public string ParentKey { get; protected set; }
        public string ForeignKey { get; protected set; }
        public PropertyInfo Property { get; internal protected set; }

        private Lazy<Tuple<PropertyInfo, HasManyAttribute>> InversePropAndAttr;

        public string InverseHasManyPropertyName { get => InversePropAndAttr.Value?.Item1.Name; }
        public PropertyInfo InverseHasManyProperty { get => InversePropAndAttr.Value?.Item1; }
        public HasManyAttribute InverseHasManyAttribute { get => InversePropAndAttr.Value?.Item2; }

        public BelongsToAttribute(Type parentType, string foreignKey, string parentKey = "")
        {
            ParentType = parentType;
            ForeignKey = foreignKey;
            ParentKey = parentKey;

            InversePropAndAttr = new Lazy<Tuple<PropertyInfo, HasManyAttribute>>(() =>
                PropertyUtil.FindHasManyProperty(ParentType, ForeignKey));
        }
    }
}
