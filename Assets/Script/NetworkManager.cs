using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;        
using Fusion;
using System.Collections.Generic;

public class NetworkManager : MonoBehaviour 
{
    // 💡 _runner から runner に名前を変更して衝突を回避
    private NetworkRunner runner;

    [Header("UIの設定")]
    [SerializeField] 
    private TMP_InputField roomNameInputField;

    [SerializeField]
    private string nextSceneName = "LobbyScene"; 

    public async void OnClickStartButton()
    {
        string targetRoomName = roomNameInputField.text;
        if (string.IsNullOrWhiteSpace(targetRoomName))
        {
            targetRoomName = "DefaultRoom"; 
        }

        Debug.Log($"ルーム名「{targetRoomName}」へ接続を開始します...");

        GameObject runnerGo = new GameObject("NetworkRunner");
        runner = runnerGo.AddComponent<NetworkRunner>();
        runner.ProvideInput = true;

        var sceneManager = runnerGo.AddComponent<NetworkSceneManagerDefault>();

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath(nextSceneName);
        
        if (sceneIndex < 0)
        {
            Debug.LogError($"エラー:「{nextSceneName}」というシーンが Build Settings に登録されていないか、名前が間違っています。");
            Destroy(runnerGo);
            return;
        }

        SceneRef sceneRef = SceneRef.FromIndex(sceneIndex);

        var result = await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.Shared,       
            SessionName = targetRoomName,    
            SceneManager = sceneManager,
            Scene = sceneRef, 
            SessionProperties = new Dictionary<string ,SessionProperty>
            {
                {"HostRole", 0}
            }
        });

        if (result.Ok)
        {
            Debug.Log($"部屋「{targetRoomName}」への入室とシーン遷移に成功しました！");
        }
        else
        {
            Debug.LogError($"接続に失敗しました: {result.ShutdownReason}");
            Destroy(runnerGo); 
        }
    }
}
