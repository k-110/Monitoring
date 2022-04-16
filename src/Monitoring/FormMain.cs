using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using OpenCvSharp;
using ImageProcessor;
using ImageProcessor.Plugins.WebP.Imaging.Formats;

using MyUtility;

namespace Monitoring
{
    /// <summary>
    /// モニタリングするアプリケーション
    /// </summary>
    public partial class FormMain : Form
    {
        /// <summary>
        /// アプリケーションの設定の構造
        /// </summary>
        public class CAppSetting
        {
            public bool StartupView;                //起動時のウインドウ表示有無
            public int CameraIndex;                 //カメラ番号
            public int Width;                       //扱う画像の幅
            public int Height;                      //扱う画像の高さ
            public double Fps;                      //カメラの設定：フレーム数
            public double ExposureCorrection;       //カメラの設定：露出(初期値への加算値)
            public double BrightnessCorrection;     //カメラの設定：輝度(初期値への加算値)
            public double ContrastCorrection;       //カメラの設定：コントラスト(初期値への加算値)
            public int CaptureInterval;             //キャプチャ周期
            public int PercentageDiff;              //前にキャプチャした画像から差異があると判断する閾値(%)
            public string ImagePath;                //画像の保存先

            /// <summary>
            /// コンストラクタ
            /// </summary>
            public CAppSetting()
            {
                StartupView = false;
                CameraIndex = 0;
                Width = 1280;
                Height = 960;
                Fps = 30.0;
                ExposureCorrection = 0.0;
                BrightnessCorrection = 0.0;
                ContrastCorrection = 0.0;
                CaptureInterval = 1000;
                PercentageDiff = 5;
                ImagePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @".\img");
            }
        }

        /// <summary>
        /// インデックス機能
        /// </summary>
        private class CIndex
        {
            private int MyIndex = 0;
            private readonly int MyMax;

            public CIndex(int Max)
            {
                MyMax = Max;
            }

            public void Clear()
            {
                MyIndex = 0;
            }

            public void CountUp()
            {
                MyIndex++;
                if (MyMax <= MyIndex)
                {
                    Clear();
                }
            }

            public int GetIndex(int Offset = 0)
            {
                int Rounded = Offset % MyMax;
                int Index = MyIndex + Rounded;
                if (MyMax <= Index)
                {
                    Index -= MyMax;
                }
                else if (Index < 0)
                {
                    Index += MyMax;
                }
                return Index;
            }
        }

        /// <summary>
        /// アプリケーションの設定
        /// </summary>
        private readonly CXmlLoader<CAppSetting> AppSetting = new CXmlLoader<CAppSetting>();

        /// <summary>
        /// カメラ画像取得用のVideoCapture
        /// </summary>
        private readonly VideoCapture MyCapture;

        /// <summary>
        /// 画像取得用の配列データ
        /// </summary>
        private readonly Mat MyFrame;

        /// <summary>
        /// 2つの画像の差の配列データ
        /// </summary>
        private readonly Mat MyDiff;

        /// <summary>
        /// MyDiffを2値化した配列データ
        /// </summary>
        private readonly Mat MyBin;

        /// <summary>
        /// 比較用の配列データ
        /// </summary>
        private readonly Mat[] MyBackup = new Mat[2];

        /// <summary>
        /// 比較用の配列データの中で次に使用する位置
        /// </summary>
        private readonly CIndex MyBackupIndex;

        /// <summary>
        /// 取り込み用のBitmapデータ
        /// </summary>
        private Bitmap MyBmp = null;

        /// <summary>
        /// タスクの終了指示
        /// </summary>
        private bool RequestTaskExit = false;

        /// <summary>
        /// デリゲータ
        /// </summary>
        private delegate void DelegateFormMain();

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public FormMain()
        {
            InitializeComponent();
            
            MyUtilityLog.LogBegin();
            notifyIconMain.Icon = this.Icon;

            //設定を読み込む
            if (!AppSetting.Load())
            {
                AppSetting.Data = new CAppSetting();
            }

            //キャプチャの準備
            MyCapture = new VideoCapture(AppSetting.Data.CameraIndex)
            {
                FrameWidth = AppSetting.Data.Width,
                FrameHeight = AppSetting.Data.Height,
                Fps = AppSetting.Data.Fps,
            };
            MyCapture.Exposure += AppSetting.Data.ExposureCorrection;
            MyCapture.Brightness += AppSetting.Data.BrightnessCorrection;
            MyCapture.Contrast += AppSetting.Data.ContrastCorrection;
            MyFrame = new Mat(AppSetting.Data.Width, AppSetting.Data.Height, MatType.CV_8UC3);
            MyDiff = MyFrame.Clone();
            MyBin = MyFrame.Clone();
            for (int i = 0; i < MyBackup.Length; i++)
            {
                MyBackup[i] = MyFrame.Clone();
            }
            MyBackupIndex = new CIndex(MyBackup.Length);

            //モニタリング開始
            Task.Run(()=> {
                if (!MyCapture.IsOpened())
                {
                    MyUtilityLog.Write("Error:Camera not found.");
                }
                else
                {
                    MyUtilityLog.Write("Status:Start monitoring.");
                    try
                    {
                        TaskMonitoring();
                    }
                    catch (Exception ex)
                    {
                        MyUtilityLog.Write(ex.ToString());
                        MessageBox.Show(string.Format("End the capture.\n\n{0}", ex.ToString()), "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    MyUtilityLog.Write("Status:End of monitoring.");
                }
            });

            if (AppSetting.Data.StartupView)
            {
                this.Show();
            }
        }

        /// <summary>
        /// モニタリング処理
        /// </summary>
        private void TaskMonitoring()
        {
            //初期表示
            MyCapture.Grab();
            OpenCvSharp.Internal.NativeMethods.videoio_VideoCapture_operatorRightShift_Mat(MyCapture.CvPtr, MyFrame.CvPtr);
            MyBmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(MyFrame);
            if (this.Visible)
            {
                this.Invoke(new DelegateFormMain(Invalidate));
            }
            System.Threading.Thread.Sleep(AppSetting.Data.CaptureInterval);

            //MyBackupに何かセットしておかないとAbsdiffで例外が発生するため
            //最初の画像をMyBackupの全てにセットしておく
            for (int i = 0; i < MyBackup.Length; i++)
            {
                Cv2.CvtColor(MyFrame, MyBackup[i], ColorConversionCodes.RGB2GRAY);

            }

            //周期監視
            while (!RequestTaskExit)
            {
                System.Threading.Thread.Sleep(AppSetting.Data.CaptureInterval);

                //キャプチャ
                if (MyCapture.Grab())
                {
                    //MyFrameに取り込み
                    OpenCvSharp.Internal.NativeMethods.videoio_VideoCapture_operatorRightShift_Mat(MyCapture.CvPtr, MyFrame.CvPtr);

                    //MyFrameをグレースケールに変換してMyBackupに出力
                    Cv2.CvtColor(MyFrame, MyBackup[MyBackupIndex.GetIndex()], ColorConversionCodes.RGB2GRAY);

                    //差異の有無をチェックして差異があれば画像を保存
                    if (IsCaptureDataChange())
                    {
                        MyBmp = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(MyFrame);
                        SaveCaptureData(MyBmp);
                        if (this.Visible) {
                            this.Invoke(new DelegateFormMain(Invalidate));
                        }
                    }
                    MyBackupIndex.CountUp();
                }
            }
        }

        /// <summary>
        /// 画像を比較して差異があるかを確認
        /// </summary>
        /// <returns>差異の有無</returns>
        private bool IsCaptureDataChange()
        {
            //差を計算
            Cv2.Absdiff(MyBackup[MyBackupIndex.GetIndex()], MyBackup[MyBackupIndex.GetIndex(-1)], MyDiff);

            //差分を2値化
            Cv2.Threshold(MyDiff, MyBin, 0, 255, ThresholdTypes.Triangle);

            //2値化した画像の変化の割合から差異があるかを判定
            int difference = (Cv2.CountNonZero(MyBin) * 100) / (MyBin.Cols * MyBin.Rows);
            if (AppSetting.Data.PercentageDiff < difference)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// キャプチャしたデータを保存
        /// ※WebP形式で保存する
        /// </summary>
        /// <param name="Bmp">画像</param>
        private void SaveCaptureData(Bitmap Bmp)
        {
            //保存先のフォルダを作成
            string DirPath = Path.Combine(AppSetting.Data.ImagePath, DateTime.Now.ToString("yyyyMMdd"));
            if (!Directory.Exists(DirPath))
            {
                Directory.CreateDirectory(DirPath);
            }

            using (MemoryStream bmpStream = new MemoryStream())
            {
                //BitmapをByteに変換
                Bmp.Save(bmpStream, System.Drawing.Imaging.ImageFormat.Bmp);
                byte[] ImageBin = bmpStream.GetBuffer();

                using (MemoryStream inStream = new MemoryStream(ImageBin))
                using (MemoryStream outStream = new MemoryStream())
                using (ImageFactory imageFactory = new ImageFactory())
                {
                    //画像をWebPに変換
                    imageFactory.Load(inStream).Format(new WebPFormat()).Save(outStream);

                    //保存
                    string FilePath = Path.Combine(DirPath, string.Format("{0}.webp", DateTime.Now.ToString("yyyyMMdd_HHmmss_fff")));
                    File.WriteAllBytes(FilePath, outStream.ToArray());
                }
            }
        }

        /// <summary>
        /// イベント：描画
        /// </summary>
        /// <param name="e">イベントのパラメータ</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (MyBmp != null)
            {
                e.Graphics.DrawImage(MyBmp, this.ClientRectangle);
                e.Graphics.DrawString(DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"), this.Font, Brushes.White, 10, 10);
            }
        }

        /// <summary>
        ///  イベント：ウインドウを非表示
        ///  QuitToolStripMenuItem_Clickからの要求の場合はアプリケーションの終了
        /// </summary>
        /// <param name="sender">イベントの発行元</param>
        /// <param name="e">イベントのパラメータ</param>
        private void FormMainClosing(object sender, FormClosingEventArgs e)
        {
            if (!RequestTaskExit)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        /// <summary>
        ///  イベント：ウインドウを表示
        /// </summary>
        /// <param name="sender">イベントの発行元</param>
        /// <param name="e">イベントのパラメータ</param>
        private void ViewToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Show();
        }

        /// <summary>
        ///  イベント：アプリケーションの終了
        ///  リソースを開放してアプリケーションを終了する
        /// </summary>
        /// <param name="sender">イベントの発行元</param>
        /// <param name="e">イベントのパラメータ</param>
        private void QuitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            RequestTaskExit = true;
            Application.Exit();
        }
    }
}
