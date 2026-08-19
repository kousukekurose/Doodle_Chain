using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Fusion; // 💡 Fusionの機能を使用

[System.Serializable]
public class CSVData
{
    public int no;
    public string name;
}

// 💡 NetworkBehaviourを継承します
public class GameManager : NetworkBehaviour
{
    public static GameManager instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI odaiText;
    [SerializeField] private TextMeshProUGUI answerText;
    [SerializeField] private Button okButton;
    [SerializeField] private TMP_InputField answerInputField; // 💡回答入力用のInputFieldを紐付け
    [SerializeField] private GameObject endObject;
    [SerializeField] private float timeToShowEndObject = 2f;

    private List<CSVData> csvDataList = new List<CSVData>();
    private CSVData currentCSVData;

    // 💡【最重要】ホストが選んだお題の「CSVリスト内のインデックス」を全員に同期するネットワーク変数
    // 初期値を -1 にしておき、値が決まった瞬間に下の OnOdaiChanged が自動発動します
    [Networked, OnChangedRender(nameof(OnOdaiChanged))]
    private int SyncedOdaiIndex { get; set; } = -1;

    private int myRole = 0;

    private void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        endObject.gameObject.SetActive(false);
        answerText.gameObject.SetActive(false);
        
        // 💡 ロビーで保存した自分の役割（1:描き手, 2:回答者）を読み込む
        myRole = PlayerPrefs.GetInt("SelectedRole", 0);

        LoadCSVData();

        // 💡 役割による初期UIの出し分け（交通整理）
        ApplyRoleUI();
    }

    // 💡 ネットワーク上にGameManagerが生成された瞬間に呼ばれる（Startの後に動く）
    public override void Spawned()
    {
        // 💡 部屋の主（マスター）だけが、最初のお題をランダムに決定してネットに送信する
        if (Runner.IsSharedModeMasterClient)
        {
            SpawnNextCSVData();
        }
    }

    private void LoadCSVData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("odai");
        if (csvFile == null) return;
        
        string[] lines = csvFile.text.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            CSVData data = new CSVData
            {
                no = int.Parse(values[0]),
                name = values[1].Trim()
            };
            csvDataList.Add(data);
        }
    }

    // 💡 役割に合わせて画面のUIボタンや入力欄をパッと消したり出したりする処理
    private void ApplyRoleUI()
    {
        if (myRole == 1) // 🎨 描き手の場合
        {
            okButton.gameObject.SetActive(false);         // OKボタンはいらないので消す
            if(answerInputField != null) answerInputField.gameObject.SetActive(false); // 入力欄も消す
        }
        else // 🧐 回答者の場合
        {
            okButton.gameObject.SetActive(false);
            if(answerInputField != null) answerInputField.gameObject.SetActive(false);
            odaiText.text = "お題：ひみつ"; // 💡回答者にはまだ答えを隠しておく
        }
    }

    public void SpawnNextCSVData()
    {
        if (csvDataList.Count == 0) return;

        // 💡 ホスト（描き手）がランダムに選んだ「インデックス番号」を同期変数に叩き込む
        // これにより、ネットの向こうの回答者側にも自動で番号が届きます
        int randomIndex = Random.Range(0, csvDataList.Count);
        SyncedOdaiIndex = randomIndex; 
    }

    // 💡【同期システム】お題のインデックス番号がネット経由で届いた瞬間に、全員の画面で自動実行される関数
    private void OnRoleChanged() // ※OnChangedRender用
    {
        OnOdaiChanged();
    }

    private void OnOdaiChanged()
    {
        if (SyncedOdaiIndex < 0 || SyncedOdaiIndex >= csvDataList.Count) return;

        // 全員の画面で、同じCSVデータ（お題）を共有させる
        currentCSVData = csvDataList[SyncedOdaiIndex];
        answerText.text = "";

        // 💡 届いたお題を画面に表示するかどうかは、自分の役割でパターン分けする！
        if (myRole == 1) // 描き手なら文字を見せる
        {
            odaiText.text = $"お題 【{currentCSVData.name}】";
        }
        else // 回答者なら「？？？」にして隠す
        {
            odaiText.text = "お題 【 ひみつ】";
        }
    }

    // 💡 UIの「OK」ボタンを押したときに、回答者が実行する関数
    public void OnClickOKButton()
    {
        if (answerInputField == null) return;
        CheckAnswer(answerInputField.text);
    }

    public void CheckAnswer(string inputText)
    {
        if (answerText == null || odaiText == null || currentCSVData == null) return;

        answerText.gameObject.SetActive(true);
        string clarifiedInputText = inputText.Trim();
        string clearedCorrectAnswer = currentCSVData.name.Trim();

        if (clarifiedInputText.Equals(clearedCorrectAnswer))
        {
            // 💡【次のステップ】正解したことをRPC通信で描き手にも伝えて、全員同時に終了画面に進ませる
            answerText.color = Color.red;
            answerText.text = "正解！";
            
            // 正解したのでお題を公開する
            odaiText.text = $"お題 【{currentCSVData.name}】";
            okButton.gameObject.SetActive(false);
            StartCoroutine(ShowEndObject(timeToShowEndObject));
        }
        else
        {
            answerText.color = Color.blue;
            answerText.text = "不正解！";
        }
    }

    private IEnumerator ShowEndObject(float time)
    {
        yield return new WaitForSeconds(time);
        endObject.gameObject.SetActive(true);
    }
}
