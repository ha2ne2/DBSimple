using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace Ha2ne2.DBSimple.Forms
{
    public static class Util
    {
        public static string GetConnectionString()
        {
            var builder = new SqlConnectionStringBuilder()
            {
                DataSource = @"localhost\SQLEXPRESS01",
                InitialCatalog = "AdventureWorks",
                IntegratedSecurity = true,
                //UserID = "(ユーザー名)",
                //Password = "(パスワード)"
            };
            return builder.ToString();
        }

        public static T MeasureTime<T>(string actionTitle, Func<T> fn)
        {
            var sw = new Stopwatch();
            sw.Start();
            var result = fn();
            sw.Stop();
            Console.WriteLine(string.Format("{0} : {1}", actionTitle.PadRight(20), sw.Elapsed));
            return result;
        }

        private static Func<T> Measurize<T>(Func<T> fn)
        {
            return () =>
            {
                var sw = new Stopwatch();
                sw.Start();
                var result = fn();
                sw.Stop();
                Console.WriteLine(string.Format("{0} : {1}", fn.ToString().PadRight(20), sw.Elapsed));
                return result;
            };
        }

        /// <summary>
        /// DBからデータをデータテーブルとして取得
        /// </summary>
        /// <returns></returns>
        public static DataTable GetDataTable(String sql)
        {
            return Util.MeasureTime("GetDataTable", () =>
            {
                DataTable table = new DataTable();

                // 接続文字列の取得
                string connectionString = Util.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                using (var command = connection.CreateCommand())
                {
                    // データベースと接続
                    connection.Open();

                    // SQL文をコマンドにセット
                    command.CommandText = sql;

                    // SQLの実行
                    SqlDataAdapter adapter = new SqlDataAdapter(command);

                    adapter.Fill(table);
                }
                return table;
            });
        }


        /// <summary>
        /// リフレクション版
        /// DataTableをモデルのリストに変換する
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<TModel> DataTableToModelListByReflection<TModel>(DataTable table) where TModel : new()
        {
            return Util.MeasureTime("ConvertToModelList", () =>
            {
                List<TModel> modelList = new List<TModel>();
                PropertyInfo[] properties = typeof(TModel).GetProperties();

                foreach (DataRow drow in table.Rows)
                {
                    TModel model = new TModel();
                    foreach (PropertyInfo p in properties)
                    {
                        Type t = p.PropertyType;

                        // 書き込み可能プロパティでない場合は飛ばす
                        if (!p.CanWrite)
                            continue;

                        object propValue;

                        // DBNullじゃない時は普通に入れる
                        if (drow[p.Name] != DBNull.Value)
                        {
                            propValue = drow[p.Name];
                        }

                        // 値がDBNullの時は値型か参照型かで入れる値を変える
                        else
                        {
                            // 値型の時はデフォルト値を入れる
                            if (t.IsValueType)
                            {
                                propValue = Activator.CreateInstance(t);
                            }

                            // 参照型の時
                            else
                            {
                                // 文字列の時だけ特別にstring.Emptyを入れる
                                if (t == typeof(string))
                                {
                                    propValue = string.Empty;
                                }
                                else
                                {
                                    propValue = null;
                                }
                            }
                        }

                        // モデルのプロパティに値をセット
                        p.SetValue(model, propValue);
                    }
                    modelList.Add(model);
                }
                return modelList;
            });
        }

        /// <summary>
        /// 構文木作生成版
        /// Bijectorを使ってDataTableをモデルのリストに変換する
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="bijector"></param>
        /// <param name="table"></param>
        /// <returns></returns>
        public static List<TModel> DataTableToModelListByBijector<TModel>(Action<TModel, DataRow> bijector, DataTable table)
            where TModel : new()
        {
            return Util.MeasureTime("ToModelListByBiject", () =>
            {
                Stopwatch newTimer = new Stopwatch();
                Stopwatch bijectTimer = new Stopwatch();
                Stopwatch addTimer = new Stopwatch();

                List<TModel> modelList = new List<TModel>();
                foreach (DataRow drow in table.Rows)
                {
                    newTimer.Start();
                    TModel model = new TModel();
                    newTimer.Stop();

                    bijectTimer.Start();
                    bijector(model, drow);
                    bijectTimer.Stop();

                    addTimer.Start();
                    modelList.Add(model);
                    addTimer.Stop();
                }

                Console.WriteLine($"new    : {newTimer.Elapsed}");
                Console.WriteLine($"biject : {bijectTimer.Elapsed}");
                Console.WriteLine($"add    : {addTimer.Elapsed}");

                return modelList;
            });
        }

        /// <summary>
        /// 関数を返す関数です
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <returns></returns>
        public static Action<TModel, DataRow> GenerateBijector<TModel>()
        {
            List<Expression> body = new List<Expression>();

            // シンボルを作成
            ParameterExpression model = Expression.Parameter(typeof(TModel), "model");
            ParameterExpression drow = Expression.Parameter(typeof(DataRow), "drow");

            // DBNullを定数として定義
            MemberExpression DBNullValue = Expression.Field(null, typeof(DBNull), "Value");

            var propInfoList = typeof(TModel).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // モデルの全てのプロパティにDataRowの対応する値を入れる処理を生成
            foreach (PropertyInfo propInfo in propInfoList)
            {
                // プロパティの型と名前を取得
                Type typeofProp = propInfo.PropertyType;
                ConstantExpression propName = Expression.Constant(propInfo.Name);

                // モデルのプロパティのセッターを取得
                MethodInfo setProp = propInfo.GetSetMethod();

                // datarowの値を取得
                IndexExpression drowValue = Expression.Property(drow, "Item", propName);

                // bodyに追加
                body.Add(Expression.Call(
                    model,
                    setProp,
                    Expression.Condition(
                        Expression.NotEqual(drowValue, DBNullValue),
                        Expression.Convert(drowValue, typeofProp),
                        Expression.Default(typeofProp))));
            }

            // コンパイル
            Action<TModel, DataRow> biject =
                Expression.Lambda<Action<TModel, DataRow>>(
                    Expression.Block(body),
                    model,
                    drow).Compile();

            return biject;
        }
    }
}
