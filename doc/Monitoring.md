@import "doc.less"

# Monitoring

## 概要設計

### 機能構造＆データフロー

```plantuml
@startuml
!define  COLOR_DMY   #lightgray
package "Monitoring" {
   class "GUI\n(FormMain)" as FormMain{
      + MessageBox
   }
   class "OpenCvSharp" as OpenCvSharp{
      ※NuGetパッケージ
   }
   class "ImageProcessor" as ImageProcessor{
      ※NuGetパッケージ
   }
   package "MyUtility"{
      class "設定機能\n(AppSetting)" as AppSetting{
      }
      class "ログ機能\n(MyUtilityLog)" as MyUtility{
      }
   }
}
package "History"{
   class "CUI\n(History)" as History{
   }
   class "識別機能\n(my_identify)" as my_identify{
      ※TensorFlowを使用
   }
   class "履歴作成機能\n(my_history)" as my_history{
   }
}
package "Webサービス"{
   class "Webサーバー" as WebServer COLOR_DMY{
      ※必要であれば構築
   }
   object "履歴ファイル" as FileHistory{
      ※Html形式
      ※History.html
   }
   object "画像ファイル" as ImgHistory{
      ※WebP形式
      ※yyyyMMdd_HHmmss_fff.webp
   }
}
object "カメラ" as Camera{
}
object "Webブラウザ" as WebBrowser{
}
object "設定ファイル" as FileHist{
}
object "設定ファイル" as FileXml{
   ※Xml形式
   ※AppData.xml
}
object "ログファイル" as FileLog{
   ※テキスト形式
   ※yyyyMMdd_TraceLog.log
}
object "画像ファイル" as FileImg{
   ※WebP形式
   ※yyyyMMdd_HHmmss_fff.webp
}

'Monitoring
FormMain    <-    OpenCvSharp    : キャプチャした画像
FormMain    ->    OpenCvSharp    : 設定
FormMain    <--   AppSetting     : 設定値
FormMain    -->   MyUtility      : ログデータ
FormMain    -->   ImageProcessor : 画像
AppSetting  <--   FileXml
MyUtility   -->   FileLog
ImageProcessor -->FileImg

Camera      <-->  OpenCvSharp

'History
History     <--   my_identify    : 識別結果
History     -->   my_history     : 識別結果
my_identify <--   FileImg
my_identify <--   FileHist
my_history  -->   FileImg        : 移動
my_history  ->    FileHistory
my_history  -->   ImgHistory
my_history  <--   FileHist

'Webサービス
WebBrowser  <---  FileHistory
WebBrowser  <--   WebServer
WebServer   <--   FileHistory
FileHistory <--   ImgHistory
@enduml
```

### 設定ファイル(Monitoring)

- 設定値の意味は「CAppSetting」クラスのコメントを参照

```xml
<?xml version="1.0" encoding="utf-8"?>
<CAppSetting>
  <StartupView>false</StartupView>
  <CameraIndex>0</CameraIndex>
  <Width>1280</Width>
  <Height>960</Height>
  <Fps>30</Fps>
  <ExposureCorrection>0</ExposureCorrection>
  <BrightnessCorrection>0</BrightnessCorrection>
  <ContrastCorrection>0</ContrastCorrection>
  <CaptureInterval>1000</CaptureInterval>
  <PercentageDiff>5</PercentageDiff>
  <ImagePath>.\img</ImagePath>
</CAppSetting>
```

### 設定ファイル(History)

```json
{
  "history": {
    "html": "./History.html",
    "keyword": "<!-- OperatingHistoryList -->"
  },
  "model": "MyModel.h5",
  "category": [
    "Operating",
    "Passing",
    "Unmanned"
  ],
  "teaching": [
    {
      "ans": 0,
      "dir": "./img/teaching/0"
    },
    {
      "ans": 1,
      "dir": "./img/teaching/1"
    },
    {
      "ans": 2,
      "dir": "./img/teaching/2"
    }
  ],
  "creation": {
    "dir": "../../Monitoring/bin/Release/img",
    "backup": "./img/backup"
  }
}
```

|設定値|内容|
|-|-|
|history > html|htmlファイルのパス|
|history > keyword|履歴を挿入する位置を特定するキーワード|
|model|ニューラルネットワークのモデルを保存するファイル名|
|category|分類結果に付ける名称|
|teaching > ans|画像の分類番号 ※categoryのインデックス|
|teaching > dir|学習用の画像ファイルが入っているフォルダ|
|creation > dir|Monitoringのキャプチャした画像データが入っているフォルダ|
|creation > backup|creation > dirの中の識別済みの画像ファイルの移動先|

### 履歴ファイル

- 下図のようなイメージ
  - テーブルを用いて日付別に画像一覧を表示

@import "img/history.png"

## 詳細設計(Monitoring)

- .NETのFormアプリ
- C#

```plantuml
@startuml
!define  COLOR_CHK   #LightPink
start
:初期化;
fork
   repeat
      :イベント待ち;
      switch (イベント)
      case (OnPaint)
         :描画処理;
      case (View)
         :ウインドウを表示;
      case (Close)
         :ウインドウを非表示;
      case (Quit)
         COLOR_CHK:タスクの終了指示;
         :アプリを終了;
      endswitch
   repeat while (アプリの終了？)
fork again
   COLOR_CHK:while (タスクの終了指示無し？)
      :画像の取り込み;
      note right
         MyCapture → MyFrame
      end note
      :取り込んだ画像をグレースケール化;
      note right
         MyFrame → MyBackup
         ※グレースケールの画像は2つ保持する
         \t・今取り込んだ画像
         \t・1つ前に取り込んだ画像
      end note
      :1つ前の画像との差を計算する;
      note right
         MyBackup → MyDiff
      end note
      :計算した差を変化あり／変化なしに2値化;
      note right
         MyDiff → MyBin
      end note
      if (2値化データの変化ありの部分が一定以上存在する？) then (true)
         :取り込んだ画像をWebP形式で保存;
         note right
            MyFrame → bmp形式 → byte[] → WebP形式
         end note
         :表示を取り込んだ画像に変更;
      endif
   endwhile
endfork
stop
@enduml
```

## 詳細設計(History)

- Pythonスクリプト

```plantuml
@startuml
start
:識別機能のオブジェクトを作成;
if (コマンドライン引数なし？) then (true)
   repeat
      :イベント待ち;
      switch (イベント)
      case (Teaching)
         :モデルを構築して学習;
      case (Deploy)
         :学習させたモデルを保存;
      case (History creation)
         :モデルを使って画像を分類;
         :分類結果を元に履歴を作成;
      case (Quit)
         :スクリプトの終了;
      endswitch
   repeat while (アプリの終了？)
else (false)
   :モデルを使って画像を分類;
   :分類結果を元に履歴を作成;
endif
stop
@enduml
```

### NNモデル

|項目|内容|
|-|-|
|課題|画像の分類|
|モデル|畳み込みニューラルネッワーク|
|分類|3項目に分類<br>0：人が履歴を残したい操作をしている画像<br>1：人が通過している画像<br>2：人がいない画像|
|扱う画像|1280×960のWebP形式<br>学習の際には60×60のグレースケールに変換|
|my_identifyの出力|A：画像ファイルのパスの配列<br>B：画像の分類結果(categoryの文字列)の配列<br><br>A、Bは同じインデックスで対になっている<br>A[0] の分類は B[0]<br>A[1] の分類は B[1]<br>　：<br>A[n] の分類は B[n]|

<br>

- モデルの性能はイマイチ
  - 教師データの質が悪くて狙い通りの学習ができていないからかと
    - サンプル数が少ない
    - 画像の質が悪い(ピントが合っていいなかったり、暗かったり)
  - 作者の画像処理のスキルが低いので今はこれが精一杯、勉強が必要やね・・・

  @import "img/result.png"