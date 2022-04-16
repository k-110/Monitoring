using System;
using System.IO;
using System.Xml.Serialization;

namespace MyUtility
{
    /// <summary>
    ///  設定値管理機能
    ///  設定値はxmlファイルの形式で扱う
    /// </summary>
    /// <param name="T">XMLのデータ型</param>
    public class CXmlLoader<T>
    {
        /*------------------------*/
        /* リソース               */
        /*------------------------*/
        #region リソース
        /// <summary>
        ///  設定値のファイルパス(デフォルトは実行ファイルのあるフォルダ内のAppSetting.xml)
        /// </summary>
        public string FilePath { get; private set; } = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AppData.xml");
        /// <summary>
        ///  実行ファイルのあるフォルダ
        /// </summary>
        public string AppPath { get; private set; } = AppDomain.CurrentDomain.BaseDirectory;
        /// <summary>
        ///  アクセスを排他するためのロックオブジェクト
        /// </summary>
        private readonly object XmlLock = new object();
        /// <summary>
        ///  設定値
        /// </summary>
        public T Data;
        #endregion

        /*------------------------*/
        /* コンストラクタ等       */
        /*------------------------*/
        #region コンストラクタ等
        /// <summary>
        ///  <see cref="FilePath"/>で指定されたファイルから設定値をロードする処理
        /// </summary>
        /// <param name="xml_file">XMLファイル名</param>
        /// <returns>処理の実行結果</returns>
        public bool Load(string xml_file = null)
        {
            bool result = false;
            lock (XmlLock)
            {
                if (xml_file != null)
                {
                    FilePath = xml_file;
                }

                if (File.Exists(FilePath))
                {
                    try
                    {
                        XmlSerializer se = new XmlSerializer(typeof(T));
                        using (StreamReader sr = new StreamReader(FilePath, new System.Text.UTF8Encoding(false)))
                        {
                            Data = (T)se.Deserialize(sr);
                            sr.Close();
                            result = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        MyUtilityLog.Write(ex.ToString());
                    }
                }
            }
            return result;
        }

        /// <summary>
        ///  <see cref="FilePath"/>で指定されたファイルへ設定値をセーブする処理
        /// </summary>
        /// <param name="xml_file">XMLファイル名</param>
        /// <returns>処理の実行結果</returns>
        public bool Save(string xml_file = null)
        {
            lock (XmlLock)
            {
                if (xml_file != null)
                {
                    FilePath = xml_file;
                }

                try
                {
                    XmlSerializer se = new XmlSerializer(typeof(T));
                    using (StreamWriter sw = new StreamWriter(FilePath, false, new System.Text.UTF8Encoding(false)))
                    {
                        //名前空間のプレフィックスを出力しないようにする
                        XmlSerializerNamespaces ns = new XmlSerializerNamespaces();
                        ns.Add(String.Empty, String.Empty);

                        se.Serialize(sw, Data, ns);
                        sw.Close();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    MyUtilityLog.Write(ex.ToString());
                }
            }
            return false;
        }

        /// <summary>
        ///  XMLファイルから読み取った文字列をテキスト表示できる文字列に変換する処理
        /// </summary>
        /// <param name="buf">XMLで読み取った文字列</param>
        /// <returns>表示用の文字列</returns>
        public string XmlStringToViewString(string buf)
        {
            //\rがないと改行表示されないことがあるため\n→\rn変換する
            //(XMLのシリアライズ化する際に\rが消えたりするので・・・)
            return buf.Replace("\r\n", "\n").Replace("\n", "\r\n");
        }
        #endregion
    }
}
