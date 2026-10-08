using UnityEngine;
using UnityEngine.UIElements;

public class ViewerShipUI : MonoBehaviour
{
	private PanelRenderer panelRenderer;
	private Label healthText;

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
		// ProgressBar‚ğæ“¾

		healthText = root.Q<Label>("ViewershipValue");

	}

	// ProgressBar‚Ì’l‚ğ•ÏX
	public void SetValue(int value) {
		if (healthText != null) {
			healthText.text = $"{value:0}";
		}
	}
}
