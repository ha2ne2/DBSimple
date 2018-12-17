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
                Stopwatch sw = new Stopwatch();
                List<TModel> modelList = new List<TModel>();

                // コマンドの組み立て
                command.Connection = tx.Connection;
                command.Transaction = tx;
                command.CommandText = selectQuery;

                // SQLの実行
                using (SqlDataReader rdr = command.ExecuteReader())
                {
                    Func<SqlDataReader, TModel> map = FunctionGenerator.GenerateMapFunction<TModel>(rdr);
                    while (rdr.Read())
                    {
                        modelList.Add(map(rdr));
                    }
                }
                sw.Stop();
                Console.WriteLine(string.Format(
                    "[{0,-10}] ({1,3}ms)",
                    "SimpleMap", sw.ElapsedMilliseconds));
                return modelList;
            }
        }


        /// <summary>
        /// ORマップをします
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="tx"></param>
        /// <param name="selectQuery"></param>
        /// <param name="preloadDepth"></param>
        /// <returns></returns>
        public static List<TModel> ORMap<TModel>(
            string connectionString,
            SqlTransaction tx,
            string selectQuery,
            int preloadDepth = 1
            )
            where TModel : class
        {
            return ORMapInternal<TModel>(connectionString, tx, selectQuery.Shrink(), typeof(TModel), preloadDepth, 0, null, null, null,null);
        }

        /// <summary>
        /// ORマップをします
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="connectionString"></param>
        /// <param name="selectQuery"></param>
        /// <param name="preloadDepth"></param>
        /// <returns></returns>
        public static List<TModel> ORMap<TModel>(
            string connectionString,
            string selectQuery,
            int preloadDepth = 1
            )
            where TModel : class
        {
            return CommonUtil.MeasureTime("ORMap", "TOTAL ELAPSED TIME", 0, () =>
            {
                // Make sure we can always go to the catch block, 
                // so we can set the latency mode back to `oldMode`
                GCLatencyMode oldMode = GCSettings.LatencyMode;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    GCSettings.LatencyMode = GCLatencyMode.LowLatency;
                    using (var connection = new SqlConnection(connectionString))
                    {
                        // データベースと接続
                        connection.Open();
                        using (var tx = connection.BeginTransaction())
                        {
                            return ORMap<TModel>(connectionString, tx, selectQuery, preloadDepth);
                        }
                    }
                }
                finally
                {
                    // ALWAYS set the latency mode back
                    GCSettings.LatencyMode = oldMode;
                }
            });
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
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ（出力のインデントに使う）</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みBelongsToプロパティの名前</param>
        /// <param name="loadedParents">読み込み済みBelongsToプロパティのモデルのリスト</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みHasManyプロパティの名前</param>
        /// <param name="loadedChildren">読み込み済みHasManyプロパティのモデルのディクショナリ</param>
        /// <returns></returns>
        private static List<TModel> ORMapInternal<TModel>(
            string connectionString,
            SqlTransaction tx,
            string selectQuery,
            Type typeofModel,
            int preloadDepth,
            int currentDepth,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedParents,
            string loadedHasManyPropertyName,
            Dictionary<int,object> loadedChildren
            )
            where TModel : class
        {
            using (var command = new SqlCommand())
            {
                List<TModel> modelList = new List<TModel>();

                if (tx == null)
                {
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
                                loadedParents,
                                loadedHasManyPropertyName,
                                loadedChildren);
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
                                FunctionGenerator.GenerateMapFunction<TModel>(rdr, typeofModel);
                            while (rdr.Read())
                            {
                                modelList.Add(map(rdr));
                            }
                        }
                    });

                    if (modelList.IsEmpty())
                    {

                    }
                    else if (preloadDepth > 0)
                    {
                        PreloadBelongsTo(connectionString, tx, modelList.AsEnumerable(), preloadDepth, currentDepth,
                                         loadedBelongsToPropertyName, loadedParents);
                        PreloadHasMany(connectionString, tx, modelList.AsEnumerable(), preloadDepth, currentDepth,
                                       loadedHasManyPropertyName, loadedChildren);
                    }
                    else
                    {
                        SetLazyObjToBelongsTo(connectionString, modelList.AsEnumerable(),
                                              loadedBelongsToPropertyName, loadedParents);
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
        /// <param name="parents">読み込み対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedHasManyPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedChildren">読み込み済みプロパティのモデル</param>
        private static void PreloadHasMany(
            string connectionString,
            SqlTransaction tx,
            IEnumerable<object> parents,
            int preloadDepth,
            int currentDepth,
            string loadedHasManyPropertyName,
            Dictionary<int,object> loadedChildren
            )
        {
            // 親のモデルが空の時はreturn
            if (parents.IsEmpty())
                return;

            Type parentType = parents.First().GetType();
            List<HasManyAttribute> hasManyAttrList = PropertyUtil.GetHasManyAttrList(parentType);

            // HasMany属性のついたプロパティが無い時はreturn
            if (hasManyAttrList.IsEmpty())
                return;

            // 親のモデルをプライマリーキーを集めて
            // SELECT * FROM child_table WHERE childFK IN (1,2,3) の 1,2,3の部分を作る
            // TODO プライマリーキープロパティ名がテーブルのプライマリーキー名という前提
            MethodInfo getPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(parentType);
            string primaryKeyList = parents
                .Select(model => (int)getPrimaryKeyMethod.Invoke(model, null))
                .Distinct().OrderBy(i => i).JoinToString(", ");

            foreach (var hasManyAttr in hasManyAttrList)
            {
                #region 下準備

                MethodInfo setHasManyMethod = hasManyAttr.Property.GetSetMethod();
                Action<object, List<object>> setObjListToHasManyProp = FunctionGenerator.GenerateSetObjListToListPropFunction(
                        setHasManyMethod, parentType, hasManyAttr.ChildType);
                MethodInfo getChildPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(hasManyAttr.ChildType);
                MethodInfo getChildForeginKeyMethod = hasManyAttr.ChildType.GetProperty(hasManyAttr.ForeignKey).GetGetMethod();
                string parentKeyPropName = hasManyAttr.InverseBelongsToAttribute.ParentKey;
                string parentKeyList = null;
                MethodInfo getParentKeyMethod = null;

                if (parentKeyPropName.IsEmpty())
                {
                    // parentKeyの指定がない場合はプライマリーキーのリストを使う。
                    parentKeyList = primaryKeyList;
                }
                else
                {
                    getParentKeyMethod = parentType
                        .GetProperty(parentKeyPropName)
                        .GetGetMethod();

                    parentKeyList = parents
                        .Select(model => (int)getParentKeyMethod.Invoke(model, null))
                        .Distinct().OrderBy(i => i).JoinToString(", ");
                }

                #endregion

                //// SQL文を作る
                string selectQuery = string.Format(
                    "SELECT * FROM [{0}] WHERE [{1}] IN ({2})",
                    hasManyAttr.ChildType.Name,  // TODO クラス名がテーブル名という前提
                    hasManyAttr.ForeignKey, // TODO 外部キープロパティ名が外部キー名という前提
                    parentKeyList);                

                //// SQLを発行してchildのリストを作り、ForeignKeyをキーにしたLookupに変換する
                ILookup<int, object> childrenLookup = ORMapInternal<object>(
                    connectionString, tx, selectQuery, hasManyAttr.ChildType,
                    preloadDepth - 1, currentDepth + 1,
                    hasManyAttr.InverseBelongsToPropertyName, parents,
                    null, null
                    )
                    .ToLookup(
                    child =>　(int)getChildForeginKeyMethod.Invoke(child, null),
                    child =>
                    {
                        object inverceChild = null;
                        if (hasManyAttr.Property.Name == loadedHasManyPropertyName &&
                            loadedChildren.TryGetValue((int)getChildPrimaryKeyMethod.Invoke(child,null), out inverceChild))
                        {
                            return inverceChild;
                        }
                        else
                        {
                            return child;
                        }
                    });

                //// 親のHasManyプロパティに子のリストをセットしていく
                foreach (var parent in parents)
                {
                    int parentKey = parentKeyPropName.IsEmpty() ?
                        (int)getPrimaryKeyMethod.Invoke(parent, null):
                        (int)getParentKeyMethod.Invoke(parent, null);

                    setObjListToHasManyProp(parent, childrenLookup[parentKey].ToList());
                }
            }              
        }


        /// <summary>
        /// IEnumerable＜object＞型にキャストされたモデルのリストの
        /// BelongsTo属性のついたプロパティをPreloadします。
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="children">対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedParents">読み込み済みプロパティのモデル</param>
        private static void PreloadBelongsTo(
            string connectionString,
            SqlTransaction tx,
            IEnumerable<object> children,
            int preloadDepth,
            int currentDepth,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedParents
            )
        {
            // 子のモデルが空の時はreturn
            if (children.IsEmpty())
                return;

            Type childType = children.First().GetType();
            List<BelongsToAttribute> belongsToAttrList = PropertyUtil.GetBelongsToAttrList(childType);

            foreach (var belongsToAttr in belongsToAttrList)
            {
                #region 下準備

                MethodInfo setBelongsToMethod = belongsToAttr.Property.GetSetMethod();
                Action<object, object> setBelongsTo = FunctionGenerator.GenerateSetObjToPropFunction(
                    setBelongsToMethod, childType, belongsToAttr.ParentType);
                MethodInfo getChildForeignKeyMethod = childType.GetProperty(belongsToAttr.ForeignKey).GetGetMethod();
                MethodInfo getChildPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(childType);
                Dictionary<int, object> childDict = children
                    .ToDictionary(child => (int)getChildPrimaryKeyMethod.Invoke(child,null));
                Type parentType = belongsToAttr.ParentType;
                string parentKeyPropertyName = StringUtil.EmptyOr(
                    belongsToAttr.ParentKey, PropertyUtil.GetPrimaryKeyName(parentType));
                MethodInfo getParentKeyMethod = parentType
                    .GetProperty(parentKeyPropertyName)
                    .GetGetMethod();

                #endregion

                Dictionary<int, object> parentDict = null;

                if (belongsToAttr.Property.Name == loadedBelongsToPropertyName)
                {
                    parentDict = loadedParents
                        .ToDictionary(parent => (int)getParentKeyMethod.Invoke(parent, null));
                }
                else
                {
                    //// SQL文を作る
                    // SELECT * FROM parent_table WHERE parentPK IN (1,2,3) の 1,2,3の部分を作る
                    string childForeignKeyList = children
                        .Select(child => (int)getChildForeignKeyMethod.Invoke(child, null))
                        .Distinct().OrderBy(i => i).JoinToString(", ");

                    string selectQuery = string.Format(
                        "SELECT * FROM [{0}] WHERE [{1}] IN ({2})",
                        belongsToAttr.ParentType.Name, // TODO 子のクラス名が子のテーブル名という前提
                        parentKeyPropertyName,         // TODO 親のキーのプロパティ名がキー名という前提
                        childForeignKeyList);

                    //// SQLを発行してparentのリストを作り、parentKeyをキーにしたDictionaryに変換する
                    parentDict = ORMapInternal<object>(
                        connectionString, tx, selectQuery, belongsToAttr.ParentType,
                        preloadDepth - 1, currentDepth + 1,
                        null, null,
                        belongsToAttr.InverseHasManyPropertyName, childDict
                        )
                        .ToDictionary(parent => (int)getParentKeyMethod.Invoke(parent, null));
                }

                //// 子のBelongsToプロパティに親をセットしていく
                foreach (var child in children)
                {
                    int childForeignKey = (int)getChildForeignKeyMethod.Invoke(child, null);

                    // ここ落としたほうがいいか？（外部キーに対応する親レコードがない場合）
                    if (parentDict.ContainsKey(childForeignKey))
                    {
                        setBelongsTo(child, parentDict[childForeignKey]);
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
        //    // 親のモデルが空の時はreturn
        //    if (parents.IsEmpty())
        //        return;

        //    Type parentType = parents.First().GetType();
        //    List<HasManyAttribute> hasManyAttrList = PropertyUtil.GetHasManyAttrList(parentType);

        //    // HasMany属性のついたプロパティが無い時はreturn
        //    if (hasManyAttrList.IsEmpty())
        //        return;

        //    foreach (var hasManyAttr in hasManyAttrList)
        //    {
        //        #region 下準備

        //        MethodInfo setHasManyMethod = hasManyAttr.Property.GetSetMethod();
        //        Action<object, List<object>> setObjListToHasManyProp = FunctionGenerator.GenerateSetObjListToListPropFunction(
        //                setHasManyMethod, parentType, hasManyAttr.ChildType);
        //        MethodInfo getChildPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(hasManyAttr.ChildType);
        //        MethodInfo getChildForeginKeyMethod = hasManyAttr.ChildType.GetProperty(hasManyAttr.ForeignKey).GetGetMethod();
        //        string parentKeyPropertyName = StringUtil.EmptyOr(
        //            hasManyAttr.InverseBelongsToAttribute.ParentKey, PropertyUtil.GetPrimaryKeyName(parentType));
        //        MethodInfo getParentKeyMethod = parentType
        //            .GetProperty(parentKeyPropertyName)
        //            .GetGetMethod();

        //        #endregion

        //        //// SQL文を作る
        //        string selectQueryBase = string.Format(
        //            "SELECT * FROM [{0}] WHERE [{1}] = ",
        //            hasManyAttr.ChildType.Name,  // TODO クラス名がテーブル名という前提
        //            hasManyAttr.ForeignKey       // TODO 外部キープロパティ名が外部キー名という前提
        //            );

        //        foreach (var parent in parents)
        //        {
        //            //// SQL文を作る
        //            // SELECT * FROM child_Table WHERE childFK = parntKey
        //            int parentKey = (int)getParentKeyMethod.Invoke(parent, null);
        //            string selectQuery = selectQueryBase + parentKey;

        //            //// SQLを発行するLazyObjectの作成
        //            var lazyObj = new Lazy<object>(() =>
        //                ORMapInternal<object>(
        //                    connectionString, null, selectQuery, hasManyAttr.ChildType,
        //                    1, 0,
        //                    hasManyAttr.InverseBelongsToPropertyName, parents,
        //                    null, null
        //                    )
        //                    .Select(child =>
        //                    {
        //                        object inverseChild = null;
        //                        if (hasManyAttr.Property.Name == loadedHasManyPropertyName &&
        //                            loadedChildren.TryGetValue((int)getChildPrimaryKeyMethod.Invoke(child, null),
        //                                out inverseChild))
        //                        {
        //                            return inverseChild;
        //                        }
        //                        else
        //                        {
        //                            return child;
        //                        }
        //                    }).ToList());

        //            ((DBSimpleModel)parent).Dict[hasManyAttr.Property.Name] = lazyObj;
        //        }
        //    }
        //}

        /// <summary>
        /// Lazyを仕込む
        /// 
        /// </summary>
        /// <param name="tx">SQLトランザクション</param>
        /// <param name="children">対象のモデル</param>
        /// <param name="preloadDepth">preloadする深さ</param>
        /// <param name="currentDepth">現在の深さ</param>
        /// <param name="loadedBelongsToPropertyName">読み込み済みプロパティの名前</param>
        /// <param name="loadedParents">読み込み済みプロパティのモデル</param>
        private static void SetLazyObjToBelongsTo(
            string connectionString,
            IEnumerable<object> children,
            string loadedBelongsToPropertyName,
            IEnumerable<object> loadedParents
            )
        {
            // 子のモデルが空の時はreturn
            if (children.IsEmpty())
                return;

            Type childType = children.First().GetType();
            List<BelongsToAttribute> belongsToAttrList = PropertyUtil.GetBelongsToAttrList(childType);

            // BelongsTo属性のついたプロパティが無い時はreturn
            if (belongsToAttrList.IsEmpty())
                return;

            foreach (var belongsToAttr in belongsToAttrList)
            {
                #region 下準備

                MethodInfo setBelongsToMethod = belongsToAttr.Property.GetSetMethod();
                Action<object, object> setBelongsTo = FunctionGenerator.GenerateSetObjToPropFunction(
                    setBelongsToMethod, childType, belongsToAttr.ParentType);
                MethodInfo getChildForeignKeyMethod = childType.GetProperty(belongsToAttr.ForeignKey).GetGetMethod();
                MethodInfo getChildPrimaryKeyMethod = PropertyUtil.GetGetPrimaryKeyMethod(childType);
                Dictionary<int, object> childDict = children
                    .ToDictionary(child => (int)getChildPrimaryKeyMethod.Invoke(child, null));
                Type parentType = belongsToAttr.ParentType;
                string parentKeyPropertyName = StringUtil.EmptyOr(
                    belongsToAttr.ParentKey, PropertyUtil.GetPrimaryKeyName(parentType));
                MethodInfo getParentKeyMethod = parentType
                    .GetProperty(parentKeyPropertyName)
                    .GetGetMethod();

                #endregion

                Dictionary<int, object> parentDict = null;

                if (belongsToAttr.Property.Name == loadedBelongsToPropertyName)
                {
                    parentDict = loadedParents
                        .ToDictionary(parent => (int)getParentKeyMethod.Invoke(parent, null));

                    //// 子のBelongsToプロパティに親をセットしていく
                    foreach (var child in children)
                    {
                        int childForeignKey = (int)getChildForeignKeyMethod.Invoke(child, null);

                        // ここ落としたほうがいいか？（外部キーに対応する親レコードがない場合）
                        if (parentDict.ContainsKey(childForeignKey))
                        {
                            setBelongsTo(child, parentDict[childForeignKey]);
                        }
                    }
                }
                else
                {
                    string selectQueryBase = string.Format(
                        "SELECT * FROM [{0}] WHERE [{1}] = ",
                        belongsToAttr.ParentType.Name,
                        parentKeyPropertyName);

                    foreach (var child in children)
                    {
                        //// SQL文を作る
                        // SELECT * FROM parent_Table WHERE parntKey = childFK
                        int childFK = (int)getChildForeignKeyMethod.Invoke(child, null);
                        string selectQuery = selectQueryBase + childFK;

                        //// SQLを発行するLazyObjectの作成
                        var lazyObj = new Lazy<object>(() =>
                            ORMapInternal<object>(
                                connectionString, null, selectQuery, belongsToAttr.ParentType,
                                1,0,
                                null, null,
                                belongsToAttr.InverseHasManyPropertyName, childDict
                            ).FirstOrDefault());

                        ((DBSimpleModel)child).Dict[belongsToAttr.Property.Name] = lazyObj;
                    }
                }
            }
        }
    }
}
