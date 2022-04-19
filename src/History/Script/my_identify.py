#-----------------------
# my_identify
#-----------------------
import tensorflow as tf
from keras.utils import np_utils
import numpy as np
#import pandas as pd
import matplotlib.pyplot as plt
import PIL
import glob
import os
import Lib.my_json as my_json


# 画像の分類を行うニューラルネットワーク
class my_identify:
    config = my_json.load_json('History.json')
    model  = None

    image_width = 60        # 画像データの幅
    image_height= 60        # 画像データの高さ
    image_color = 1         # 画像データの色サイズ(グレースケール)


    #-----------------------
    # 画像を読み込んで加工
    def shape_image(self, file):
        img = PIL.Image.open(file)
        return img.resize((self.image_width, self.image_height)).convert('L')


    #-----------------------
    # 学習
    def teaching(self):
        print('\n\n\n---> Status:学習開始')

        # 教師データ作成
        TeachingData = {    # 教師データ
            'Image':[],     # → 画像データ
            'Type':[]       # → 描かれている線の数
        }
        for info in self.config['teaching']:
            files = glob.glob(info['dir'] + '/*.webp', recursive=False)
            files.extend(glob.glob(info['dir'] + '/**/*.webp', recursive=False))
            for file in files:
                # 画像データを読み込んで加工
                img = self.shape_image(file)
                # 画像データをリストに格納
                TeachingData['Image'].append(np.array(img))
                TeachingData['Type'].append(info['ans'])
        print('---> Status:教師データ作成完了.')

        #-----------------------
        # モデルの構築
        self.model = tf.keras.models.Sequential([
            # レイヤー1
            tf.keras.layers.Conv2D(
                32,                         # フィルタ数
                (5, 5),                     # カーネル
                input_shape=(self.image_width, self.image_height, self.image_color),
                                            # 入力の形状＝画像データは幅×高さ×色の配列
                activation='relu'           # 活性化関数
            ),
            # レイヤー2
            tf.keras.layers.MaxPooling2D(
            ),
            # レイヤー3
            tf.keras.layers.Conv2D(
                64,                         # フィルタ数
                (5, 5),                     # カーネル
                activation='relu'           # 活性化関数
            ),
            # レイヤー4
            tf.keras.layers.MaxPooling2D(
            ),
            # レイヤー5
            tf.keras.layers.Flatten(),
            # レイヤー6
            tf.keras.layers.Dense(
                len(self.config['category']),
                                            # 出力は値が各項目に属する確率、分類項目数を設定する
                activation='softmax'        # 活性化関数
            )
        ])
        # デバッグのために構築したモデルを表示
        self.model.summary()
        print('---> Status:モデルの構築完了.')

        #-----------------------
        # モデルのコンパイル
        self.model.compile(
            optimizer='adam',               # 最適化アルゴリズム
            loss='categorical_crossentropy',# 目的関数
            metrics=['accuracy']            # 通常は['accuracy']を指定するらしい
        )
        print('---> Status:モデルのコンパイル完了.')

        #-----------------------
        # モデルの訓練
        x_train = np.array(TeachingData['Image']).reshape(len(TeachingData['Image']), self.image_width, self.image_height, self.image_color)
        y_train = np_utils.to_categorical(TeachingData['Type'], len(self.config['category']))
        x_train = x_train / 255.0       # 正規化(色の値を0.0～1.0の範囲に調整)した方が精度があがるらしい
        print('x_train : x_train.shape', x_train.shape)
        print('y_train : y_train.shape', y_train.shape)
        epochs = 10
        batch_size = 64
        stack = self.model.fit(
            x_train,                    # x：活性化関数の式に入力する値？
            y_train,                    # y：活性関数にxを入力した時の出力？
            epochs=epochs,              # 学習を繰り返す回数(仮の数値、損失関数の値を見ながらほぼ収束回数に調整する)
            batch_size=batch_size       # 教師データの数で決める(約3桁＝64、約4桁＝256、約5桁以上＝1024ぐらいでOK)
        )

        # 学習曲線のグラフの作成
        x = range(epochs)
        fig = plt.figure()
        plt.title('result')
        plt.plot(x, stack.history['accuracy'], label='accuracy')
        plt.plot(x, stack.history['loss'], label='loss')
        plt.legend(loc='center right', bbox_to_anchor=(1, 0.5))
        fig.savefig('result.png')
        print('---> Status:モデルの訓練完了.')

        #-----------------------
        # モデルの評価
        # 今回は動作確認したいだけなので教師データを流用
        x_test = x_train    
        y_test = y_train
        score = self.model.evaluate(x_test, y_test, batch_size=batch_size)
        print('test loss, test acc: ', score)
        print('Restored model, accuracy: {:5.2f}%'.format(100*score[1]))
        print('---> Status:モデルの評価完了.')


    #-----------------------
    # 学習結果の適用
    def applying(self):
        if self.model:
            self.model.save(self.config['model'])
            print('\n\n\n---> Status:学習結果の適用完了')
            return
        print('\n\n\n---> Error:学習結果の適用失敗')


    #-----------------------
    # 分類結果の生成
    def creation(self):
        print('\n\n\n---> Status:分類結果の生成開始')

        # モデルの読み込み
        if not self.model:
            if os.path.exists(self.config['model']):
                self.model = tf.keras.models.load_model(self.config['model'])
        if not self.model:
            print('\n\n\n---> Error:モデルの読み込み失敗')
            return
        # デバッグのために構築したモデルを表示
        self.model.summary()
        print('\n\n\n---> Status:モデルの読み込み完了')

        # 分類するデータを作成
        IdentifyResult = {
            'File':[],      # → ファイル名
            'Category':[]   # → 分類名
        }
        IdentifyData = []
        files = glob.glob(self.config['creation']['dir'] + '/*.webp', recursive=False)
        files.extend(glob.glob(self.config['creation']['dir'] + '/**/*.webp', recursive=False))
        IdentifyResult['File'].extend(files)
        for file in files:
            # 画像データを読み込んで加工
            img = self.shape_image(file)
            # 画像データをリストに格納
            IdentifyData.append(np.array(img))
        print('---> Status:分類するデータ作成完了.')

        # 分類を実行
        x_train = np.array(IdentifyData).reshape(len(IdentifyData), self.image_width, self.image_height, self.image_color)
        x_train = x_train / 255.0
        print('x_train : x_train.shape',x_train.shape)
        y_train = self.model.predict(x_train)
        print('---> Status:分類完了.')

        # 結果を返す
        for y_predict in y_train:
            name = self.config['category'][y_predict.argmax()]
            IdentifyResult['Category'].append(name)
        print('---> Status:分類結果の生成完了.')
        return IdentifyResult
