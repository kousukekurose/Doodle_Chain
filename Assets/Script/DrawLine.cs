using UnityEngine;
using UnityEngine.InputSystem;

public class DrawLine : MonoBehaviour
{
    public static DrawLine instance { get; private set; }
    [SerializeField] 
    private GameObject linePrefab;
    [Header("描画範囲の制限")]
    [SerializeField]
    private float minX = -5f;
    [SerializeField]
    private float maxX = 5f;
    [SerializeField]
    private float minY = -5f;
    [SerializeField]
    private float maxY = 5f;

    private LineRenderer currentLineRenderer;
    private int positionCount;
    private Camera mainCamera;
    private bool isDrawing = false;
    private Vector2 mousePos2D;
    private Vector3 worldPosition;

    public bool canDraw = false;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
    void Start()
    {
        canDraw = true;
        mainCamera = Camera.main;
    }

    void Update()
    {
        if(!canDraw) return;
        if (Mouse.current == null) return;

        // マウスの左ボタンが今「押されているか」を確実に取得
        bool isMousePressed = Mouse.current.leftButton.isPressed;

        if (isMousePressed)
        {
            // 押されている ＆ まだ描き始めていないなら「描き始め」の処理
            if (!isDrawing)
            {
                // マウスのスクリーン座標を取得
                mousePos2D = Mouse.current.position.ReadValue();
                worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePos2D.x, mousePos2D.y, 10));

                if(!IsInsideDrawingArea(worldPosition))
                {
                    // 描画範囲外なら処理を中断
                    isDrawing = false;
                    return;
                }

                if(!isDrawing )
                {
                    // 描画範囲内なら処理を続行
                    // プレハブから新しく線オブジェクトを生成する
                    GameObject newLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
                    currentLineRenderer = newLine.GetComponent<LineRenderer>();
                    currentLineRenderer.useWorldSpace = true;

                    positionCount = 0;
                    isDrawing = true;
                }
            }
            else
            {
                //書ける範囲外に出た時の処理
                mousePos2D = Mouse.current.position.ReadValue();
                worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePos2D.x, mousePos2D.y, 10));
                if(!IsInsideDrawingArea(worldPosition))
                {
                    // 描画範囲外なら処理を中断
                    isDrawing = false;
                    return;
                }

                AddPoint();
            }
        }
        else
        {
            // 押されていないなら「描き終わり」状態にする（クローン生成をストップ）
            isDrawing = false;
        }
    }

    void AddPoint()
    {
        if (currentLineRenderer == null) return;

        mousePos2D = Mouse.current.position.ReadValue();
        worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePos2D.x, mousePos2D.y, 10));

        if (positionCount == 0 || Vector3.Distance(currentLineRenderer.GetPosition(positionCount - 1), worldPosition) > 0.1f)
        {
            positionCount++;
            currentLineRenderer.positionCount = positionCount;
            currentLineRenderer.SetPosition(positionCount - 1, worldPosition);
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
        currentLineRenderer = null; // 線の接続を完全に切る（AddPointが動かなくなる）
    }

    public void ClearLines()
    {
        LineRenderer[] lines = FindObjectsByType<LineRenderer>();
        foreach (LineRenderer line in lines)
        {
            Destroy(line.gameObject);
        }
    }
}
