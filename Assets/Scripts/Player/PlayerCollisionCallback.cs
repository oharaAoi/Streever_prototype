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

		// åöï®Ç…Ç†ÇΩÇ¡ÇΩç€ÇÃèàóù
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

	private void OnCollisionStay(Collision collision) {
		foreach (ContactPoint contact in collision.contacts) {
			// ï«Ç∆îªíË
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
