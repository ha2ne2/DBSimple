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
        #region public method

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
        /// Typeを引数にとり、HasMany属性のリストを返します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<HasManyAttribute> GetHasManyAttrList(Type type)
        {
            return GetAttributeList<HasManyAttribute>(type);
        }

        /// <summary>
        /// Typeを引数にとり、BelongsTo属性のリストを返します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<BelongsToAttribute> GetBelongsToAttrList(Type type)
        {
            return GetAttributeList<BelongsToAttribute>(type);
        }

        /// <summary>
        /// Typeを引数にとり、PrimaryKey属性の付いたプロパティの名前を返します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string GetPrimaryKeyName(Type t)
        {
            var prop = GetPrimaryKeyProperty(t);
            return prop.Name;
        }

        /// <summary>
        /// Typeを引数にとり、そのクラスのPrimaryKey属性の付いたプロパティを取得する関数を返します。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static MethodInfo GetGetPrimaryKeyMethod(Type t)
        {
            var prop = GetPrimaryKeyProperty(t);
            return prop.GetGetMethod();
        }

        #endregion

        #region private method

        /// <summary>
        /// Typeを引数にとり、指定された属性のリストを返します。
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static List<T> GetAttributeList<T>(Type type) where T : ORAttribute
        {
            List<T> result = new List<T>();
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
            {
                var belongsToAttrs = prop
                    .GetCustomAttributes(typeof(T), true)
                    .Cast<T>();

                if (belongsToAttrs.IsEmpty())
                    continue;

                // 1つのプロパティに複数の指定されたAttributeが付いていたときは例外を投げる
                if (belongsToAttrs.Count() > 1)
                    throw new AttributeException($"Property: {type.Name}.{prop.Name} has many {typeof(T).Name}.");

                var belongsToAttr = belongsToAttrs.First();

                // 属性からプロパティへの参照をセットする
                //（プロパティから属性は取得できるが、属性からプロパティは取得できないので自分でセットする）
                belongsToAttr.Property = prop;
                result.Add(belongsToAttr);
            }

            return result;
        }

        /// <summary>
        /// Typeを引数にとり、PrimaryKey属性の付いたプロパティのPropertyInfoを返します。
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public static PropertyInfo GetPrimaryKeyProperty(Type t)
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

            return prop;
        }

        #endregion
    }
}
