using UnityEngine;

public class FollowCamera : MonoBehaviour {

	[SerializeField]
	private Transform target;

	[SerializeField]
	private Player player;

	[SerializeField]
	private Vector3 offset = new Vector3(0.0f, 5.0f, -8.0f);

	[SerializeField]
	private float smoothTime = 0.15f;

	private Vector3 velocity;

	// 現在のカメラオフセット
	private Vector3 currentOffset;

	private void Start() {
		if (target != null) {
			currentOffset = target.rotation * offset;
		}
	}

	private void LateUpdate() {
		if (target == null || player == null) {
			return;
		}

		Quaternion rotation;

		if (player.isGrounded) {
			// 地上ではプレイヤーの回転をすべて反映
			rotation = target.rotation;
		} else {
			// 空中ではY軸回転のみを反映
			rotation = player.MoveRotation;
		}

		// カメラ固定操作が有効な場合のみ追従方向を更新
		currentOffset = rotation * offset;

		// カメラの目標位置
		Vector3 targetPosition = target.position + currentOffset;

		// 滑らかに移動
		transform.position = Vector3.SmoothDamp(
			transform.position,
			targetPosition,
			ref velocity,
			smoothTime
		);

		// プレイヤーの方向を向く
		transform.LookAt(target.position, Vector3.up);
	}
}