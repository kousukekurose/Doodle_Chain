using UnityEngine;
using UnityEngine.InputSystem;

public class DrawLine : MonoBehaviour
{
    [SerializeField] 
    private GameObject linePrefab;

    private LineRenderer currentLineRenderer;
    private int positionCount;
    private Camera mainCamera;
    private bool isDrawing = false;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Mouse.current == null) return;

        // マウスの左ボタンが今「押されているか」を確実に取得
        bool isMousePressed = Mouse.current.leftButton.isPressed;

        if (isMousePressed)
        {
            // 押されている ＆ まだ描き始めていないなら「描き始め」の処理
            if (!isDrawing)
            {
                // プレハブから新しく線オブジェクトを生成する
                GameObject newLine = Instantiate(linePrefab, Vector3.zero, Quaternion.identity);
                currentLineRenderer = newLine.GetComponent<LineRenderer>();
                currentLineRenderer.useWorldSpace = true;

                positionCount = 0;
                isDrawing = true;
                AddPoint();
            }
            else
            {
                // 押されている ＆ すでに描き始めているなら「ドラッグ中」の処理
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

        Vector2 mousePos2D = Mouse.current.position.ReadValue();
        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(new Vector3(mousePos2D.x, mousePos2D.y, 10));

        if (positionCount == 0 || Vector3.Distance(currentLineRenderer.GetPosition(positionCount - 1), worldPosition) > 0.1f)
        {
            positionCount++;
            currentLineRenderer.positionCount = positionCount;
            currentLineRenderer.SetPosition(positionCount - 1, worldPosition);
        }
    }
}
