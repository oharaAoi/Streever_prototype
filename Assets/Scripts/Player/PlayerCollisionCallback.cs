using UnityEngine;

public class PlayerCollisionCallback : MonoBehaviour
{

	[SerializeField]
	private Animator animator;

	[SerializeField]
	private Player player;

	public void Awake() {
		player = GetComponent<Player>();
	}

	private void OnTriggerEnter(Collider other) {

		// Œš•¨‚É‚ ‚½‚Á‚½Û‚Ìˆ—
		if (other.tag == "building") {
			DeliveryAddress address = other.GetComponent<DeliveryAddress>();
			if (address != null) {
				PlayerInformation info = GetComponent<PlayerInformation>();
				if (info != null) {
					info.viewership += address.GetIncreaseViewership();
				}
			}
		}

		// Pole‚É‚ ‚½‚Á‚½‚Ìˆ—
		//if (other.tag == "Pole") {
		//	if (!player.isGrounded) {
		//		if (animator != null) {
		//			animator.SetTrigger("PoleHit");
		//			player.isCorrectingRotation = true;
		//		}

		//	}
		//}
	}

	private void OnCollisionStay(Collision collision) {
		foreach (ContactPoint contact in collision.contacts) {
			// •Ç‚Æ”»’è
			if (Mathf.Abs(contact.normal.y) < 0.2f) {
				player.wallNormal = contact.normal;
				break;
			}
		}
	}

	private void OnCollisionExit(Collision collision) {
		player.wallNormal = Vector3.zero;
	}

}
