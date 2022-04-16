import os
import datetime


# ログ書き込み
def log_write(data):
    now = datetime.datetime.now()
    dir = 'log'
    name = dir + '/' + now.strftime('%Y%m%d_') + 'TraceLog.log'
    if not os.path.exists(dir):
        os.mkdir(dir)
    f = open(name, 'a', encoding='UTF-8')
    f.write(now.strftime('%Y/%m/%d %H:%M:%S\t') + data + '\n')
    f.close()
    # 標準出力にも表示する
    # ・複数要因がからまない異常は見ればするぐにわかるため
    #   ログファイルを開く手間を省くことができる
    print(data)


# ログ開始
def log_start():
    log_write('')
    log_write('------------ Start -------------')


# log_end
def log_end():
    log_write('------------ End ---------------')
