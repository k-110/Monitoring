import os
import json


# ロード
def load_json(file):
    f = open(file, 'r', encoding='UTF-8')
    data = json.load(f)
    f.close()
    return data


# セーブ
def save_json(file, data):
    f = open(file, 'w', encoding='UTF-8')
    f.write(json.dumps(data, ensure_ascii=False, indent=2))
    f.close()
