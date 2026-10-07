using UnityEngine;

public class PlayerCollisionCallback : MonoBehaviour
{

	private void OnTriggerEnter(Collider other) {
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

}
