#-----------------------
# my_history
#-----------------------
import os
import shutil
import Lib.my_json as my_json
import Lib.my_text as my_text


# 分類結果から履歴を作成
def make_history(result):
    config = my_json.load_json('History.json')

    # 履歴の元となるデータ配列を作成
    text_imgdir  = config['history']['html'].replace(os.path.basename(config['history']['html']), '') + 'img/'
    operating = []
    for index in range(len(result['File'])):
        if result['Category'][index] == 'Operating':
            shutil.copy2(result['File'][index], text_imgdir)
            file_name = os.path.basename(result['File'][index])
            text_date = file_name[0:4] + '/' + file_name[4:6] + '/' + file_name[6:8]
            record_index = next((i for i, x in enumerate(operating) if x['date'] == text_date), None)
            if record_index == None:
                new_record = {
                    'date': text_date,
                    'img': [text_imgdir + file_name]
                }
                operating.append(new_record)
            else:
                operating[record_index]['img'].append(text_imgdir + file_name)
        shutil.move(result['File'][index], config['creation']['backup'] + '/')

    text_to_insert = ''
    for record in operating:
        img_list = sorted(record['img'])
        tr_text  = '\n        <tr>\n'
        tr_text += '          <td>' + record['date'] + '</td>\n'
        tr_text += '          <td>\n'
        for img in img_list:
            tr_text += '            <a href="' + img + '"><img src="' + img + '" width="64" height="64"></a>\n'
        tr_text += '          </td>\n'
        tr_text += '        </tr>'
        text_to_insert += tr_text

    # htmlを更新
    html_text = my_text.load_text(config['history']['html'])
    keyword_index = html_text.find(config['history']['keyword']) + len(config['history']['keyword'])
    html_new  = html_text[:keyword_index] + text_to_insert + html_text[keyword_index:]
    my_text.save_text(config['history']['html'], html_new)
