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


	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start() {

	}

	// Update is called once per frame
	void Update() {

	}

	private void LateUpdate() {
		if (target == null) {
			return;
		}

		Vector3 targetPosition = new Vector3();

		if (player != null) {
			if (player.isGrounded) {
				targetPosition = target.position + target.rotation * offset;
			} else {
				targetPosition = target.position + offset;
			}
		}

		transform.position = Vector3.SmoothDamp(
			transform.position,
			targetPosition,
			ref velocity,
			smoothTime
		);

		if (player != null) {
			if (player.isGrounded) {
				transform.LookAt(target);
			}
		}
	}

}
