using UnityEngine;
using System.Collections;

public class SlowMotion : MonoBehaviour {

	[SerializeField]
	private float slowScale = 0.2f;

	[SerializeField]
	private float slowDuration = 0.5f;

	private Coroutine slowCoroutine;

	// スロー開始
	public void StartSlowMotion() {
		if (slowCoroutine != null) {
			StopCoroutine(slowCoroutine);
		}

		slowCoroutine = StartCoroutine(PlaySlowMotion());
	}

	private IEnumerator PlaySlowMotion() {

		// ゲーム全体の時間を遅くする
		Time.timeScale = slowScale;

		// 現実時間で待機
		yield return new WaitForSecondsRealtime(slowDuration);

		// 元の速度に戻す
		Time.timeScale = 1.0f;

		slowCoroutine = null;
	}

	private void OnDisable() {
		if (slowCoroutine != null) {
			StopCoroutine(slowCoroutine);
			slowCoroutine = null;
		}

		Time.timeScale = 1.0f;
	}
}