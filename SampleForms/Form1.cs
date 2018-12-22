using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using Dapper;
using Ha2ne2.DBSimple.Model;
using Ha2ne2.DBSimple.Util;

namespace Ha2ne2.DBSimple.Forms
{
    public partial class Form1 : Form
    {
        #region コンストラクタ

        public Form1()
        {
            InitializeComponent();
        }

        #endregion

        #region イベントハンドラ

        private void btnGetByReflection_Click(object sender, EventArgs e)
        {
            ClearCache();

            List<OrderHeader> orderHeaderList = GetModelListByReflection<OrderHeader>(
                Util.GetConnectionString(),
                "SELECT * FROM Sales.SalesOrderheader");

            dataGridView1.DataSource = orderHeaderList;
        }

        private void btnGetByDBSimple_Click(object sender, EventArgs e)
        {
            ClearCache();

            List<OrderHeader> orderHeaderList = GetModelListByDBSimple<OrderHeader>(
                Util.GetConnectionString(),
                "SELECT * FROM Sales.SalesOrderheader");

            dataGridView1.DataSource = orderHeaderList;
        }

        private void btnGetByDapper_Click(object sender, EventArgs e)
        {
            ClearCache();

            List<OrderHeader> orderHeaderList = GetModelListByDapper<OrderHeader>(
                Util.GetConnectionString(),
                "SELECT * FROM Sales.SalesOrderheader");

            dataGridView1.DataSource = orderHeaderList;
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
        }

        private void btnORMap_Click(object sender, EventArgs e)
        {
            ClearCache();

            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine("var users = DBSimple.ORMap<User>(connString, \"SELECT * FROM [User]\", preloadDepth: 2);");
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            var users = DBSimple.ORMap<User>(
                Util.GetConnectionString(),
                "SELECT * FROM [User]",
                preloadDepth: 2);

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine("DUMP(users);");
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine(string.Empty);
            Debug.WriteLine(ObjectDumper.Dump(users).Replace("\r", ""));

            //Debug.WriteLine(users[0].Posts[0].Title);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ClearCache();

            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine("var posts = DBSimple.ORMap<Post>(connString, \"SELECT * FROM [Post] WHERE PostID = 1002\", preloadDepth: 0);");
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            var posts = DBSimple.ORMap<Post>(
                Util.GetConnectionString(),
                "SELECT * FROM [Post] WHERE PostID = 1002",
                preloadDepth: 0);

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine("DUMP(posts);");
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine(string.Empty);
            Debug.WriteLine(ObjectDumper.Dump(posts).Replace("\r", ""));
        }

        #endregion

        #region プライベートメソッド

        /// <summary>
        /// リフレクションを使ってDBからモデルのリストを取得
        /// </summary>
        /// <returns></returns>
        private List<TModel> GetModelListByReflection<TModel>(string connectionString, string selectQuery) where TModel : new()
        {
            return CommonUtil.MeasureTime("Reflection", string.Empty, 0, () =>
            {
                DataTable table = Util.GetDataTable(connectionString, selectQuery);
                List<TModel> modelList = Util.DataTableToModelListByReflection<TModel>(table);

                return modelList;
            });
        }

        private List<TModel> GetModelListByDBSimple<TModel>(string connectionString, string selectQuery)
        {
            return CommonUtil.MeasureTime("DBSimple", string.Empty, 0, () =>
            {
                List<TModel> modelList = new List<TModel>();

                using (var connection = new SqlConnection(connectionString))
                {
                    // データベースと接続
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    using (var tx = connection.BeginTransaction())
                    {
                        return DBSimple.SimpleMap<TModel>(tx, selectQuery);
                    }
                }
            });
        }

        /// <summary>
        /// Dapperを使ってDBからモデルのリストを取得
        /// </summary>
        /// <returns></returns>
        private List<TModel> GetModelListByDapper<TModel>(string connectionString, string selectQuery)
        {
            return CommonUtil.MeasureTime("Dapper", string.Empty, 0, () =>
            {
                List<TModel> modelList = new List<TModel>();

                using (var connection = new SqlConnection(connectionString))
                {
                    // データベースと接続
                    connection.Open();

                    using (var command = connection.CreateCommand())
                    using (var tx = connection.BeginTransaction())
                    {
                        return connection.Query<TModel>(selectQuery, null, tx).ToList();
                    }
                }
            });
        }

        /// <summary>
        /// DBの実行プランのキャッシュなどをクリアする。
        /// </summary>
        private void ClearCache()
        {
            // 接続文字列の取得
            string connectionString = Util.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            using (var command = connection.CreateCommand())
            {
                connection.Open();
                command.CommandText = "DBCC DROPCLEANBUFFERS; DBCC FREEPROCCACHE";
                command.ExecuteNonQuery();
            }
        }

        #endregion

    }
}
