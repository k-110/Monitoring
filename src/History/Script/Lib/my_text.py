import os


# ロード
def load_text(file):
    f = open(file, 'r', encoding='UTF-8')
    data = f.read()
    f.close()
    return data


# セーブ
def save_text(file, data):
    f = open(file, 'w', encoding='UTF-8')
    f.write(data)
    f.close()
