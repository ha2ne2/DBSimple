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
            dataGridView1.DataSource = GetModelListByReflection<OrderHeader>("SELECT * FROM Sales.SalesOrderheader2");
        }

        private void btnGetByDapper_Click(object sender, EventArgs e)
        {
            ClearCache();
            dataGridView1.DataSource = GetModelListByDapper<OrderHeader>("SELECT * FROM Sales.SalesOrderheader2");
        }

        private void btnGetByBijector_Click(object sender, EventArgs e)
        {
            ClearCache();
            //dataGridView1.DataSource = GetModelListByBijector<OrderHeader>("SELECT * FROM Sales.SalesOrderheader2");
            var OrderHeaderList = DBSimple.ORMap<OrderHeader>(
                Util.GetConnectionString(),
                "SELECT * FROM Sales.SalesOrderheader2");

            dataGridView1.DataSource = OrderHeaderList;
            int i = 1;
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
            Debug.WriteLine(ObjectDumper.Dump(users).Replace("\r",""));

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


        private void btnAsyncAwait_Click(object sender, EventArgs e)
        {
            dataGridView1.DataSource = null;
        }

        #endregion

        #region プライベートメソッド

        private void ClearCache()
        {
            // 接続文字列の取得
            string connectionString = Util.GetConnectionString();

            using (var connection = new SqlConnection(connectionString))
            using (var command = connection.CreateCommand())
            {
                // データベースと接続
                connection.Open();

                // SQL文をコマンドにセット
                command.CommandText = "DBCC DROPCLEANBUFFERS;DBCC FREEPROCCACHE";

                command.ExecuteNonQuery();
            }
        }

        private List<TModel> GetModelListByDapper<TModel>(string sql)
        {
            return Util.MeasureTime("GetModelList2", () =>
            {
                List<TModel> modelList = new List<TModel>();

                // 接続文字列の取得
                string connectionString = Util.GetConnectionString();

                using (var connection = new SqlConnection(connectionString))
                using (var command = connection.CreateCommand())
                {
                    // データベースと接続
                    connection.Open();

                    return connection.Query<TModel>(sql).ToList();
                }
            });
        }

        /// <summary>
        /// DBからモデルのリストを取得
        /// </summary>
        /// <returns></returns>
        private List<TModel> GetModelListByReflection<TModel>(string sql) where TModel : new()
        {
            DataTable table = Util.GetDataTable(sql);
            List<TModel> modelList = Util.DataTableToModelListByReflection<TModel>(table);

            return modelList;
        }

        #endregion

    }
}
