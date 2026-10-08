using UnityEngine;
using UnityEngine.UIElements;

public class BoostBarUI : MonoBehaviour
{

	private PanelRenderer panelRenderer;
	private ProgressBar progressBar;

	private void OnEnable() {
		panelRenderer = GetComponent<PanelRenderer>();
		panelRenderer.RegisterUIReloadCallback(OnUIReload);
	}

	private void OnDisable() {
		panelRenderer.UnregisterUIReloadCallback(OnUIReload);
	}

	private void OnUIReload(
		PanelRenderer renderer,
		VisualElement root,
		int version) {
		// ProgressBarを取得
		progressBar = root.Q<ProgressBar>("BoostBar");

		if (progressBar == null)
			return;

		// 最大値・最小値を設定
		progressBar.lowValue = 0;
		progressBar.highValue = 80;

		// 初期値
		progressBar.value = 100;
	}

	// ProgressBarの値を変更
	public void SetValue(float value) {
		if (progressBar == null)
			return;

		progressBar.value = value;
	}

}
