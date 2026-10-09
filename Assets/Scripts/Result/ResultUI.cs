
using UnityEngine;
using UnityEngine.UIElements;

public class ResultUI : MonoBehaviour
{
    private UIDocument uiDocument;

    private Label viewershipText;
    private Label moneyText;

    private void OnEnable()
    {
        // UI Documentを取得
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("UIDocumentが見つかりません");
            return;
        }

        // UXMLからLabelを取得
        VisualElement root = uiDocument.rootVisualElement;

        viewershipText = root.Q<Label>("ViewershipValue");
        moneyText = root.Q<Label>("HaveMoneyValue");

        // リザルトの数値を表示
        UpdateResult();
    }

    private void UpdateResult()
    {
        if (viewershipText != null)
        {
            viewershipText.text =
                $"視聴者数：{ResultData.Viewership:N0}人";
        }

        if (moneyText != null)
        {
            moneyText.text =
                $"所持金：{ResultData.HaveMoney:N0}円";
        }
    }
}
