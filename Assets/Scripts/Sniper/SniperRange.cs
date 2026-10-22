using UnityEngine;

public class SniperRange : MonoBehaviour
{

	[SerializeField]
	private Sniper sniper;

	// 範囲に侵入した時
	private void OnTriggerEnter(Collider other) {

		// 建物にあたった際の処理
		if (other.tag == "Player") {
			if (!sniper) {
				return;
			}

			// ターゲットに設定
			sniper.AimTarget(other.gameObject);
		}

	}

	// 範囲外に出た時
	private void OnTriggerExit(Collider other) {
		// 建物にあたった際の処理
		if (other.tag == "Player") {
			if (!sniper) {
				return;
			}

			// ターゲットに設定
			sniper.AimTarget(null);
		}
	}
}
