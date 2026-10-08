using UnityEngine;

public class PlayerInformation : MonoBehaviour
{

    [SerializeField]
	private HaveMoneyUI haveMoneyUI;

	[SerializeField]
	private ViewerShipUI viewerShipUI;

	// 所持金
	public int haveMoney = 0;

    // 視聴者数
    public int viewership = 0;

    // トリックアクション成功時のベースの増える視聴者数
    public int trickIncreaseViewership = 100;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		if(haveMoneyUI != null) {
            haveMoneyUI.SetValue(haveMoney);
		}

        if(viewerShipUI != null) {
			viewerShipUI.SetValue(viewership);
		}
	}

    public void TrickViewer(bool isSuccess, int countX, int countY) {
        if (isSuccess) {
			viewership += trickIncreaseViewership * (countX * countY);
		} else {
			viewership -= trickIncreaseViewership * (countX * countY);
		}

		viewership = Mathf.Max(viewership, 0);
	}
}
