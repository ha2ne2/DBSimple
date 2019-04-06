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
            List<HasManyAttribute> result = new List<HasManyAttribute>();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var hasManyAttrs = prop
                    .GetCustomAttributes(typeof(HasManyAttribute), true)
                    .Cast<HasManyAttribute>();

                if (hasManyAttrs.IsEmpty())
                    continue;

                // 1つのプロパティに複数のHasManyAttributeが付いていたときは例外を投げる
                if (hasManyAttrs.Count() > 1)
                    throw new AttributeException($"Property: {type.Name}.{prop.Name} has many HasManyAttribute.");

                var hasManyAttr = hasManyAttrs.First();

                // 属性からプロパティへの参照をセットする（そうしないと参照する手段がない）
                hasManyAttr.Property = prop;
                result.Add(hasManyAttr);
            }

            return result;
        }

        public static List<BelongsToAttribute> GetBelongsToAttrList(Type type)
        {
            List<BelongsToAttribute> result = new List<BelongsToAttribute>();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var belongsToAttrs = prop
                    .GetCustomAttributes(typeof(BelongsToAttribute), true)
                    .Cast<BelongsToAttribute>();

                if (belongsToAttrs.IsEmpty())
                    continue;

                // 1つのプロパティに複数のBelongsToAttributeが付いていたときは例外を投げる
                if (belongsToAttrs.Count() > 1)
                    throw new AttributeException($"Property: {type.Name}.{prop.Name} has many BelongsToAttribute.");

                var belongsToAttr = belongsToAttrs.First();

                // 属性からプロパティへの参照をセットする（そうしないと参照する手段がない）
                belongsToAttr.Property = prop;
                result.Add(belongsToAttr);
            }
            return result;
        }

        public static string GetPrimaryKeyName(Type t)
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Count() >= 1);

            // プライマリーキープロパティが定義されていなかった場合は例外を投げる
            if (props.IsEmpty())
                throw new Exception($"{t.Name} Class: PrimaryKey Property was not found");

            // プライマリーキープロパティが複数定義されていた場合は例外を投げる
            if (props.Count() > 1)
                throw new Exception(
                    $"{t.Name} Class: has many PrimaryKey Property\r\n" +
                    string.Join(", ", props.Select(p => p.Name)));

            var prop = props.First();

            // プライマリーキープロパティにゲッターが設定されていなかった場合は例外を投げる
            if (!prop.CanRead)
                throw new Exception($"{t.Name} Class: PrimaryKey Property doesn't have getter");

            return prop.Name;
        }

        /// <summary>
        /// Type tを引数にとり、そのクラスのPrimaryKey属性のプロパティを取得する関数を返す関数です
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static MethodInfo GetGetPrimaryKeyMethod(Type t)
        {
            var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetCustomAttributes(typeof(PrimaryKeyAttribute), true).Count() >= 1);

            // プライマリーキープロパティが定義されていなかった場合は例外を投げる
            if (props.IsEmpty())
                throw new Exception($"{t.Name} Class: PrimaryKey Property was not found");

            // プライマリーキープロパティが複数定義されていた場合は例外を投げる
            if (props.Count() > 1)
                throw new Exception(
                    $"{t.Name} Class: has many PrimaryKey Property\r\n" +
                    string.Join(", ", props.Select(p => p.Name)));

            var prop = props.First();

            // プライマリーキープロパティにゲッターが設定されていなかった場合は例外を投げる
            if (!prop.CanRead)
                throw new Exception($"{t.Name} Class: PrimaryKey Property doesn't have getter");

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
                    .GetCustomAttributes(typeof(BelongsToAttribute), true)
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
