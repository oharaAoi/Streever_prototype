using UnityEngine;

public class PlayerRotationController : MonoBehaviour
{
	[SerializeField]
	private float restoreDelay = 2.0f;

	[SerializeField]
	private float restoreSpeed = 5.0f;

	private Rigidbody rb;
	private float timer = 0.0f;

	private void Awake() {
		rb = GetComponent<Rigidbody>();
	}

	private void FixedUpdate() {

		// 経過時間を加算
		timer += Time.fixedDeltaTime;

		// 一定時間経過するまでは何もしない
		if (timer < restoreDelay) {
			return;
		}

		// 現在の上方向をワールドの上方向に合わせる回転差
		Quaternion correction = Quaternion.FromToRotation(
			transform.up,
			Vector3.up
		);

		// 目標の回転を計算
		Quaternion targetRotation = correction * rb.rotation;

		// 徐々に回転させる
		Quaternion newRotation = Quaternion.Slerp(
			rb.rotation,
			targetRotation,
			restoreSpeed * Time.fixedDeltaTime
		);

		rb.MoveRotation(newRotation);
	}

	// タイマーをリセット
	public void ResetTimer() {
		timer = 0.0f;
	}
}
