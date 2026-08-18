using UnityEngine;
using Fusion;

[RequireComponent(typeof(LineRenderer))]
public class SyncLinePoints : NetworkBehaviour
{
    private LineRenderer _lineRenderer;
    private int _positionCount = 0;

    void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
    }

    // 💡 RPC通信：自分の画面で呼ぶと、ネットの向こうの全員の画面でも同時に動く
    [Rpc(RpcSources.All, RpcTargets.All)]
    public void AddPointRPC(Vector3 nextPoint)
    {
        _positionCount++;
        _lineRenderer.positionCount = _positionCount;
        _lineRenderer.SetPosition(_positionCount - 1, nextPoint);
    }
}
