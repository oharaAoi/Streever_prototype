using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private int gameTime = 120;

    private bool isClear = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		isClear = false;
	}

    // Update is called once per frame
    void Update()
    {
		if (gameTime < Time.time && !isClear) {
            isClear = true;
            ChangeScene();
		}
    }

    public int GetGameTime() {
        return gameTime;
    }

    [SerializeField]
    private PlayerInformation playerInformation;

    public void ChangeScene()
    {
        // ResultSceneに渡す情報を保存
        ResultData.HaveMoney = playerInformation.haveMoney;
        ResultData.Viewership = playerInformation.viewership;

        // Scene切り替え
        SceneManager.LoadScene("ResultScene");
    }
}
