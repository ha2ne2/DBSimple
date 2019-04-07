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
        public static Tuple<PropertyInfo, BelongsToAttribute> FindBelongsToProperty(
            Type targetType,
            Type referenceType,
            string foreignKey)
        {
            return FindORProperty<BelongsToAttribute>(targetType, referenceType, foreignKey);
        }

        /// <summary>
        /// targetTypeクラス中から、referenceTypeをReferenceTypeに持ち
        /// foreignKeyをForeignKeyに持つHasMany属性とそれが紐づくプロパティを返します。
        /// 見つからなかった場合はnullを返します。
        /// モデル間の相互参照を実現するために使われます。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="foreignKey"></param>
        /// <returns></returns>
        public static Tuple<PropertyInfo, HasManyAttribute> FindHasManyProperty(
            Type targetType,
            Type referenceType,
            string foreignKey)
        {
            return FindORProperty<HasManyAttribute>(targetType, referenceType, foreignKey);
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
                var orAttributeList = prop
                    .GetCustomAttributes(typeof(T), true)
                    .Cast<T>();

                if (orAttributeList.IsEmpty())
                    continue;

                // 指定されたAttributeが1つのプロパティに複数付いていた場合は例外を投げる
                if (orAttributeList.Count() > 1)
                    throw new AttributeException($"Property: {type.Name}.{prop.Name} has many {typeof(T).Name}.");

                var orAttribute = orAttributeList.First();

                // 属性からプロパティへの参照をセットする
                //（プロパティから属性は取得できるが、属性からプロパティは取得できないので自分でセットする）
                orAttribute.Property = prop;
                orAttribute.MyType = type;
                result.Add(orAttribute);
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
                throw new AttributeException(
                    $"{t.Name} Class: has many PrimaryKey Property\r\n" +
                    string.Join(", ", props.Select(p => p.Name)));

            var prop = props.First();

            // プライマリーキープロパティにゲッターが設定されていなかった場合は例外を投げる
            if (!prop.CanRead)
                throw new Exception($"{t.Name} Class: PrimaryKey Property doesn't have getter");

            return prop;
        }

        /// <summary>
        /// targetTypeクラスの中から、referenceTypeをReferenceTypeに持ち
        /// foreignKeyをForeignKeyに持つHasMany属性とそれが紐づけられたプロパティを返します。
        /// 見つからなかった場合はnullを返します。
        /// モデル間の相互参照を実現するために使われます。
        /// </summary>
        /// <param name="t"></param>
        /// <param name="foreignKey"></param>
        /// <returns></returns>
        public static Tuple<PropertyInfo, T> FindORProperty<T>(
            Type targetType,
            Type referenceType,
            string foreignKey)
            where T : ORAttribute
        {
            var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => {
                    T attr = (T)p.GetCustomAttribute(typeof(T), true);
                    return
                        (attr != null &&
                         attr.ReferenceType == referenceType &&
                         attr.ForeignKey == foreignKey);
                });

            // 指定された条件の属性の設定されたプロパティが見つからなかった場合はnullを返す
            //（必ずしも設定されていないと行けないわけではないため）
            if (props.IsEmpty())
                return null;

            // 指定された条件の属性の設定されたプロパティが複数見つかった場合は例外を投げる
            if (props.Count() > 1)
                throw new AttributeException(
                    string.Format(
                        "Class: {0} has many [{1} ({2},{3})] .\r\n{4}",
                        targetType.Name,
                        typeof(T).Name,
                        referenceType.Name,
                        foreignKey,
                        string.Join(", ", props.Select(p => p.Name))));

            var prop = props.First();

            // 指定された条件の属性の設定されたプロパティにゲッターが設定されていなかった場合は例外を投げる
            if (!prop.CanRead)
                throw new Exception($"{targetType.Name} Class: {typeof(T).Name} Property doesn't have getter");

            var orAttribute = (T)prop.GetCustomAttribute(typeof(T), true);
            return new Tuple<PropertyInfo, T>(prop, orAttribute);
        }

        #endregion
    }
}
