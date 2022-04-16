#-----------------------
# History
#-----------------------
import sys
import Lib.my_log as my_log
import my_history as my_history
from my_identify import my_identify

#-----------------------
# 履歴作成開始
my_log.log_start()


#-----------------------
# NNモデルの生成
identify = my_identify()


#-----------------------
# 処理選択
if len(sys.argv) == 1:
    # コマンドライン引数が無い場合の処理
    while True:
        print('\n\nEnter the command number.')
        print(' 1: Teaching')
        print(' 2: Deploy')
        print(' 3: History creation')
        print(' 4: Quit')
        print('', end='>')
        command = input()

        if command == '1':
            my_log.log_write('Start Teaching.')
            identify.teaching()
        elif command == '2':
            my_log.log_write('Start Deploy.')
            identify.applying()
        elif command == '3':
            my_log.log_write('Start History creation.')
            result = identify.creation()
            my_history.make_history(result)
        elif command == '4':
            my_log.log_write('Start Quit.')
            break
else:
    # コマンドライン引数がある場合の処理
    my_log.log_write('Start automatic processing.')
    result = identify.creation()
    my_log.log_write('Start make_history.')
    my_history.make_history(result)
    my_log.log_write('End automatic processing.')
