using System;
using System.Linq;
using System.Reflection;
using Ha2ne2.DBSimple.Util;

namespace Ha2ne2.DBSimple
{
    public class HasManyAttribute : ORAttribute
    {
        public Type ChildType { get; protected set; }
        public string ParentKey { get; protected set; }
        public string ForeignKey { get; protected set; }
        public PropertyInfo Property { get; internal protected set; }

        private Lazy<Tuple<PropertyInfo, BelongsToAttribute>> InversePropAndAttr;

        public string InverseBelongsToPropertyName { get { return InversePropAndAttr.Value?.Item1.Name; } }
        public PropertyInfo InverseBelongsToProperty { get { return InversePropAndAttr.Value?.Item1; } }
        public BelongsToAttribute InverseBelongsToAttribute { get { return InversePropAndAttr.Value?.Item2; } }

        public HasManyAttribute(Type childType, string foreignKey, string parentKey = "")
        {
            ChildType = childType;
            ParentKey = parentKey;
            ForeignKey = foreignKey;

            InversePropAndAttr = new Lazy<Tuple<PropertyInfo, BelongsToAttribute>>(() =>
                PropertyUtil.FindBelongsToProperty(ChildType, ForeignKey));
        }        
    }
}