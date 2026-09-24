using UnityEngine;
using UnityEngine.InputSystem;
using Fusion; // 💡 Fusion 2.1の機能を使用

public class DrawLine : NetworkBehaviour
{
    public static DrawLine instance { get; private set; }
    
    [SerializeField] 
    private GameObject linePrefab; // 💡 NetworkObjectとSyncLinePointsコンポーネントをつけた線のプレハブ
    
    [Header("描画範囲の制限")]
    [SerializeField] private float minX = -5f;
    [SerializeField] private float maxX = 5f;
    [SerializeField] private float minY = -5f;
    [SerializeField] private float maxY = 5f;

    private LineRenderer currentLineRenderer;
    private int positionCount;
    private Camera mainCamera;
    private bool isDrawing = false;
    private Vector2 inputPos2D;
    private Vector3 worldPosition;

    public bool canDraw = false;

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }

    void Start()
    {
        mainCamera = Camera.main;

        // 💡【重要：ロビーからの役割受け取り】
        // ロビーシーンのPlayerPrefsで保存した役割（1:描き手, 2:回答者）を読み込みます
        int myRole = PlayerPrefs.GetInt("SelectedRole", 0);

        if (myRole == 1)
        {
            Debug.Log("あなたは【描き手】です。お絵描きが許可されました。");
            canDraw = true; 
        }
        else
        {
            Debug.Log("あなたは【回答者】です。描画機能はオフ（見る専門）になります。");
            canDraw = false; // 回答者は描けない
        }
    }

    public override void Spawned()
    {
        instance = this;
    }

    void Update()
    {
        // ロビーで回答者を選んだ、または描画制限がかかっている場合は入力をすべてスルー
        if (!canDraw) return;

        // マウス ＆ スマホタッチ両対応の判定
        bool isPressing = false;
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            isPressing = true;
            inputPos2D = Touchscreen.current.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            isPressing = true;
            inputPos2D = Mouse.current.position.ReadValue();
        }

        if (isPressing)
        {
            worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(inputPos2D.x, inputPos2D.y, 10));

            if (!isDrawing)
            {
                if (!IsInsideDrawingArea(worldPosition))
                {
                    isDrawing = false;
                    return;
                }

                // 💡 描き手が線を引くと、Runner.Spawnによりネット上の全員の画面に「線」が生まれます
                NetworkObject newLineObj = Runner.Spawn(linePrefab, Vector3.zero, Quaternion.identity);
                currentLineRenderer = newLineObj.GetComponent<LineRenderer>();
                currentLineRenderer.useWorldSpace = true;

                positionCount = 0;
                isDrawing = true;
            }
            else
            {
                if (!IsInsideDrawingArea(worldPosition))
                {
                    isDrawing = false;
                    return;
                }

                AddPoint();
            }
        }
        else
        {
            isDrawing = false;
        }
    }

    void AddPoint()
    {
        if (currentLineRenderer == null) return;

        if (positionCount == 0 || Vector3.Distance(currentLineRenderer.GetPosition(positionCount - 1), worldPosition) > 0.1f)
        {
            positionCount++;
            currentLineRenderer.positionCount = positionCount;
            currentLineRenderer.SetPosition(positionCount - 1, worldPosition);

            // 💡 自分が座標を1つ追加したら、その線プレハブ側の同期用スクリプトを呼び出して
            // ネットの向こうの相手（回答者）の画面にあるLineRendererにも同時に座標を追加させます
            var syncScript = currentLineRenderer.GetComponent<SyncLinePoints>();
            if (syncScript != null)
            {
                syncScript.AddPointRPC(worldPosition);
            }
        }
    }

    private bool IsInsideDrawingArea(Vector3 position)
    {
        return position.x >= minX && position.x <= maxX && 
                position.y >= minY && position.y <= maxY;
    }

        public void StopDrawingForce()
    {
        isDrawing = false;
        currentLineRenderer = null; // 線の接続を完全に切る
    }

}
