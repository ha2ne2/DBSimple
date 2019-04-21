using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Ha2ne2.DBSimple.Util
{
    public class MyTimer : IDisposable
    {
        public string Title { get; set; }
        public string Body { get; set; }
        public int Indent { get; set; }
        private Stopwatch StopWatch { get; set; }

        /// <summary>
        /// 時間の測定をします。
        /// using構文で使われることを想定しています。
        /// usingブロックを抜けるときに、そのブロックにかかった実行時間をデバッグ出力に出力します。
        /// 例）using (new MyTimer("処理名", "詳細", 2) { ... }
        /// </summary>
        /// <param name="title">処理の名前</param>
        /// <param name="body">処理の詳細</param>
        /// <param name="indent">インデントのレベル</param>
        public MyTimer(string title, string body = "", int indent = 0)
        {
            Title = title;
            Body = body;
            Indent = indent;
            StopWatch = new Stopwatch();
            StopWatch.Start();
        }

        public void Dispose()
        {
            // GetFrameの引数は0だとこのメソッド、1だとusingブロックを含むメソッド、
            // 2だとusingブロックを含むメソッドを呼び出したメソッドを示す。
            // 欲しい情報は2なので2を指定している。
            MethodBase caller = new StackTrace().GetFrame(2).GetMethod();
            string callerClassName = caller.ReflectedType.Name;
            string callerName = callerClassName + "." + caller.Name;

            StopWatch.Stop();
            Debug.WriteLine(string.Format(
                "{0}[{1,-10}] [{2,-30}] ({3,3}ms) {4}",
                new string(' ', Indent * 2),
                Title,
                callerName,
                StopWatch.ElapsedMilliseconds,
                Body));
        }
    }
}
