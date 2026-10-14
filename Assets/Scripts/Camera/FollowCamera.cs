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
			Quaternion rotation = Quaternion.Euler(
				0.0f,
				target.eulerAngles.y,
				0.0f
			);

			currentOffset = rotation * offset;
		}
	}

	private void LateUpdate() {
		if (target == null || player == null) {
			return;
		}

		// X軸回転の影響を受けない進行方向
		Vector3 forward = player.MoveDirection;

		if (forward.sqrMagnitude < 0.001f) {
			return;
		}

		// 進行方向からY軸回転を生成
		Quaternion rotation = Quaternion.LookRotation(
			forward,
			Vector3.up
		);

		// カメラオフセットを計算
		currentOffset = rotation * offset;

		Vector3 targetPosition = target.position + currentOffset;

		transform.position = Vector3.SmoothDamp(
			transform.position,
			targetPosition,
			ref velocity,
			smoothTime
		);

		transform.LookAt(target.position);
	}
}