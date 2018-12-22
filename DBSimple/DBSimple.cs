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
        /// シンプルなマップ
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="selectQuery"></param>
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
        /// シンプルなマップ
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="tx"></param>
        /// <param name="selectQuery"></param>
        /// <returns></returns>
        public static List<TModel> SimpleMap<TModel>(SqlTransaction tx, string selectQuery)
        {
            using (var command = new SqlCommand())
            {
                List<TModel> modelList = new List<TModel>();

                // コマンドの組み立て
                command.Connection = tx.Connection;
                command.Transaction = tx;
                command.CommandText = selectQuery;

                // SQLの実行
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
        /// ORマップをします
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="selectQuery"></param>
        /// <param name="preloadDepth">プリロードする深さ</param>
        /// <returns></returns>
        public static List<TModel> ORMap<TModel>(
            string connectionString,
            string selectQuery,
            int preloadDepth = 1
            )
            where TModel : class
        {
            using (var connection = new SqlConnection(connectionString))
            {
                // データベースと接続
                connection.Open();
                using (var tx = connection.BeginTransaction())
                {
                    MethodBase caller = new StackTrace().GetFrame(1).GetMethod();
                    string callerClassName = caller.ReflectedType.Name;
                    string callerName = callerClassName + "." + caller.Name;

                    var sw = new Stopwatch();
                    sw.Start();
                    var result = ORMapInternal<TModel>(connectionString, tx, selectQuery, typeof(TModel), preloadDepth, 0, null, null, null, null); ;
                    sw.Stop();

                    string title = "ORMap";
                    string body = "TOTAL ELAPSED TIME";
                    int indent = 0;

                    Debug.WriteLine(string.Format(
                        "{0}[{1,-10}] [{2,-30}] ({3,3}ms) {4}",
                        new string(' ', indent * 2),
                        title,
                        callerName,
                        sw.ElapsedMilliseconds,
                        body));

                    return result;
                }
            }
        }

        /// <summary>
        /// ORマップをします
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="tx"></param>
        /// <param name="selectQuery"></param>
        /// <param name="preloadDepth">プリロードする深さ</param>
        /// <returns></returns>
        public static List<TModel> ORMap<TModel>(
            string connectionString,
            SqlTransaction tx,
            string selectQuery,
            int preloadDepth = 1
            )
            where TModel : class
        {
            MethodBase caller = new StackTrace().GetFrame(1).GetMethod();
            string callerClassName = caller.ReflectedType.Name;
            string callerName = callerClassName + "." + caller.Name;

            var sw = new Stopwatch();
            sw.Start();
            var result = ORMapInternal<TModel>(connectionString, tx, selectQuery, typeof(TModel), preloadDepth, 0, null, null, null, null); ;
            sw.Stop();

            string title = "ORMap";
            string body = "TOTAL ELAPSED TIME";
            int indent = 0;

            Debug.WriteLine(string.Format(
                "{0}[{1,-10}] [{2,-30}] ({3,3}ms) {4}",
                new string(' ', indent * 2),
                title,
                callerName,
                sw.ElapsedMilliseconds,
                body));

            return result;
        }

        #endregion

        #region private method

        /// <summary>
        /// 再帰的にORマップします。
        /// </summary>
        /// <typeparam name="TModel">モデルの型</typeparam>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="selectQuery">セレクトクエリー</param>
        /// <param name="typeofModel">モデルの実際の型</param>
        /// <param name="preloadDepth">プリロードする深さ</param>
        /// <param name="currentDepth">現在の深さ（出力のインデントに使う）</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みBelongsToプロパティの名前</param>
        /// <param name="loadedBelongsToObj">読み込み済みBelongsToプロパティのモデルのリスト</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みHasManyプロパティの名前</param>
        /// <param name="loadedHasManyObj">読み込み済みHasManyプロパティのモデルのディクショナリ(キーはHasManyインスタンスのFK)</param>
        /// <returns></returns>
        private static List<TModel> ORMapInternal<TModel>(
            string connectionString,
            SqlTransaction tx,
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
            using (var command = new SqlCommand())
            {
                List<TModel> modelList = new List<TModel>();

                if (tx == null)
                {
                    // トランザクションがnullならトランザクションを開始して再起
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // データベースと接続
                        connection.Open();
                        using (tx = connection.BeginTransaction())
                        {
                            return ORMapInternal<TModel>(
                                connectionString,
                                tx,
                                selectQuery,
                                typeofModel,
                                preloadDepth,
                                currentDepth,
                                loadedBelongsToPropertyName,
                                loadedBelongsToObj,
                                loadedHasManyPropertyName,
                                loadedHasManyObj);
                        }
                    }
                }
                else
                {
                    // コマンドの組み立て
                    command.Connection = tx.Connection;
                    command.Transaction = tx;
                    command.CommandText = selectQuery;

                    CommonUtil.MeasureTime(typeofModel.Name, selectQuery, currentDepth, () =>
                    {
                        // SQLの実行
                        using (SqlDataReader rdr = command.ExecuteReader())
                        {
                            Func<SqlDataReader, TModel> map =
                                FunctionGenerator.GenerateMapFunction<TModel>(selectQuery, rdr, typeofModel);
                            while (rdr.Read())
                            {
                                modelList.Add(map(rdr));
                            }
                        }
                    });

                    if (modelList.IsEmpty())
                    {
                        // nop
                    }
                    else if (preloadDepth > 0)
                    {
                        PreloadHasMany(connectionString, tx, modelList.AsEnumerable(), preloadDepth, currentDepth,
                                       loadedHasManyPropertyName, loadedHasManyObj);
                        PreloadBelongsTo(connectionString, tx, modelList.AsEnumerable(), preloadDepth, currentDepth,
                                         loadedBelongsToPropertyName, loadedBelongsToObj);
                    }
                    else
                    {
                        SetLazyObjToBelongsTo(connectionString, modelList.AsEnumerable(),
                                              loadedBelongsToPropertyName, loadedBelongsToObj);
                        //SetLazyObjToHasMany(connectionString, modelList.AsEnumerable(),
                        //                    loadedHasManyPropertyName, loadedChildren);
                    }

                    return modelList;
                }
            }
        }

        /// <summary>
        /// IEnumerable＜object＞型にキャストされたモデルのリストの
        /// HasMany属性のついたプロパティをPreloadします。
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="models">読み込み対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedHasManyObjDict">読み込み済みプロパティのインスタンスのディクショナリ</param>
        private static void PreloadHasMany(
            string connectionString,
            SqlTransaction tx,
            IEnumerable<object> models,
            int preloadDepth,
            int currentDepth,
            string loadedHasManyPropertyName,
            Dictionary<int,object> loadedHasManyObjDict
            )
        {
            // 親のモデルが空の時はreturn
            if (models.IsEmpty())
                return;

            Type modelType = models.First().GetType();
            List<HasManyAttribute> hasManyAttrList = PropertyUtil.GetHasManyAttrList(modelType);

            // HasMany属性のついたプロパティが無い時はreturn
            if (hasManyAttrList.IsEmpty())
                return;

            // モデルをプライマリーキーを集めて
            // SELECT * FROM has_many_table WHERE has_many_FK IN (1,2,3) の 1,2,3の部分を作る
            // TODO プライマリーキープロパティ名がテーブルのプライマリーキー名という前提
            MethodInfo getPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(modelType);
            string primaryKeyList = models
                .Select(model => (int)getPrimaryKeyMethod.Invoke(model, null))
                .Distinct().OrderBy(i => i).JoinToString(", ");

            foreach (var hasManyAttr in hasManyAttrList)
            {
                #region 下準備

                string referenceKeyPropName = hasManyAttr.InverseBelongsToAttribute.ReferenceKey;

                MethodInfo setHasManyMethod = hasManyAttr.Property.GetSetMethod();
                MethodInfo getHasManyObjForeginKeyMethod = hasManyAttr.Type.GetProperty(hasManyAttr.ForeignKey).GetGetMethod();
                MethodInfo getHasManyObjPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(hasManyAttr.Type);
                MethodInfo getReferenceKeyMethod = referenceKeyPropName.IsEmpty() ?
                    getPrimaryKeyMethod :
                    modelType.GetProperty(referenceKeyPropName).GetGetMethod();

                string referenceKeyList = referenceKeyPropName.IsEmpty() ?
                    primaryKeyList :
                    models
                        .Select(model => (int)getReferenceKeyMethod.Invoke(model, null))
                        .Distinct().OrderBy(i => i).JoinToString(", ");

                Action<object, List<object>> setObjListToHasManyProp = FunctionGenerator.GenerateSetObjListToListPropFunction(
                    setHasManyMethod, modelType, hasManyAttr.Type);

                #endregion

                //// SQL文を作る
                string selectQuery = string.Format(
                    "SELECT * FROM [{0}] WHERE [{1}] IN ({2})",
                    hasManyAttr.Type.Name,  // TODO クラス名がテーブル名という前提
                    hasManyAttr.ForeignKey, // TODO 外部キープロパティ名が外部キー名という前提
                    referenceKeyList);

                //// SQLを発行してHasManyのリストを作り、ForeignKeyをキーにしたLookupに変換する
                ILookup<int, object> hasManyLookup = ORMapInternal<object>(
                    connectionString, tx, selectQuery, hasManyAttr.Type,
                    preloadDepth - 1, currentDepth + 1,
                    hasManyAttr.InverseBelongsToPropertyName, models,
                    null, null
                    )
                    .ToLookup(
                    hasManyObj =>　(int)getHasManyObjForeginKeyMethod.Invoke(hasManyObj, null),
                    hasManyObj =>
                    {
                        object loaded = null;

                        if (hasManyAttr.Property.Name == loadedHasManyPropertyName &&
                            loadedHasManyObjDict.TryGetValue((int)getHasManyObjPrimaryKeyMethod.Invoke(hasManyObj,null), out loaded))
                        {
                            return loaded;
                        }
                        else
                        {
                            return hasManyObj;
                        }
                    });

                //// HasManyプロパティにインスタンスをセットしていく
                foreach (var model in models)
                {
                    int referenceKey = (int)getReferenceKeyMethod.Invoke(model, null);
                    setObjListToHasManyProp(model, hasManyLookup[referenceKey].ToList());
                }
            }              
        }

        /// <summary>
        /// IEnumerable＜object＞型にキャストされたモデルのリストの
        /// BelongsTo属性のついたプロパティをPreloadします。
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="models">対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedBelongsToObj">読み込み済みプロパティのモデル</param>
        private static void PreloadBelongsTo(
            string connectionString,
            SqlTransaction tx,
            IEnumerable<object> models,
            int preloadDepth,
            int currentDepth,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedBelongsToObj
            )
        {
            // 子のモデルが空の時はreturn
            if (models.IsEmpty())
                return;

            Type modelType = models.First().GetType();
            List<BelongsToAttribute> belongsToAttrList = PropertyUtil.GetBelongsToAttrList(modelType);

            foreach (var belongsToAttr in belongsToAttrList)
            {
                #region 下準備

                Type belongsToType = belongsToAttr.Type;
                string belongsToObjReferenceKeyPropertyName = StringUtil.EmptyOr(
                    belongsToAttr.ReferenceKey, PropertyUtil.GetPrimaryKeyName(belongsToType));
                MethodInfo setBelongsToMethod = belongsToAttr.Property.GetSetMethod();
                MethodInfo getForeignKeyMethod = modelType.GetProperty(belongsToAttr.ForeignKey).GetGetMethod();
                MethodInfo getPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(modelType);
                MethodInfo getBelongsToObjReferenceKeyMethod = belongsToType
                    .GetProperty(belongsToObjReferenceKeyPropertyName)
                    .GetGetMethod();
                Dictionary<int, object> modelDict = models
                    // 左外部結合で補助テーブルを左に置き主キーを半ば意図的にnullにするケースがある。そういうレコードは飛ばす。
                    .Where(model => (int)getPrimaryKeyMethod.Invoke(model, null) != 0)
                    .ToDictionary(model => (int)getPrimaryKeyMethod.Invoke(model,null));
                Action<object, object> setBelongsTo = FunctionGenerator.GenerateSetObjToPropFunction(
                    setBelongsToMethod, modelType, belongsToAttr.Type);

                #endregion

                Dictionary<int, object> belongsToDict = null;

                if (belongsToAttr.Property.Name == loadedBelongsToPropertyName)
                {
                    belongsToDict = loadedBelongsToObj.ToDictionary(
                        belongsToObj => (int)getBelongsToObjReferenceKeyMethod.Invoke(belongsToObj, null));
                }
                else
                {
                    //// SQL文を作る
                    // SELECT * FROM parent_table WHERE parentPK IN (1,2,3) の 1,2,3の部分を作る
                    string foreignKeyList = models
                        .Select(child => (int)getForeignKeyMethod.Invoke(child, null))
                        .Distinct().OrderBy(i => i).JoinToString(", ");

                    string selectQuery = string.Format(
                        "SELECT * FROM [{0}] WHERE [{1}] IN ({2})",
                        belongsToAttr.Type.Name,   // TODO BelongsToクラス名がテーブル名という前提
                        belongsToObjReferenceKeyPropertyName,  // TODO referenceKeyプロパティ名が参照先列名という前提
                        foreignKeyList);

                    //// SQLを発行してbelongsToのリストを作り、referenceKeyをキーにしたDictionaryに変換する
                    belongsToDict = ORMapInternal<object>(
                        connectionString, tx, selectQuery, belongsToAttr.Type,
                        preloadDepth - 1, currentDepth + 1,
                        null, null,
                        belongsToAttr.InverseHasManyPropertyName, modelDict
                        )
                        .ToDictionary(parent => (int)getBelongsToObjReferenceKeyMethod.Invoke(parent, null));
                }

                //// 子のBelongsToプロパティに親をセットしていく
                foreach (var model in models)
                {
                    int foreignKey = (int)getForeignKeyMethod.Invoke(model, null);

                    // ここ落としたほうがいいか？（外部キーに対応するbelongsToレコードがない場合）
                    if (belongsToDict.ContainsKey(foreignKey))
                    {
                        setBelongsTo(model, belongsToDict[foreignKey]);
                    }
                    else
                    {
                        Debug.WriteLine($"foreign key value {foreignKey} was not found on {modelType}.{belongsToObjReferenceKeyPropertyName}");
                    }
                }
            }
        }

        #endregion


        /// <summary>
        /// Lazyを仕込む（こいつがまだ未完成）
        /// 
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="parents">読み込み対象のモデル</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedChildren">読み込み済みプロパティのモデル</param>
        //private static void SetLazyObjToHasMany(
        //    string connectionString,
        //    IEnumerable<object> parents,
        //    string loadedHasManyPropertyName,
        //    Dictionary<int, object> loadedChildren
        //    )
        //{
        //}

        /// <summary>
        /// Lazyを仕込む
        /// 
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="models">対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedBelongsToObj">読み込み済みプロパティのモデル</param>
        private static void SetLazyObjToBelongsTo(
            string connectionString,
            IEnumerable<object> models,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedBelongsToObj
            )
        {
            // 子のモデルが空の時はreturn
            if (models.IsEmpty())
                return;

            Type modelType = models.First().GetType();
            List<BelongsToAttribute> belongsToAttrList = PropertyUtil.GetBelongsToAttrList(modelType);

            // BelongsTo属性のついたプロパティが無い時はreturn
            if (belongsToAttrList.IsEmpty())
                return;

            foreach (var belongsToAttr in belongsToAttrList)
            {
                #region 下準備

                Type belongsToType = belongsToAttr.Type;
                string belongsToObjReferenceKeyPropertyName = StringUtil.EmptyOr(
                    belongsToAttr.ReferenceKey, PropertyUtil.GetPrimaryKeyName(belongsToType));
                MethodInfo setBelongsToMethod = belongsToAttr.Property.GetSetMethod();
                MethodInfo getForeignKeyMethod = modelType.GetProperty(belongsToAttr.ForeignKey).GetGetMethod();
                MethodInfo getPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(modelType);
                MethodInfo getReferenceKeyMethod = belongsToType
                    .GetProperty(belongsToObjReferenceKeyPropertyName)
                    .GetGetMethod();
                Dictionary<int, object> modelDict = models
                    // 左外部結合で補助テーブルを左に置き主キーを半ば意図的にnullにするケースがある。そういうレコードは飛ばす。
                    .Where(model => (int)getPrimaryKeyMethod.Invoke(model, null) != 0)
                    .ToDictionary(model => (int)getPrimaryKeyMethod.Invoke(model, null));
                Action<object, object> setBelongsTo = FunctionGenerator.GenerateSetObjToPropFunction(
                    setBelongsToMethod, modelType, belongsToAttr.Type);

                #endregion                

                if (belongsToAttr.Property.Name == loadedBelongsToPropertyName)
                {
                    Dictionary<int, object> loadedBelongsToDict = loadedBelongsToObj.ToDictionary(
                        belongsToObj => (int)getReferenceKeyMethod.Invoke(belongsToObj, null));

                    //// BelongsToプロパティに読み込み済みインスタンスをセットしていく
                    foreach (var model in models)
                    {
                        int foreignKey = (int)getForeignKeyMethod.Invoke(model, null);

                        // ここ落としたほうがいいか？（外部キーに対応するbelongsToレコードがない場合）
                        if (loadedBelongsToDict.ContainsKey(foreignKey))
                        {
                            setBelongsTo(model, loadedBelongsToDict[foreignKey]);
                        }
                        else
                        {
                            Debug.WriteLine($"foreign key value {foreignKey} was not found on {modelType}.{belongsToObjReferenceKeyPropertyName}");
                        }
                    }
                }
                else
                {
                    string selectQueryBase = string.Format(
                        "SELECT * FROM [{0}] WHERE [{1}] = ",
                        belongsToAttr.Type.Name,
                        belongsToObjReferenceKeyPropertyName);

                    foreach (var model in models)
                    {
                        //// SQL文を作る
                        // SELECT * FROM parent_Table WHERE referenceKey = childFK
                        int modelFK = (int)getForeignKeyMethod.Invoke(model, null);
                        string selectQuery = selectQueryBase + modelFK;

                        //// SQLを発行するLazyObjectの作成
                        var lazyObj = new Lazy<object>(() =>
                            ORMapInternal<object>(
                                connectionString, null, selectQuery, belongsToAttr.Type,
                                1,0,
                                null, null,
                                belongsToAttr.InverseHasManyPropertyName, modelDict
                            ).FirstOrDefault());

                        ((DBSimpleModel)model).Dict[belongsToAttr.Property.Name] = lazyObj;
                    }
                }
            }
        }
    }
}
