using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;        // 💡 TextMeshProのInputFieldを使う場合
using Fusion;
using System.Collections.Generic;

public class NetworkManager : NetworkBehaviour
{
    private NetworkRunner _runner;

    [Header("UIの設定")]
    [SerializeField] 
    private TMP_InputField roomNameInputField;

    [SerializeField]
    private string nextSceneName = "LobbyScene"; // 次に遷移するシーン名

    // ★自作のUIボタンをクリックしたときに実行する関数
    public async void OnClickStartButton()
    {
        // 1. 入力された文字をルーム名として取得（空っぽならデフォル名にする）
        string targetRoomName = roomNameInputField.text;
        if (string.IsNullOrWhiteSpace(targetRoomName))
        {
            targetRoomName = "DefaultRoom"; // 未入力時の予備の部屋名
        }

        Debug.Log($"ルーム名「{targetRoomName}」へ接続を開始します...");

        // 2. 通信を管理するオブジェクトを自動生成
        GameObject runnerGo = new GameObject("NetworkRunner");
        _runner = runnerGo.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneManager = runnerGo.AddComponent<NetworkSceneManagerDefault>();

        // 3. ユーザーが決めたルーム名で部屋に入る（なければ作る）
        var result = await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,       
            SessionName = targetRoomName,    // 💡 ユーザーが入力をした文字列をここに渡す！
            SceneManager = sceneManager,
            SessionProperties = new Dictionary<string ,SessionProperty>
            {
                {"HostRole",0}
            }
        });

        // 4. 接続結果の判定
        if (result.Ok)
        {
            Debug.Log($"部屋「{targetRoomName}」への入室に成功しました！");
            SceneManager.LoadScene(nextSceneName); 
        }
        else
        {
            Debug.LogError($"接続に失敗しました: {result.ShutdownReason}");
        }
    }
}
