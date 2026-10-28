using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private int gameTime = 120;

    private bool isClear = false;

    private float elapsedTime = 0;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		isClear = false;
		elapsedTime = 0;
	}

    // Update is called once per frame
    void Update()
    {
		elapsedTime += Time.deltaTime; 
		if (gameTime < elapsedTime && !isClear) {
            isClear = true;
            ChangeScene();
		}
    }

    public float GetGameTime() {
        return gameTime - elapsedTime;
    }

    [SerializeField]
    private PlayerInformation playerInformation;

    public void ChangeScene()
    {
        // ResultSceneに渡す情報を保存
        ResultData.Viewership = playerInformation.viewership;

        // Scene切り替え
        SceneManager.LoadScene("ResultScene");
    }
}
