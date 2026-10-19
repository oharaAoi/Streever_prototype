using UnityEngine;

public class PlayerAnimationEventReceiver : MonoBehaviour
{
	private Player player;

	private void Awake() {
		player = GetComponentInParent<Player>();
	}

	public void FinishTrick() {
		if (player != null) {
			player.FinishTrick();
		}
	}
}
