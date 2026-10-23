
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class ResultUI : MonoBehaviour
{
    private UIDocument uiDocument;

    private Label viewershipText;
    private Label moneyText;

    private bool isTransitioning = false;

    private void OnEnable()
    {
        uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("UIDocumentが見つかりません");
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        viewershipText = root.Q<Label>("ViewershipValue");
        moneyText = root.Q<Label>("HaveMoneyValue");

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

    private void Update()
    {
        // シーン遷移中は処理しない
        if (isTransitioning)
        {
            return;
        }

        // Spaceを押した瞬間だけ判定
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ChangeScene();
        }
    }

    private void ChangeScene()
    {
        isTransitioning = true;

        Debug.Log("CityMapSceneへ移動します");

        SceneManager.LoadScene(
            "CityMapScene",
            LoadSceneMode.Single
        );
    }
}
