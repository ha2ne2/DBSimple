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
using Ha2ne2.DBSimple.SampleModels;
using Ha2ne2.DBSimple.Util;

namespace Ha2ne2.DBSimple.Forms
{
    public partial class SampleForm : Form
    {
        #region コンストラクタ

        public SampleForm()
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
            Debug.WriteLine("var authors = DBSimple.ORMap<Author>(connString, \"SELECT * FROM [Author]\", preloadDepth: 2);");
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            var users = DBSimple.ORMap<Author>(
                Util.GetConnectionString(),
                "SELECT * FROM [Author]",
                preloadDepth: 2);

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine("DUMP(authors);");
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine(string.Empty);
            Debug.WriteLine(ObjectDumper.Dump(users).Replace("\r", ""));

            //Debug.WriteLine(users[0].Books[0].Title);

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ClearCache();

            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine("var books = DBSimple.ORMap<Book>(connString, \"SELECT * FROM [Book] WHERE BookID = 4\", preloadDepth: 0);");
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            var posts = DBSimple.ORMap<Book>(
                Util.GetConnectionString(),
                "SELECT * FROM [Book] WHERE BookID = 4",
                preloadDepth: 0);

            Debug.WriteLine(string.Empty);
            Debug.WriteLine("---------------------------------------------------------------------------------------");
            Debug.WriteLine("DUMP(books);");
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
