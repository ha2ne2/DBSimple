using Ha2ne2.DBSimple.Exceptions;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Util
{
    public static class FunctionGenerator
    {
        /// <summary>
        /// TModelとSqlDataReaderを引数に取り、
        /// DBの1行をTModelにマップする関数を返す関数
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="selectQuery">セレクトクエリ(例外が起こった時の表示用)</param>
        /// <param name="rdr"></param>
        /// <param name="actualType"></param>
        /// <returns></returns>
        public static Func<SqlDataReader, TModel> GenerateMapFunction<TModel>(
            string selectQuery,
            SqlDataReader rdr,
            Type actualType = null)
        {
            if (actualType == null)
                actualType = typeof(TModel);

            // 生成するmapメソッドの本体
            List<Expression> body = new List<Expression>();

            // シンボルを作成
            ParameterExpression reader = Expression.Parameter(typeof(SqlDataReader), "reader");
            ParameterExpression model = Expression.Variable(actualType, "model");

            // model = new actualType();
            body.Add(Expression.Assign(model, Expression.New(actualType)));

            // モデルのプロパティのリストを取得
            var propInfoList = actualType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // モデルのプロパティにセットする文を生成
            foreach (PropertyInfo propInfo in propInfoList)
            {
                // 書き込めないプロパティはスキップ
                if (!propInfo.CanWrite)
                    continue;

                Type propType = propInfo.PropertyType;

                // ORAttributeがついている場合はスキップ
                if (propInfo.GetCustomAttributes<ORAttribute>().IsNotEmpty())
                    continue;

                // rdrから列の序数を取得
                ConstantExpression colOrdinal = null;

                try
                {
                    colOrdinal = Expression.Constant(rdr.GetOrdinal(propInfo.Name));
                }
                catch (IndexOutOfRangeException)
                {
                    throw new ColumnNotFoundException($"Column {propInfo.Name} was Not Found. Check your model definition and Query.\r\nModel: {actualType.ToString()}\r\nSelect Query : {selectQuery}");
                }


                // モデルのプロパティのセッターを取得
                MethodInfo setProp = propInfo.GetSetMethod();

                // モデルのプロパティの型に応じてrdrのゲッターを変える
                bool needsCast = false;
                string getterName;

                if (propType == typeof(int))
                    getterName = nameof(SqlDataReader.GetInt32);
                else if (propType == typeof(string))
                    getterName = nameof(SqlDataReader.GetString);
                else if (propType == typeof(decimal))
                    getterName = nameof(SqlDataReader.GetDecimal);
                else if (propType == typeof(DateTime))
                    getterName = nameof(SqlDataReader.GetDateTime);
                else if (propType == typeof(byte))
                    getterName = nameof(SqlDataReader.GetByte);
                else if (propType == typeof(bool))
                    getterName = nameof(SqlDataReader.GetBoolean);
                else if (propType == typeof(Guid))
                    getterName = nameof(SqlDataReader.GetGuid);
                else
                {
                    needsCast = true;
                    getterName = "GetValue";
                }

                // reader.IsDBNull(ord)相当の式木を生成
                MethodInfo isDBNull = typeof(SqlDataReader)
                    .GetMethod(nameof(SqlDataReader.IsDBNull), BindingFlags.Public | BindingFlags.Instance);
                Expression isReaderValueDBNull = Expression.Call(reader, isDBNull, colOrdinal);

                // reader.GetValue(ord)相当の式木を生成
                MethodInfo getValue = typeof(SqlDataReader)
                    .GetMethod(getterName, BindingFlags.Public | BindingFlags.Instance);
                Expression getReaderValue = Expression.Call(reader, getValue, colOrdinal);

                // bodyに追加
                body.Add(Expression.Call(
                    model,
                    setProp,
                    Expression.Condition(
                        isReaderValueDBNull,
                        Expression.Default(propType),
                        (needsCast) ?
                            Expression.ConvertChecked(getReaderValue, propType) :
                            getReaderValue)));
            }

            body.Add(model);

            // コンパイル
            Func<SqlDataReader, TModel> map =
                Expression.Lambda<Func<SqlDataReader, TModel>>(
                    Expression.Block(typeof(TModel), new[] { model }, body),
                    reader).Compile();

            return map;
        }


        /// <summary>
        /// object AとList＜object＞ Bを引数に取り、
        /// AをmodelTypeにキャストし、
        /// BをList＜elemType＞にキャストし、
        /// objectのList＜elemType＞型のプロパティにBセットする関数を返す関数です。
        /// </summary>
        /// <param name="setListProperty"></param>
        /// <param name="modelType"></param>
        /// <param name="propType"></param>
        /// <returns></returns>
        public static Action<object, List<object>> GenerateSetObjListToListPropFunction(
            MethodInfo setListProperty,
            Type modelType,
            Type propType)
        {
            // このメソッドの目標は下記のAction型のインスタンスを生成することです。
            // 通常C#ではType型のインスタンスを使ってキャストをしたり、
            // Type型のインスタンスをジェネリクスの型引数にすることが出来ませんが、
            // 式木を使いメソッドを動的に生成することで可能にします。
            /*
            Action<object, List<object>> set = (model, objList) =>
            {
                List<elemType> castedList = new List<elemType>();
                int len = objList.Count();
                int i = 0;
                for (; ; )
                {
                    if (i >= len)
                        break;
                    castedList.Add((elemType)objList[i]);
                    i++;
                }
                setListProperty((modelType)model, castedList);
            };
            */

            #region 下準備

            // elemType型からList<elemType>型を作る
            Type listType = typeof(List<>).MakeGenericType(propType);

            // IEnumerable<object>.Count()を取得
            // 実体はEnumerable.Count(this IEnumerable<T> source)
            MethodInfo count =
                typeof(Enumerable)
                    .GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .Where(m =>
                    {
                        var p = m.GetParameters();
                        return
                            m.Name == nameof(Enumerable.Count) &&
                            p.Length == 1 &&
                            p[0].ParameterType.IsGenericType &&
                            p[0].ParameterType.GetGenericTypeDefinition() == typeof(IEnumerable<>);
                    })
                    .SingleOrDefault()
                    .MakeGenericMethod(typeof(object));

            // List<childType>.Addメソッドを取得
            MethodInfo listAdd = listType
                .GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);

            #endregion

            #region 式木生成

            // 生成するメソッドの本体
            List<Expression> body = new List<Expression>();

            // シンボルを生成
            ParameterExpression model = Expression.Parameter(typeof(object), "model");
            ParameterExpression objList = Expression.Parameter(typeof(List<object>), "objList");
            ParameterExpression castedList = Expression.Variable(listType, "castedList");
            ParameterExpression len = Expression.Variable(typeof(int), "len");
            ParameterExpression i = Expression.Variable(typeof(int), "i");

            // castedList = new List<elemType>(); を生成
            body.Add(Expression.Assign(
                castedList,
                Expression.New(listType)));

            // len = objList.Count(); を生成
            body.Add(Expression.Assign(len,
                Expression.Call(null, count, objList)));

            // i = 0; を生成
            body.Add(Expression.Assign(i, Expression.Constant(0)));

            // for(;;) {if (i >= len) break; castedList.Add((elemType)obj[i]); i++} を生成
            LabelTarget endLoop = Expression.Label("EndLoop");
            body.Add(Expression.Loop(
                Expression.Block(
                    Expression.IfThen(
                        Expression.GreaterThanOrEqual(i, len),
                        Expression.Break(endLoop)),
                    Expression.Call(
                        castedList,
                        listAdd,
                        Expression.ConvertChecked(
                            Expression.Property(objList, "Item", i),
                            propType)),
                    Expression.AddAssign(i, Expression.Constant(1))),
                endLoop));

            // setListProperty(model, castedList); を生成
            body.Add(Expression.Call(
                Expression.Convert(model, modelType),
                setListProperty,
                castedList));

            // コンパイル！
            Action<object, List<object>> set =
                Expression.Lambda<Action<object, List<object>>>(
                    Expression.Block(new[] { castedList, i, len }, body),
                    model, objList).Compile();

            #endregion

            return set;
        }


        /// <summary>
        /// 2つのobject、A、Bを引数に取り、
        /// AをmodelTypeにキャストし、
        /// BをelemTypeにキャストし、
        /// AのelemType型のプロパティにBをセットする関数を返す関数です。
        /// </summary>
        /// <param name="setProperty"></param>
        /// <param name="modelType"></param>
        /// <param name="propType"></param>
        /// <returns></returns>
        public static Action<object, object> GenerateSetObjToPropFunction(
            MethodInfo setProperty,
            Type modelType,
            Type propType)
        {
            // このメソッドの目標は下記のAction型のインスタンスを生成することです。
            // 通常C#ではType型のインスタンスを使ってキャストをすることが出来ませんが、
            // 式木を使いメソッドを動的に生成することで可能にします。
            /*
            Action<object, object> set = (model, obj) =>
            {
                setProperty((modelType)model, (elemType)obj);
            };
            */

            #region 式木生成

            // 生成するメソッドの本体
            List<Expression> body = new List<Expression>();

            // シンボルを生成
            ParameterExpression model = Expression.Parameter(typeof(object), "model");
            ParameterExpression obj = Expression.Parameter(typeof(object), "obj");

            // setProperty(model, castedList); を生成
            body.Add(Expression.Call(
                Expression.Convert(model, modelType),
                setProperty,
                Expression.ConvertChecked(obj, propType)));

            // コンパイル！
            Action<object, object> set =
                Expression.Lambda<Action<object, object>>(
                    Expression.Block(body),
                    model, obj).Compile();

            #endregion

            return set;
        }
    }
}
