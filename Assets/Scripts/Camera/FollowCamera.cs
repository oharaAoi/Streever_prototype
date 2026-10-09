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

	// ワールド座標系でのカメラオフセット
	private Vector3 currentOffset;

	private void Start() {
		if (target != null) {
			currentOffset = target.rotation * offset;
		}
	}

	private void LateUpdate() {
		if (target == null) {
			return;
		}

		// 接地中のみプレイヤーの回転を反映
		if (player != null && player.isGrounded) {
			currentOffset = target.rotation * offset;
		}

		// 空中では最後に保存したオフセットを使用
		Vector3 targetPosition = target.position + currentOffset;

		transform.position = Vector3.SmoothDamp(
			transform.position,
			targetPosition,
			ref velocity,
			smoothTime
		);

		// 接地中のみカメラの向きを変更
		if (player != null && player.isGrounded) {
			transform.LookAt(target);
		}
	}
}