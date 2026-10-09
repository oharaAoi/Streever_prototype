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
					info.haveMoney += address.GetDeliveryFree() + info.viewership;
					info.viewership += address.GetIncreaseViewership();
				}
			}
		}

		// ƒ|[ƒ‹‚É‚ ‚½‚Á‚½Û‚Ìˆ—
		if (other.tag == "Pole") {
			if (!player.isGrounded) {
				if (animator != null) {
					animator.SetTrigger("PoleHit");
					player.isCorrectingRotation = true;
				}
			}
		}

	}

}
