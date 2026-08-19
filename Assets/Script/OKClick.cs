using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fusion; // 💡 Fusionの機能を使用

// 💡 NetworkBehaviourを継承し、インスペクターから「NetworkObject」コンポーネントもアタッチしてください
public class OKClick : NetworkBehaviour
{
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button okButton;
    [SerializeField] private Button doodleOKButton;
    [SerializeField] private TextMeshProUGUI odaiText;
    
    private GameManager gameManager;
    private DrawLine drawLine;
    private int myRole = 0;

    void Start()
    {
        // 💡 ロビーから引き継いだ自分の役割を取得
        myRole = PlayerPrefs.GetInt("SelectedRole", 0);

        // 💡 起動時の初期UI状態をセット（最初は全員回答欄を隠す）
        inputField.gameObject.SetActive(false);
        okButton.gameObject.SetActive(false);
        odaiText.gameObject.SetActive(true);

        // 💡 役割によるボタンの出し分け
        if (myRole == 1) // 🎨 描き手なら「描き終わりボタン」を表示
        {
            Debug.Log(myRole);
            doodleOKButton.gameObject.SetActive(true);
        }
        else // 🧐 回答者なら「描き終わりボタン」は邪魔なので消しておく
        {
            Debug.Log(myRole);
            doodleOKButton.gameObject.SetActive(false);
        }
    }

    // 💡 描き手が「描き終わった（OK）」ボタンを押したときに実行する関数
    public void OnDoodleOK()
    {
        // 所有権チェック（念のため、描き手本人しか押せないようにガード）
        if (myRole != 1) return;

        Debug.Log("描き手が終了ボタンを押しました。全員の画面を入力モードに切り替えます。");
        
        // 💡【最重要】RPCを呼び出して、ネットの向こう側にいる全員の画面を「入力モード」へ一斉に変形させる！
        RpcSwitchToAnswerMode();
    }

    // 💡【RPC通信】この関数が呼ばれると、ネットで繋がっている全員の画面で同時に実行されます！
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcSwitchToAnswerMode()
    {
        // 1. インスタンスを安全に直前で取得（StartでのNullエラー対策）
        drawLine = DrawLine.instance;
        gameManager = GameManager.instance;

        if(myRole == 2)
        {
            inputField.gameObject.SetActive(true);
            okButton.gameObject.SetActive(true);
        }
        else
        {
            inputField.gameObject.SetActive(false);
            okButton.gameObject.SetActive(false);
        }
        doodleOKButton.gameObject.SetActive(false);

        // 3. お絵描き機能を強制停止（これで描き手もこれ以上線を引けなくなります）
        if (drawLine != null)
        {
            drawLine.canDraw = false;
            drawLine.StopDrawingForce(); 
        }
    }

    // 回答者が「回答（OK）」ボタンをクリックしたときの関数
    public void OnOKButtonClick()
    {
        gameManager = GameManager.instance;
        string inputText = inputField.text;
        
        if (gameManager != null)
        {
            gameManager.CheckAnswer(inputText);
        }
        else
        {
            Debug.LogError("GameManager instance is null.");
        }
        Debug.Log("Input Text: " + inputText);
    }

    public void OnClearButtonClick()
    {
        inputField.text = "";
    }
}
