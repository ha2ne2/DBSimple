using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Forms
{
    public static class Bijector
    {
        /// <summary>
        /// TModelとSqlDataReaderを引数に取り、
        /// DBの1行をTModelにマップする関数を返す関数
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="rdr"></param>
        /// <returns></returns>
        public static Action<TModel, SqlDataReader> GenerateBijector<TModel>(SqlDataReader rdr)
        {
            List<Expression> body = new List<Expression>();

            // シンボルを作成
            ParameterExpression model = Expression.Parameter(typeof(TModel), "model");
            ParameterExpression reader = Expression.Parameter(typeof(SqlDataReader), "reader");

            // モデルのプロパティのリストを取得
            var propInfoList = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // モデルのプロパティにセットする文を生成
            foreach (PropertyInfo propInfo in propInfoList)
            {
                // 書き込めないプロパティにはセットしない
                if (!propInfo.CanWrite)
                    continue;

                // rdrから列の序数を取得
                ConstantExpression rowOrdinal = Expression.Constant(rdr.GetOrdinal(propInfo.Name));

                // モデルのプロパティのセッターを取得
                Type typeofProp = propInfo.PropertyType;
                MethodInfo setProp = propInfo.GetSetMethod();

                bool needsCast = false;
                string getterName;

                if (typeofProp == typeof(int))
                    getterName = "GetInt32";
                else if (typeofProp == typeof(string))
                    getterName = "GetString";
                else if (typeofProp == typeof(decimal))
                    getterName = "GetDecimal";
                else if (typeofProp == typeof(DateTime))
                    getterName = "GetDateTime";
                else if (typeofProp == typeof(byte))
                    getterName = "GetByte";
                else if (typeofProp == typeof(bool))
                    getterName = "GetBoolean";
                else if (typeofProp == typeof(Guid))
                    getterName = "GetGuid";
                else
                {
                    needsCast = true;
                    getterName = "GetValue";
                }

                // reader.IsDBNull(ord)相当の式木を生成
                MethodInfo isDBNull = typeof(SqlDataReader)
                    .GetMethod("IsDBNull", BindingFlags.Public | BindingFlags.Instance);
                Expression isReaderValueDBNull = Expression.Call(reader, isDBNull, rowOrdinal);

                // reader.GetValue(ord)相当の式木を生成
                MethodInfo getValue = typeof(SqlDataReader)
                    .GetMethod(getterName, BindingFlags.Public | BindingFlags.Instance);
                Expression getReaderValue = Expression.Call(reader, getValue, rowOrdinal);

                // bodyに追加
                body.Add(Expression.Call(
                    model,
                    setProp,
                    Expression.Condition(
                        isReaderValueDBNull,
                        Expression.Default(typeofProp),
                        (needsCast) ?
                            Expression.ConvertChecked(getReaderValue, typeofProp) :
                            getReaderValue)));
            }

            //var x = Expression.Lambda<Action<TModel, SqlDataReader>>(
            //    Expression.Block(body),
            //    model,
            //    reader);

            // コンパイル
            Action<TModel, SqlDataReader> biject =
                Expression.Lambda<Action<TModel, SqlDataReader>>(
                    Expression.Block(body),
                    model,
                    reader).Compile();

            return biject;
        }
    }
}
