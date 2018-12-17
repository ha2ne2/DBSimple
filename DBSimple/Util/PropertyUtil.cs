using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Ha2ne2.DBSimple.Exceptions;

namespace Ha2ne2.DBSimple.Util
{
    public static class PropertyUtil
    {
        /// <summary>
        /// タイプと外部キーを引数にとり、
        /// その外部キーを外部キーに持つBelongsTo属性とそれが紐づくプロパティを返します。
        /// 見つからなかった場合はnullを返します。
        /// モデル間の相互参照を実現するために使われます。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="foreignKeyPropName"></param>
        /// <returns></returns>
        public static Tuple<PropertyInfo, BelongsToAttribute>
            FindBelongsToProperty(Type t, string foreignKeyPropName)
        {
            return t.GetProperties().AsEnumerable()
                .Select(p =>
                {
                    BelongsToAttribute belongsToAttr =
                        (BelongsToAttribute)p.GetCustomAttribute(typeof(BelongsToAttribute));

                    if (belongsToAttr != null &&
                        belongsToAttr.ForeignKey == foreignKeyPropName)
                    {
                        return new Tuple<PropertyInfo, BelongsToAttribute>(
                            p,
                            belongsToAttr);
                    }
                    else
                    {
                        return null;
                    }
                }).FirstOrDefault(tuple => tuple != null);
        }

        /// <summary>
        /// タイプと外部キーを引数にとり、
        /// その外部キーを外部キーに持つHasMany属性とそれが紐づくプロパティを返します。
        /// 見つからなかった場合はnullを返します。
        /// モデル間の相互参照を実現するために使われます。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="foreignKeyPropName"></param>
        /// <returns></returns>
        public static Tuple<PropertyInfo, HasManyAttribute>
            FindHasManyProperty(Type t, string foreignKeyPropName)
        {
            return t.GetProperties().AsEnumerable()
                .Select(p =>
                {
                    HasManyAttribute hasManyAttr =
                        (HasManyAttribute)p.GetCustomAttribute(typeof(HasManyAttribute));

                    if (hasManyAttr != null &&
                        hasManyAttr.ForeignKey == foreignKeyPropName)
                    {
                        return new Tuple<PropertyInfo, HasManyAttribute>(
                            p,
                            hasManyAttr);
                    }
                    else
                    {
                        return null;
                    }
                }).FirstOrDefault(tuple => tuple != null);
        }


        /// <summary>
        /// Typeを引数にとり、HasMany属性のリストを返す。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<HasManyAttribute> GetHasManyAttrList(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<HasManyAttribute> result = new List<HasManyAttribute>();

            foreach (var prop in props)
            {
                var hasManyAttrs = prop
                    .GetCustomAttributes(typeof(HasManyAttribute), false)
                    .Cast<HasManyAttribute>();

                if (hasManyAttrs.IsEmpty())
                    continue;

                if (hasManyAttrs.Count() > 1)
                    throw new AttributeException($"Property: {type.Name}.{prop.Name} has many HasManyAttribute.");

                var hasManyAttr = hasManyAttrs.First();

                // 属性からプロパティへの逆参照をセットする
                hasManyAttr.Property = prop;
                result.Add(hasManyAttr);
            }

            return result;
        }

        public static List<BelongsToAttribute> GetBelongsToAttrList(Type type)
        {
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            List<BelongsToAttribute> result = new List<BelongsToAttribute>();

            foreach (var prop in props)
            {
                var belongsToAttrs = prop
                    .GetCustomAttributes(typeof(BelongsToAttribute), false)
                    .Cast<BelongsToAttribute>();

                if (belongsToAttrs.IsEmpty())
                    continue;

                if (belongsToAttrs.Count() > 1)
                    throw new AttributeException($"Property: {type.Name}.{prop.Name} has many BelongsToAttribute.");

                var belongsToAttr = belongsToAttrs.First();

                // 属性からプロパティへの逆参照をセットする
                belongsToAttr.Property = prop;
                result.Add(belongsToAttr);
            }
            return result;
        }

        public static string GetPrimaryKeyName(Type t)
        {
            var prop = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Count() >= 1);

            if (prop == null)
                throw new Exception($"Class: {t.Name} PrimaryKey Property was not found");

            if (!prop.CanRead)
                throw new Exception($"Class: {t.Name} PrimaryKey Property doesn't have getter");

            return prop.Name;
        }

        /// <summary>
        /// Type tを引数にとり、そのクラスのPrimaryKey属性のプロパティを取得する関数を返す関数です
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static MethodInfo GetGetPrimaryKeyMethod(Type t)
        {
            var prop = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), false).Count() >= 1);

            if (prop == null)
                throw new Exception($"Class: {t.Name} PrimaryKey Property was not found");

            if (!prop.CanRead)
                throw new Exception($"Class: {t.Name} PrimaryKey Property doesn't have getter");

            return prop.GetGetMethod();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="childType"></param>
        /// <param name="childForeignKeyProperty"></param>
        /// <returns></returns>
        public static MethodInfo GetSetBelongsToMethod(Type childType, PropertyInfo childForeignKeyProperty)
        {
            try
            {
                var belongsToAttr = childForeignKeyProperty
                    .GetCustomAttributes(typeof(BelongsToAttribute), false)
                    .Cast<BelongsToAttribute>()
                    .SingleOrDefault();

                return (belongsToAttr == null || belongsToAttr.ForeignKey.IsEmpty()) ?
                    null :
                    childType.GetProperty(belongsToAttr.ForeignKey).GetSetMethod();
            }
            catch (InvalidOperationException e)
            {
                throw new AttributeException($"Property: {childType.Name}.{childForeignKeyProperty.Name} has many BelongsToAttribute.", e);
            }
          
        }
    }
}
