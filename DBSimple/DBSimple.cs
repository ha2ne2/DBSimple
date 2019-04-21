using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Ha2ne2.DBSimple.Util;

namespace Ha2ne2.DBSimple
{
    public static class DBSimple
    {
        #region public method

        /// <summary>
        /// 接続文字列を引数で指定して、フラットなマッピングを行います。
        /// </summary>
        /// <typeparam name="TModel">マップするモデルの型</typeparam>
        /// <param name="connectionString">接続文字列</param>
        /// <param name="selectQuery">SQL</param>
        /// <returns></returns>
        public static List<TModel> SimpleMap<TModel>(string connectionString, string selectQuery)
        {
            using (var connection = new SqlConnection(connectionString))
            {
                // データベースと接続
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    return SimpleMap<TModel>(tx, selectQuery);
                }
            }
        }

        /// <summary>
        /// トランザクションを引数で指定して、フラットなマッピングを行います。
        /// </summary>
        /// <typeparam name="TModel">マップするモデルの型</typeparam>
        /// <param name="tx">トランザクション</param>
        /// <param name="selectQuery">SQL</param>
        /// <returns></returns>
        public static List<TModel> SimpleMap<TModel>(SqlTransaction tx, string selectQuery)
        {
            using (var command = new SqlCommand())
            {
                // コマンドの組み立て
                command.Connection = tx.Connection;
                command.Transaction = tx;
                command.CommandText = selectQuery;

                // SQLの実行
                List<TModel> modelList = new List<TModel>();

                using (SqlDataReader rdr = command.ExecuteReader())
                {
                    Func<SqlDataReader, TModel> map = FunctionGenerator.GenerateMapFunction<TModel>(selectQuery, rdr);
                    while (rdr.Read())
                    {
                        modelList.Add(map(rdr));
                    }
                }

                return modelList;
            }
        }

        /// <summary>
        /// 接続文字列を引数で指定して、ORマップをします。
        /// </summary>
        /// <typeparam name="TModel">マップするモデルの型</typeparam>
        /// <param name="connectionString">接続文字列</param>
        /// <param name="selectQuery">SQL</param>
        /// <param name="preloadDepth">プロパティをプリロードする深さ</param>
        /// <returns></returns>
        public static List<TModel> ORMap<TModel>(
            string connectionString,
            string selectQuery,
            int preloadDepth = 1
            )
            where TModel : class
        {
            using (new MyTimer("ORMap", "TOTAL ELAPSED TIME", 0))
            {
                return ORMapInternal<TModel>(
                    null,
                    connectionString,
                    selectQuery,
                    typeof(TModel),
                    preloadDepth,
                    0,
                    null, null, null, null);
            }
        }

        /// <summary>
        /// トランザクションと接続文字列を指定して、ORマップをします。
        /// 接続文字列は遅延バインドされたプロパティのデータ取得時に使われます。
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="tx"></param>
        /// <param name="connectionString"></param>
        /// <param name="selectQuery"></param>
        /// <param name="preloadDepth">プロパティをプリロードする深さ</param>
        /// <returns></returns>
        public static List<TModel> ORMap<TModel>(
            SqlTransaction tx,
            string connectionString,
            string selectQuery,
            int preloadDepth = 1
            )
            where TModel : class
        {
            using(new MyTimer("ORMap", "TOTAL ELAPSED TIME",0))
            {
                return ORMapInternal<TModel>(
                    tx,
                    connectionString,
                    selectQuery,
                    typeof(TModel),
                    preloadDepth,
                    0,
                    null, null, null, null);
            }
        }

        #endregion

        #region private method

        /// <summary>
        /// 再帰的にORマップします。
        /// </summary>
        /// <typeparam name="TModel">モデルの型</typeparam>
        /// <param name="tx">SQLトランザクション（nullの場合は、接続文字列を使ってトランザクションを開始します）</param>
        /// <param name="connectionString">接続文字列</param>
        /// <param name="selectQuery">セレクトクエリ</param>
        /// <param name="typeofModel">
        /// モデルの実際の型です。
        /// 再帰的にプリロードする際に、TModelにはobjectが指定され、この引数に実際の型が指定され呼び出されます。
        /// ジェネリクスの型引数（TModel）は実行時に指定出来ない為この引数が必要になります。
        /// </param>
        /// <param name="preloadDepth">プロパティをプリロードする深さ</param>
        /// <param name="currentDepth">現在の深さ（出力のインデントに使う）</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みBelongsToプロパティの名前</param>
        /// <param name="loadedBelongsToObj">読み込み済みBelongsToプロパティのモデルのリスト</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みHasManyプロパティの名前</param>
        /// <param name="loadedHasManyObj">読み込み済みHasManyプロパティのモデルのディクショナリ(キーはHasManyインスタンスのForeignKey)</param>
        /// <returns></returns>
        private static List<TModel> ORMapInternal<TModel>(
            SqlTransaction tx,
            string connectionString,
            string selectQuery,
            Type typeofModel,
            int preloadDepth,
            int currentDepth,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedBelongsToObj,
            string loadedHasManyPropertyName,
            Dictionary<int, object> loadedHasManyObj
            )
            where TModel : class
        {
            if (connectionString.IsEmpty())
                throw new Exception("ConnectionString is Empty");

            using (var command = new SqlCommand())
            {
                List<TModel> modelList = new List<TModel>();
                SqlConnection temporaryConnection = null;
                try
                {
                    // 引数で指定されたトランザクションがnullなら
                    // 接続文字列を元にコネクションを開きトランザクションを開始する
                    if (tx == null)
                    {
                        temporaryConnection = new SqlConnection(connectionString);
                        temporaryConnection.Open();
                        tx = temporaryConnection.BeginTransaction();
                    }

                    // コマンドの組み立て
                    command.Connection = tx.Connection;
                    command.Transaction = tx;
                    command.CommandText = selectQuery;

                    using (new MyTimer(typeofModel.Name, selectQuery, currentDepth)) // 時間測定
                    using (SqlDataReader rdr = command.ExecuteReader()) // SQLの発行
                    {
                        // DBの1行をTModel1つにマップするメソッドを生成
                        Func<SqlDataReader, TModel> map =
                            FunctionGenerator.GenerateMapFunction<TModel>(selectQuery, rdr, typeofModel);

                        // 全件マップする
                        while (rdr.Read())
                        {
                            modelList.Add(map(rdr));
                        }
                    }

                    if (modelList.IsEmpty())
                    {
                        // nop
                    }

                    // preloadDepthが0より大きい場合、再帰的にプリロードする
                    else if (preloadDepth > 0)
                    {
                        PreloadBelongsTo(tx, connectionString, modelList.AsEnumerable(), preloadDepth, currentDepth,
                            loadedBelongsToPropertyName, loadedBelongsToObj);
                        PreloadHasMany(tx, connectionString, modelList.AsEnumerable(), preloadDepth, currentDepth,
                            loadedHasManyPropertyName, loadedHasManyObj);
                    }

                    // preloadDepthが0以下の場合、プリロードはせず、代わりに
                    // 遅延読み込みする為のメソッドをモデルにセットする。
                    else
                    {
                        SetLazyObjToBelongsToProp(connectionString, modelList.AsEnumerable(),
                            loadedBelongsToPropertyName, loadedBelongsToObj);
                        SetLazyObjToHasManyProp(connectionString, modelList.AsEnumerable(),
                            loadedHasManyPropertyName, loadedHasManyObj);
                    }

                    return modelList;
                }
                finally
                {
                    // 自分でコネクションを開いていた場合は閉じる
                    if (temporaryConnection != null)
                    {
                        temporaryConnection.Dispose();
                        tx.Dispose();
                    }
                }
            }
        }

        /// <summary>
        /// IEnumerable＜object＞型にキャストされたモデルのリストの
        /// HasMany属性のついたプロパティをPreloadします。
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="modelList">読み込み対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedHasManyObjDict">読み込み済みプロパティのインスタンスのディクショナリ</param>
        private static void PreloadHasMany(
            SqlTransaction tx,
            string connectionString,
            IEnumerable<object> modelList,
            int preloadDepth,
            int currentDepth,
            string loadedHasManyPropertyName,
            Dictionary<int, object> loadedHasManyObjDict
            )
        {
            // モデルのリストが空の時はreturn
            if (modelList.IsEmpty())
                return;

            Type modelType = modelList.First().GetType();
            List<HasManyAttribute> hasManyAttrList = PropertyUtil.GetHasManyAttrList(modelType);

            // HasMany属性のついたプロパティが無い時はreturn
            if (hasManyAttrList.IsEmpty())
                return;

            // モデルのプライマリーキーを集めて
            // SELECT * FROM has_many_table WHERE has_many_FK IN (1,2,3) の 1,2,3の部分を作る
            // TODO プライマリーキープロパティ名がテーブルのプライマリーキー名という前提
            MethodInfo getModelPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(modelType);
            string modelPrimaryKeyList = modelList
                .Select(model => (int)getModelPrimaryKeyMethod.Invoke(model, null))
                .OrderBy(i => i).JoinToString(", ");

            foreach (var hasManyAttr in hasManyAttrList)
            {
                var context = new SetHasManyContext(
                    hasManyAttr,
                    modelType,
                    modelList,
                    modelPrimaryKeyList,
                    getModelPrimaryKeyMethod);

                //// SQL文を作る
                string selectQuery = string.Format(
                    "SELECT * FROM [{0}] WHERE [{1}] IN ({2})",
                    hasManyAttr.ReferenceType.Name, // TODO クラス名がテーブル名という前提
                    hasManyAttr.ForeignKey,         // TODO 外部キープロパティ名が外部キー名という前提
                    context.ReferenceKeyList);

                //// SQLを発行してHasManyのリストを作り、ForeignKeyをキーにしたLookupに変換する
                ILookup<int, object> hasManyLookup = ORMapInternal<object>(
                    tx, connectionString, selectQuery, hasManyAttr.ReferenceType,
                    preloadDepth - 1, currentDepth + 1,
                    hasManyAttr.InverseBelongsToPropertyName, modelList,
                    null, null
                    )
                    .ToLookup(
                    hasManyObj =>　(int)context.GetForeginKeyMethod.Invoke(hasManyObj, null),
                    hasManyObj =>
                    {
                        object loaded = null;

                        // Book BelongsTo Author、 Author HasMany Bookという関係があったとする。
                        // 今手元にbookAというオブジェクトがあり、このメソッドで「bookA.Author.Books」に
                        // 対するセット処理が走っているとする。
                        // その際、SELECT * FROM Book WHERE AuthorID = 3 というようなSQLが発行され
                        // その結果がList<Book>にマッピングされAuthor.Booksにセットされる。
                        // そこには bookA == book.Author.Books[n] となるようなnが存在する。
                        // 単純にSQLを発行した結果得られたList<Book>をAuthor.Booksにセットすると
                        // オブジェクト同一性が保たれない。
                        // それが保たれるように、Books[n]にはbookAをセットする。
                        if (hasManyAttr.Property.Name == loadedHasManyPropertyName &&
                            loadedHasManyObjDict.TryGetValue((int)context.GetPrimaryKeyMethod.Invoke(hasManyObj,null), out loaded))
                        {
                            return loaded;
                        }
                        else
                        {
                            return hasManyObj;
                        }
                    });

                //// HasManyプロパティにインスタンスをセットしていく
                foreach (var model in modelList)
                {
                    int referenceKey = (int)context.GetReferenceKeyMethod.Invoke(model, null);
                    context.SetObjListToHasManyProp(model, hasManyLookup[referenceKey].ToList());
                }
            }
        }

        /// <summary>
        /// IEnumerable＜object＞型にキャストされたモデルのリストの
        /// BelongsTo属性のついたプロパティをPreloadします。
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="modelList">対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedBelongsToObj">読み込み済みプロパティのモデル</param>
        private static void PreloadBelongsTo(
            SqlTransaction tx,
            string connectionString,
            IEnumerable<object> modelList,
            int preloadDepth,
            int currentDepth,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedBelongsToObj
            )
        {
            // モデルのリストが空の時はreturn
            if (modelList.IsEmpty())
                return;

            Type modelType = modelList.First().GetType();
            List<BelongsToAttribute> belongsToAttrList = PropertyUtil.GetBelongsToAttrList(modelType);

            foreach (var belongsToAttr in belongsToAttrList)
            {
                var context = new SetBelongsToContext(
                    belongsToAttr,
                    modelType,
                    modelList);

                Dictionary<int, object> belongsToDict = null;

                // authorA.Books[3].Authorという構造があったときに
                // authorA == authorA.Books[3].Author なので再読込しない
                if (belongsToAttr.Property.Name == loadedBelongsToPropertyName)
                {
                    belongsToDict = loadedBelongsToObj.ToDictionary(
                        belongsToObj => (int)context.GetReferenceKeyMethod.Invoke(belongsToObj, null));
                }
                else
                {
                    //// SQL文を作る
                    // SELECT * FROM belongsToTable WHERE referenceKey IN (1,2,3) の 1,2,3の部分を作る
                    string foreignKeyList = modelList
                        .Select(model => (int)context.GetForeignKeyMethod.Invoke(model, null))
                        .Distinct().OrderBy(i => i).JoinToString(", ");

                    string selectQuery = string.Format(
                        "SELECT * FROM [{0}] WHERE [{1}] IN ({2})",
                        belongsToAttr.ReferenceType.Name, // TODO BelongsToのクラス名がテーブル名という前提
                        context.ReferenceKeyPropName,     // TODO referenceKeyプロパティ名が参照先列名という前提
                        foreignKeyList);

                    //// SQLを発行してbelongsToのリストを作り、referenceKeyをキーにしたDictionaryに変換する
                    belongsToDict = ORMapInternal<object>(
                        tx, connectionString, selectQuery, belongsToAttr.ReferenceType,
                        preloadDepth - 1, currentDepth + 1,
                        null, null,
                        belongsToAttr.InverseHasManyPropertyName, context.ModelDict
                        )
                        .ToDictionary(belongsToObj => (int)context.GetReferenceKeyMethod.Invoke(belongsToObj, null));
                }

                //// BelongsToプロパティにインスタンスをセットしていく
                foreach (var model in modelList)
                {
                    int foreignKey = (int)context.GetForeignKeyMethod.Invoke(model, null);

                    // ここ落としたほうがいいか？（外部キーに対応するbelongsToレコードがない場合）
                    if (belongsToDict.ContainsKey(foreignKey))
                    {
                        context.SetBelongsTo(model, belongsToDict[foreignKey]);
                    }
                    else
                    {
                        Debug.WriteLine($"foreign key value {foreignKey} was not found on {modelType}.{context.ReferenceKeyPropName}");
                    }
                }
            }
        }

        /// <summary>
        /// HasManyプロパティの遅延読み込み用メソッドを作成しモデルにセットします
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="modelList">読み込み対象のモデル</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedHasManyObj">読み込み済みプロパティのモデル</param>
        private static void SetLazyObjToHasManyProp(
            string connectionString,
            IEnumerable<object> modelList,
            string loadedHasManyPropertyName,
            Dictionary<int, object> loadedHasManyObjDict
            )
        {
            // モデルのリストが空の時はreturn
            if (modelList.IsEmpty())
                return;

            Type modelType = modelList.First().GetType();
            List<HasManyAttribute> hasManyAttrList = PropertyUtil.GetHasManyAttrList(modelType);

            // HasMany属性のついたプロパティが無い時はreturn
            if (hasManyAttrList.IsEmpty())
                return;

            // モデルをプライマリーキーを集めて
            // SELECT * FROM has_many_table WHERE has_many_FK IN (1,2,3) の 1,2,3の部分を作る
            MethodInfo getModelPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(modelType);
            string modelPrimaryKeyList = modelList
                .Select(model => (int)getModelPrimaryKeyMethod.Invoke(model, null))
                .OrderBy(i => i).JoinToString(", ");

            foreach (var hasManyAttr in hasManyAttrList)
            {
                var context = new SetHasManyContext(
                    hasManyAttr,
                    modelType,
                    modelList,
                    modelPrimaryKeyList,
                    getModelPrimaryKeyMethod);

                string selectQueryBase = string.Format(
                    "SELECT * FROM [{0}] WHERE [{1}] = ",
                    hasManyAttr.ReferenceType.Name,
                    hasManyAttr.ForeignKey);

                foreach (var model in modelList)
                {
                    //// SQL文を作る
                    // SELECT * FROM hasManyTable WHERE foreignKey = referenceKey
                    int modelReferenceKey = (int)context.GetReferenceKeyMethod.Invoke(model, null);
                    string selectQuery = selectQueryBase + modelReferenceKey;

                    //// SQLを発行するLazyObjectの作成
                    var lazyObj = new Lazy<object>(() =>
                    {
                        ILookup<int, object> hasManyLookup = ORMapInternal<object>(
                            null, connectionString, selectQuery, hasManyAttr.ReferenceType,
                            1, 0,
                            hasManyAttr.InverseBelongsToPropertyName, modelList,
                            null, null
                            ).ToLookup(
                            hasManyObj => (int)context.GetForeginKeyMethod.Invoke(hasManyObj, null),
                            hasManyObj =>
                            {
                                object loaded = null;

                                if (hasManyAttr.Property.Name == loadedHasManyPropertyName &&
                                    loadedHasManyObjDict.TryGetValue((int)context.GetPrimaryKeyMethod.Invoke(hasManyObj, null), out loaded))
                                {
                                    return loaded;
                                }
                                else
                                {
                                    return hasManyObj;
                                }
                            });

                        int referenceKey = (int)context.GetReferenceKeyMethod.Invoke(model, null);

                        // 単にhasManyLookup[referenceKey].ToList()を返そうとすると
                        // List<object>なので返せない。（実態はList<T>だが）
                        // なので一度セッターでセットし、ゲッターで値を再取得しreturnする。
                        context.SetObjListToHasManyProp(model, hasManyLookup[referenceKey].ToList());
                        return ((DBSimpleModel)model).LazyLoaderDict[hasManyAttr.Property.Name].Value;
                     });

                    ((DBSimpleModel)model).LazyLoaderDict[hasManyAttr.Property.Name] = lazyObj;
                }
            }
        }

        /// <summary>
        /// BelongsToプロパティの遅延読み込み用メソッドを作成しモデルにセットします
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="modelList">対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedBelongsToObj">読み込み済みプロパティのモデル</param>
        private static void SetLazyObjToBelongsToProp(
            string connectionString,
            IEnumerable<object> modelList,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedBelongsToObj
            )
        {
            // モデルのリストが空の時はreturn
            if (modelList.IsEmpty())
                return;

            Type modelType = modelList.First().GetType();
            List<BelongsToAttribute> belongsToAttrList = PropertyUtil.GetBelongsToAttrList(modelType);

            // BelongsTo属性のついたプロパティが無い時はreturn
            if (belongsToAttrList.IsEmpty())
                return;

            foreach (var belongsToAttr in belongsToAttrList)
            {
                var context = new SetBelongsToContext(
                    belongsToAttr,
                    modelType,
                    modelList);

                if (belongsToAttr.Property.Name == loadedBelongsToPropertyName)
                {
                    Dictionary<int, object> loadedBelongsToDict = loadedBelongsToObj.ToDictionary(
                        belongsToObj => (int)context.GetReferenceKeyMethod.Invoke(belongsToObj, null));

                    //// BelongsToプロパティに読み込み済みインスタンスをセットしていく
                    foreach (var model in modelList)
                    {
                        int foreignKey = (int)context.GetForeignKeyMethod.Invoke(model, null);

                        // ここ落としたほうがいいか？（外部キーに対応するbelongsToレコードがない場合）
                        if (loadedBelongsToDict.ContainsKey(foreignKey))
                        {
                            context.SetBelongsTo(model, loadedBelongsToDict[foreignKey]);
                        }
                        else
                        {
                            Debug.WriteLine($"foreign key value {foreignKey} was not found on {modelType}.{context.ReferenceKeyPropName}");
                        }
                    }
                }
                else
                {
                    string selectQueryBase = string.Format(
                        "SELECT * FROM [{0}] WHERE [{1}] = ",
                        belongsToAttr.ReferenceType.Name,
                        context.ReferenceKeyPropName);

                    foreach (var model in modelList)
                    {
                        //// SQL文を作る
                        // SELECT * FROM belongsToTable WHERE referenceKey = belongsToObjFK
                        int modelFK = (int)context.GetForeignKeyMethod.Invoke(model, null);
                        string selectQuery = selectQueryBase + modelFK;

                        //// SQLを発行するLazyObjectの作成
                        var lazyLoader = new Lazy<object>(() =>
                            ORMapInternal<object>(
                                null, connectionString, selectQuery, belongsToAttr.ReferenceType,
                                1, 0,
                                null, null,
                                belongsToAttr.InverseHasManyPropertyName, context.ModelDict
                            ).FirstOrDefault());

                        ((DBSimpleModel)model).LazyLoaderDict[belongsToAttr.Property.Name] = lazyLoader;
                    }
                }
            }
        }

        #endregion

        #region private class

        private class SetHasManyContext
        {
            public string ReferenceKeyPropName { get; set; }
            public Type HasManyType { get; set; }
            public MethodInfo SetHasManyMethod { get; set; }
            public MethodInfo GetForeginKeyMethod { get; set; }
            public MethodInfo GetPrimaryKeyMethod { get; set; }
            public MethodInfo GetReferenceKeyMethod { get; set; }
            public string ReferenceKeyList { get; set; }
            public Action<object, List<object>> SetObjListToHasManyProp { get; set; }

            public SetHasManyContext(
                HasManyAttribute hasManyAttr,
                Type modelType,
                IEnumerable<object> modelList,
                string modelPrimaryKeyList,
                MethodInfo getModelPrimaryKeyMethod)
            {
                // 外部キーの参照先キー名を取得。参照先キー名が未設定の場合はプライマリーキーを参照先キーとする。
                //（外部キーが必ずしもプライマリーキーを参照しているとは限らない）
                ReferenceKeyPropName = StringUtil.EmptyOr(
                    hasManyAttr.ReferenceKey,
                    hasManyAttr.InverseBelongsToAttribute.ReferenceKey);

                HasManyType = hasManyAttr.ReferenceType;
                SetHasManyMethod = hasManyAttr.Property.GetSetMethod();
                GetForeginKeyMethod = HasManyType.GetProperty(hasManyAttr.ForeignKey).GetGetMethod();
                GetPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(HasManyType);
                GetReferenceKeyMethod = ReferenceKeyPropName.IsEmpty() ?
                    getModelPrimaryKeyMethod :
                    modelType.GetProperty(ReferenceKeyPropName).GetGetMethod();

                ReferenceKeyList = ReferenceKeyPropName.IsEmpty() ?
                    modelPrimaryKeyList :
                    modelList
                        .Select(model => (int)GetReferenceKeyMethod.Invoke(model, null))
                        .OrderBy(i => i)
                        .JoinToString(", ");

                if (ReferenceKeyPropName.IsEmpty())
                {
                    ReferenceKeyPropName = PropertyUtil.GetPrimaryKeyName(modelType);
                }

                SetObjListToHasManyProp = FunctionGenerator.GenerateSetObjListToListPropFunction(
                    SetHasManyMethod, modelType, hasManyAttr.ReferenceType);
            }
        }

        private class SetBelongsToContext
        {
            public string ReferenceKeyPropName { get; set; }
            public Type BelongsToType { get; set; }
            public MethodInfo SetBelongsToMethod { get; set; }
            public MethodInfo GetForeignKeyMethod { get; set; }
            public MethodInfo GetPrimaryKeyMethod { get; set; }
            public MethodInfo GetReferenceKeyMethod { get; set; }
            public Dictionary<int, object> ModelDict { get; set; }
            public Action<object, object> SetBelongsTo { get; set; }

            public SetBelongsToContext(
                BelongsToAttribute belongsToAttr,
                Type modelType,
                IEnumerable<object> modelList)
            {
                ReferenceKeyPropName = StringUtil.EmptyOr(
                   belongsToAttr.ReferenceKey,
                   belongsToAttr.InverseHasManyAttribute.ReferenceKey);

                BelongsToType = belongsToAttr.ReferenceType;
                SetBelongsToMethod = belongsToAttr.Property.GetSetMethod();
                GetForeignKeyMethod = modelType.GetProperty(belongsToAttr.ForeignKey).GetGetMethod();
                GetPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(modelType);
                GetReferenceKeyMethod = ReferenceKeyPropName.IsEmpty() ?
                    PropertyUtil.GetGetPrimaryKeyMethod(BelongsToType) :
                    BelongsToType.GetProperty(ReferenceKeyPropName).GetGetMethod();

                if (ReferenceKeyPropName.IsEmpty())
                {
                    ReferenceKeyPropName = PropertyUtil.GetPrimaryKeyName(belongsToAttr.ReferenceType);
                }                

                ModelDict = modelList
                    // 左外部結合で補助テーブルを左に置き主キーを半ば意図的にnullにするケースがある。そういうレコードは飛ばす。
                    .Where(model => (int)GetPrimaryKeyMethod.Invoke(model, null) != 0)
                    .ToDictionary(model => (int)GetPrimaryKeyMethod.Invoke(model, null));

                SetBelongsTo = FunctionGenerator.GenerateSetObjToPropFunction(
                    SetBelongsToMethod, modelType, belongsToAttr.ReferenceType);
            }
        }

        #endregion
    }
}
