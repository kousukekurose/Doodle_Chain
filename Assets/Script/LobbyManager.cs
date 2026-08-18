using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using TMPro;
using System.Linq; 
using System.Collections.Generic;
using Fusion;

public class LobbyManager : MonoBehaviour // シーン直置きのため通常のMonoBehaviourで管理
{
    [Header("UI設定")]
    [SerializeField] private TMP_Text statusText; 
    [SerializeField] private Button startGameButton;
    
    [Header("ホスト専用の役割選択UI")]
    [SerializeField] private GameObject hostUIObject;    
    [SerializeField] private TMP_Text myRoleStatusText;  

    [Header("遷移先の設定")]
    [SerializeField] private string gameSceneName = "Test"; 

    private NetworkRunner _myNetworkRunner;
    private const string ROLE_KEY = "HostRole"; // セッションプロパティ用の合言葉

    private bool _localSelectionDone = false;

    void Start()
    {
        startGameButton.gameObject.SetActive(false);
        hostUIObject.SetActive(false); 
        _localSelectionDone = false;
        
        _myNetworkRunner = FindAnyObjectByType<NetworkRunner>();

        if (_myNetworkRunner == null)
        {
            statusText.text = "エラー: 通信が切断されました";
            return;
        }

        statusText.text = "対戦相手を待っています...";
        myRoleStatusText.text = "ホストの選択を待っています...";
    }

    void Update()
    {
        // 1. ネットワーク本体が捕まるまでは何もしない（Nullエラー防止）
        if (_myNetworkRunner == null)
        {
            _myNetworkRunner = FindAnyObjectByType<NetworkRunner>();
            return;
        }

        // 2. サーバー（部屋）との接続が完全に確立するまで待機する
        if (!_myNetworkRunner.IsCloudReady)
        {
            statusText.text = "ネットワーク同期中...";
            return;
        }

        // 3. 自分がホスト（1人目の入室者）かどうかを確実に判定する
        bool isHost = _myNetworkRunner.IsSharedModeMasterClient;

        if (isHost)
        {
            if (!_localSelectionDone)
            {
                statusText.text = "【あなたはホストです】役割を選んでください。";
                hostUIObject.SetActive(true); 
                startGameButton.gameObject.SetActive(false);
            }
            else
            {
                statusText.text = "【あなたはホストです】準備が完了しました。";
                hostUIObject.SetActive(false); 
                
                // 1人（テスト用）または2人揃った時に確実にスタートボタンを出す
                int playerCount = _myNetworkRunner.ActivePlayers.Count();
                if (playerCount >= 1) 
                {
                    startGameButton.gameObject.SetActive(true);
                }
            }
        }
        else
        {
            statusText.text = "【あなたはゲストです】";
            hostUIObject.SetActive(false); 
            startGameButton.gameObject.SetActive(false);

            // クライアント側：ホストの選択（セッションプロパティ）を安全に監視する
            CheckHostSelection();
        }
    }

    // ホストが「絵を描く」を選んだとき
    public void OnSelectDrawer()
    {
        _localSelectionDone = true; 
        SetHostRoleProperty(1); // 1: 描き手
        myRoleStatusText.text = "あなたの役割: 【絵を描く人】";
        PlayerPrefs.SetInt("SelectedRole", 1); 
        PlayerPrefs.Save();
    }

    // ホストが「答える」を選んだとき
    public void OnSelectAnswerer()
    {
        _localSelectionDone = true; 
        SetHostRoleProperty(2); // 2: 回答者
        myRoleStatusText.text = "あなたの役割: 【答える人】";
        PlayerPrefs.SetInt("SelectedRole", 2); 
        PlayerPrefs.Save();
    }

    // 部屋のカスタムプロパティ（通信エラーの起きない安全な書き込み）を使用します
    private void SetHostRoleProperty(int roleValue)
    {
        // 💡【修正点】IsOpen を Fusion 2.1最新の「IsRunning」に書き換えました
        if (_myNetworkRunner == null || !_myNetworkRunner.IsRunning) return;
        
        var properties = new Dictionary<string, SessionProperty>
        {
            {ROLE_KEY,SessionProperty.Convert(roleValue)}
        };
        properties[ROLE_KEY] = roleValue;
        _myNetworkRunner.SessionInfo.UpdateCustomProperties(properties);
    }

    // クライアント側：通信の準備ができた状態で安全にホストの選択を受け取る
    private void CheckHostSelection()
    {
        if (_myNetworkRunner == null || _myNetworkRunner.SessionInfo == null || _myNetworkRunner.SessionInfo.Properties == null) return;

        if (_myNetworkRunner.SessionInfo.Properties.TryGetValue(ROLE_KEY, out var propertyValue))
        {
            int hostRole = (int)propertyValue;

            if (hostRole == 1) 
            {
                myRoleStatusText.text = "ホストが選択完了\n今回はあなたは 答える人 です";
                PlayerPrefs.SetInt("SelectedRole", 2); 
            }
            else if (hostRole == 2) 
            {
                myRoleStatusText.text = "ホストが選択完了\n今回はあなたは 絵を描く人 です";
                PlayerPrefs.SetInt("SelectedRole", 1); 
            }
            PlayerPrefs.Save();
        }
        else
        {
            myRoleStatusText.text = "ホストが役割を選択中です...\nしばらくお待ちください。";
        }
    }

    public void OnGameScene()
    {
        int buildIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{gameSceneName}.unity");
        if (buildIndex != -1)
        {
            _myNetworkRunner.LoadScene(SceneRef.FromIndex(buildIndex));
        }
    }
}
