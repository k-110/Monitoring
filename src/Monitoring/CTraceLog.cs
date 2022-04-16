using System;
using System.IO;

namespace MyUtility
{
    #region ユーティリティで使用するログ処理
    /// <summary>
    ///  ユーティリティで使用するログ処理
    /// </summary>
    public static class MyUtilityLog
    {
        /// <summary>
        ///  ログ管理機能オブジェクト
        /// </summary>
        private static readonly CTraceLog UtilityTraceLog = new CTraceLog();

        /// <summary>
        ///  ログ処理
        /// </summary>
        /// <param name="log_text">ログする文字列</param>
        public static void Write(string log_text)
        {
            UtilityTraceLog.Write(log_text);
        }

        /// <summary>
        ///  開始をログする処理
        /// </summary>
        public static void LogBegin()
        {
            UtilityTraceLog.Write("-------------Begin--------------");
        }

        /// <summary>
        ///  終了をログする処理
        /// </summary>
        public static void LogEnd()
        {
            UtilityTraceLog.Write("-------------End----------------");
        }

        /// <summary>
        ///  最後に書き込みいたログファイルのフルパス
        /// </summary>
        public static string GetLogFilePath()
        {
            return UtilityTraceLog.LogFilePath;
        }
    }
    #endregion

    /// <summary>
    ///  ログ管理機能
    ///  ログはテキストファイル形式で扱う
    /// </summary>
    public class CTraceLog
    {
        /*------------------------*/
        /* リソース               */
        /*------------------------*/
        #region リソース
        /// <summary>
        ///  ログファイルを保存するディレクトリパス(デフォルトは実行ファイルのあるフォルダ内のlog)
        /// </summary>
        private readonly string LogDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "log");
        /// <summary>
        ///  ログファイル名(yyyyMMddはログファイルの作成日付に置き換わる)
        /// </summary>
        private readonly string FileName = "yyyyMMdd_TraceLog.log";
        /// <summary>
        ///  アクセスを排他するためのロックオブジェクト
        /// </summary>
        private readonly object LogLock = new object();
        /// <summary>
        ///  最後の書き込みしたログファイルのフルパス
        /// </summary>
        public string LogFilePath { get; private set; } = "";
        #endregion

        /*------------------------*/
        /* コンストラクタ等       */
        /*------------------------*/
        #region コンストラクタ等
        /// <summary>
        ///  ログ処理
        ///  (注)ファイルに書き込む際の文字コードはUTF-8
        ///  (注)書き込む文字列の先頭に日時、終端に改行を自動で付加する
        /// </summary>
        /// <param name="log_text">ログする文字列</param>
        public void Write(string log_text)
        {
            lock (LogLock)
            {
                try
                {
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    DateTime date = DateTime.Now;
                    LogFilePath = System.IO.Path.Combine(LogDirectory, FileName.Replace("yyyyMMdd", date.ToString("yyyyMMdd")));
                    using (StreamWriter sw = new StreamWriter(LogFilePath, true, System.Text.Encoding.UTF8))
                    {
                        sw.WriteLine(date.ToString("yyyy/MM/dd HH:mm:ss\t") + log_text);
                        sw.Close();
                    }
                }
                catch (Exception ex)
                {
                    Console.Write(ex.Message);
                }
            }
        }
        #endregion
    }
}
