
using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour {

	private Coroutine shakeCoroutine;

	// ƒJƒƒ‰‚ğ—h‚ç‚·
	public void Shake(float duration, float strength) {

		// ‚·‚Å‚É—h‚ê‚Ä‚¢‚éê‡‚Í’â~
		if (shakeCoroutine != null) {
			StopCoroutine(shakeCoroutine);
		}

		shakeCoroutine = StartCoroutine(
			ShakeCoroutine(duration, strength)
		);
	}

	private IEnumerator ShakeCoroutine(float duration, float strength) {

		float elapsed = 0.0f;

		while (elapsed < duration) {

			// ƒ‰ƒ“ƒ_ƒ€‚È—h‚ê‚ğŒvZ
			Vector3 offset = Random.insideUnitSphere * strength;

			// ƒJƒƒ‰‚ÌˆÊ’u‚ğ•ÏX
			transform.localPosition = offset;

			elapsed += Time.unscaledDeltaTime;

			yield return null;
		}

		// Œ³‚ÌˆÊ’u‚É–ß‚·
		transform.localPosition = Vector3.zero;

		shakeCoroutine = null;
	}
}
