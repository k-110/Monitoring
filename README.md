# Monitoring
画像処理の勉強用

- 詳しい設計は「doc/Monitoring.md」を参照  
  ※VScodeにmarkdown-preview-enhancedを入れて記載  

## 開発環境  

- Visula Studio 2019  
- Python 3.8  
  Pythonをインストールして「src > History > make_venv.bat」を実行すれば仮想環境が構築される  

## 概要  

- 2つのプログラムを使ったシステム  
  - カメラの画像を監視して変化があったら画像に残すアプリ(C#)  
  - 画像を分類して履歴を作成するスクリプト(Python)  
