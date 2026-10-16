
using UnityEngine;

public class MinimapCameraFollow : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField]
    private Player player;

    [Header("カメラ設定")]
    [SerializeField]
    private float cameraHeight = 230.0f;

    private float currentYaw = 0.0f;

    private void LateUpdate()
    {
        if (player == null)
            return;

        // プレイヤーの位置に追従
        Vector3 position = player.transform.position;

        transform.position = new Vector3(
            position.x,
            cameraHeight,
            position.z
        );

        // Player.csが管理している移動方向を取得
        Vector3 direction = player.MoveDirection;

        // 水平方向だけ使用
        direction.y = 0.0f;

        if (direction.sqrMagnitude > 0.0001f)
        {
            direction.Normalize();

            // 水平旋回角度を計算
            currentYaw = Mathf.Atan2(
                direction.x,
                direction.z
            ) * Mathf.Rad2Deg;
        }

        // カメラは常に真下を向く
        // X軸・Z軸の姿勢回転は反映しない
        transform.rotation =
            Quaternion.Euler(0.0f, currentYaw, 0.0f) *
            Quaternion.Euler(90.0f, 0.0f, 0.0f);
    }
}
