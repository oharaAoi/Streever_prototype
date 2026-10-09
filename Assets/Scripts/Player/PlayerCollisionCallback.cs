using UnityEngine;

public class PlayerCollisionCallback : MonoBehaviour
{

	[SerializeField]
	private Animator animator;

	private void OnTriggerEnter(Collider other) {

		// Œš•¨‚É‚ ‚½‚Á‚½Û‚Ìˆ—
		if (other.tag == "building") {
			DeliveryAddress address = other.GetComponent<DeliveryAddress>();
			if (address != null) {
				PlayerInformation info = GetComponent<PlayerInformation>();
				if (info != null) {
					info.haveMoney += address.GetDeliveryFree() + info.viewership;
					info.viewership += address.GetIncreaseViewership();
				}
			}
		}

	}


	private void OnCollisionEnter(Collision collision) {
		// ƒ|[ƒ‹‚É‚ ‚½‚Á‚½Û‚Ìˆ—
		if (collision.gameObject.CompareTag("Pole")) {
			if (animator != null) {
				animator.SetTrigger("PoleHit");
			}
		}
	}

}
