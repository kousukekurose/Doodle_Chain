using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using Fusion;

[System.Serializable]
public class CSVData
{
    public int no;
    public string name;
}

public class GameManager : NetworkBehaviour
{
    public static GameManager instance { get; private set; }
    
    [SerializeField] private TextMeshProUGUI odaiText;
    [SerializeField] private TextMeshProUGUI answerText;
    
    [SerializeField] private GameObject endObject;
    [SerializeField] private float timeToShowEndObject = 2f;

    private List<CSVData> csvDataList = new List<CSVData>();
    private CSVData currentCSVData;

    // 💡 お題のインデックスを全員に同期するネットワーク変数（コールバックをFusion2の推奨形式に修正）
    [Networked, OnChangedRender(nameof(OnOdaiChanged))]
    private int SyncedOdaiIndex { get; set; } = -1;

    private int myRole = 0;
    private bool isUiInitialized = false;

    private void Awake()
    {
        if (instance == null) { instance = this; }
        else { Destroy(gameObject); }
    }

    void Start()
    {
        endObject.gameObject.SetActive(false);
        answerText.gameObject.SetActive(false);
        
        UpdateMyRole();
        LoadCSVData();
        ApplyRoleUI();
    }

    private void UpdateMyRole()
    {
        myRole = PlayerPrefs.GetInt("SelectedRole", 0);
    }

    public override void Spawned()
    {
        UpdateMyRole();

        // 💡 自分が「描き手（Role 1）」だった場合、権限を持つホストにお題の決定と同期を依頼する
        if (myRole == 1)
        {
            Debug.Log("[描き手検知] 権限者（ホスト）にお題決定リクエスト(RPC)を送信します...");
            Rpc_RequestSpawnNextCSVData();
        }
        else
        {
            if (SyncedOdaiIndex >= 0)
            {
                RefreshOdaiUI();
            }
        }
    }

    public override void Render()
    {
        // ネットワーク越しにインデックスが届いたら即座に反映
        if (!isUiInitialized && SyncedOdaiIndex >= 0 && csvDataList.Count > 0)
        {
            RefreshOdaiUI();
            isUiInitialized = true;
        }
    }

    private void LoadCSVData()
    {
        TextAsset csvFile = Resources.Load<TextAsset>("odai");
        if (csvFile == null) return;
        
        string[] lines = csvFile.text.Split(new char[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        for (int i = 1; i < lines.Length; i++)
        {
            string[] values = lines[i].Split(',');
            if (values.Length < 2) continue;

            string cleanedNo = values[0].Trim();
            string cleanedName = values[1].Trim();
            cleanedName = System.Text.RegularExpressions.Regex.Replace(cleanedName, @"[\p{C}]", "");

            if (string.IsNullOrEmpty(cleanedName)) continue;

            CSVData data = new CSVData { no = int.Parse(cleanedNo), name = cleanedName };
            csvDataList.Add(data);
        }
        Debug.Log($"[CSVロード完了] 合計お題数: {csvDataList.Count}");
    }

    private void ApplyRoleUI()
    {
        if (myRole != 1) 
        {
            odaiText.text = "おだい ひみつ";
        }
    }

    // 💡【最重要】描き手（ゲスト）から、オブジェクトの所有者（ホスト）に向けて実行するRPC
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void Rpc_RequestSpawnNextCSVData()
    {
        Debug.Log("[RPC受信] 描き手からリクエストを受信。ホスト権限でお題を決定します。");
        SpawnNextCSVData();
    }

    public void SpawnNextCSVData()
    {
        if (csvDataList.Count == 0) return;

        // 💡 権限チェック（ホスト側で実行されていれば必ず通過します）
        if (!Object.HasStateAuthority)
        {
            Debug.LogError("書き込み権限がないため、お題を同期変数にセットできませんでした。");
            return;
        }

        int randomIndex = Random.Range(0, csvDataList.Count);
        SyncedOdaiIndex = randomIndex; 
        Debug.Log($"[ホストがお題決定完了] インデックス {randomIndex} を同期変数に書き込みました。");

        // 💡 ホスト自身（今回は回答者）のUIや内部データも強制的に同期させる
        RefreshOdaiUI();
    }

    // 💡 リモートで値が変わった時に自動で走るコールバック
    private void OnOdaiChanged()
    {
        Debug.Log($"【同期検知】お題インデックスが更新されました: {SyncedOdaiIndex}");
        RefreshOdaiUI();
    }

    public void RefreshOdaiUI()
    {
        UpdateMyRole();
        
        if (SyncedOdaiIndex < 0 || csvDataList == null || SyncedOdaiIndex >= csvDataList.Count) return;

        currentCSVData = csvDataList[SyncedOdaiIndex];
        answerText.text = "";

        if (myRole == 1) // 描き手
        {
            odaiText.text = $"おだい {currentCSVData.name}";
        }
        else // 回答者
        {
            odaiText.text = "おだい ひみつ";
        }
        Debug.Log($"[RefreshOdaiUI完了] 表示テキスト: {odaiText.text}");
    }

    public void CheckAnswer(string inputText)
    {
        if (answerText == null || odaiText == null) return;

        string clarifiedInputText = inputText.Trim();
        Debug.Log($"[CheckAnswer] 入力された文字: [{clarifiedInputText}]");

        if (csvDataList == null || csvDataList.Count == 0)
        {
            LoadCSVData();
        }

        // 💡 正しい同期変数から答えを引っ張る
        if (SyncedOdaiIndex < 0 || SyncedOdaiIndex >= csvDataList.Count)
        {
            Debug.LogError($"[CheckAnswerエラー] 同期インデックスが不正です: {SyncedOdaiIndex}");
            return;
        }

        string currentCorrectAnswer = csvDataList[SyncedOdaiIndex].name.Trim();
        Debug.Log($"[CheckAnswer照合] 判定する正しい答え: [{currentCorrectAnswer}]");

        if (!string.IsNullOrEmpty(currentCorrectAnswer) && clarifiedInputText.Equals(currentCorrectAnswer, System.StringComparison.OrdinalIgnoreCase))
        {
            if (Object != null && Object.IsValid) Rpc_OnCorrectAnswer(currentCorrectAnswer);
        }
        else
        {
            if (Object != null && Object.IsValid) Rpc_OnWrongAnswer(clarifiedInputText);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_OnCorrectAnswer(string correctAnswer)
    {
        if (answerText == null || odaiText == null) return;

        answerText.gameObject.SetActive(true);
        answerText.color = Color.red;
        answerText.text = "せいかい！";
        odaiText.text = $"おだい {correctAnswer}";
        
        StartCoroutine(ShowEndObject(timeToShowEndObject));
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void Rpc_OnWrongAnswer(string inputText)
    {
        if (answerText == null) return;

        answerText.gameObject.SetActive(true);
        answerText.color = Color.blue;
        answerText.text = inputText + " はちがうみたいだよ？";
    }

    private IEnumerator ShowEndObject(float time)
    {
        yield return new WaitForSeconds(time);
        endObject.gameObject.SetActive(true);
    }
}
