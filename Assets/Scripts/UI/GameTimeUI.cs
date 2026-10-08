using UnityEngine;
using UnityEngine.UIElements;

public class GameTimeUI : MonoBehaviour
{

	[SerializeField]
	private GameManager gameManager;

	private PanelRenderer panelRenderer;
	private Label gameTimeText;

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

		gameTimeText = root.Q<Label>("GameTime");

	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		if (gameTimeText != null) {
			gameTimeText.text = $"{gameManager.GetGameTime() - Time.time:0}";
		}
	}
}
