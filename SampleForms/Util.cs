using Ha2ne2.DBSimple.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
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
                DataSource = ConfigurationManager.AppSettings["SQLServerName"],
                InitialCatalog = ConfigurationManager.AppSettings["SQLDBName"],
                IntegratedSecurity = Convert.ToBoolean(ConfigurationManager.AppSettings["TrustedConnection"]),
                //AuthorID = "(ユーザー名)",
                //Password = "(パスワード)"
            };
            return builder.ToString();
        }

        /// <summary>
        /// DBからデータをデータテーブルとして取得
        /// </summary>
        /// <returns></returns>
        public static DataTable GetDataTable(string connectionString, string sql)
        {
            return CommonUtil.MeasureTime("GetDataTable", string.Empty, 1, () =>
            {
                DataTable table = new DataTable();

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
            return CommonUtil.MeasureTime("DataTableToModelList", string.Empty, 1, () =>
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
    }
}
