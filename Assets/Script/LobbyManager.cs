using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 
using TMPro;
using System.Linq; 
using System.Collections.Generic;
using Fusion;

public class LobbyManager : NetworkBehaviour
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
    private const string ROLE_KEY = "HostRole"; 

    // 💡 ローカル（自分の画面）で選択が完了したかを確実に管理するフラグ
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
        if (_myNetworkRunner == null) return;

        bool isHost = _myNetworkRunner.IsSharedModeMasterClient;

        if (isHost)
        {
            // 💡 ネットの同期遅延に左右されないよう、ローカルの完了フラグでUIを切り替える
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
                
                // 💡 選択が終わっていれば、1人（テスト時）または2人揃った時に確実にスタートボタンを出す
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
            
            // クライアント側：ホストの選択をリアルタイム監視
            CheckHostSelection();
        }
    }

    public void OnSelectDrawer()
    {
        _localSelectionDone = true; // 💡 選択完了フラグを立てる
        SetHostRoleProperty(1); // 1: 描き手
        myRoleStatusText.text = "あなたの役割: 【絵を描く人】";
        PlayerPrefs.SetInt("SelectedRole", 1); 
        PlayerPrefs.Save();
    }

    public void OnSelectAnswerer()
    {
        _localSelectionDone = true; // 💡 選択完了フラグを立てる
        SetHostRoleProperty(2); // 2: 回答者
        myRoleStatusText.text = "あなたの役割: 【答える人】";
        PlayerPrefs.SetInt("SelectedRole", 2); 
        PlayerPrefs.Save();
    }

    private void SetHostRoleProperty(int roleValue)
    {
        var properties = new Dictionary<string, SessionProperty>();
        properties[ROLE_KEY] = roleValue;
        _myNetworkRunner.SessionInfo.UpdateCustomProperties(properties);
    }

    private void CheckHostSelection()
    {
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
        else
        {
            Debug.LogError($"エラー: Build Settingsに {gameSceneName} が見つかりません。パスを確認してください: Assets/Scenes/{gameSceneName}.unity");
        }
    }
}
