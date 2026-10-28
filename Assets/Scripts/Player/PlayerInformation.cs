using UnityEditor;
using UnityEngine;

public class PlayerInformation : MonoBehaviour
{

	[SerializeField]
	private ViewerShipUI viewerShipUI;

	[SerializeField]
	private int[] buffViewerShip;

    // 視聴者数
    public int viewership = 0;

    // トリックアクション成功時のベースの増える視聴者数
    public int trickIncreaseViewership = 100;

	private int prevViewership = 0;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(viewerShipUI != null) {
			viewerShipUI.SetValue(viewership);
		}
	}

    public void TrickViewer(bool isSuccess, int countX, int countY) {
        if (isSuccess) {
			viewership += trickIncreaseViewership * (countX * countY);
		} 

		viewership = Mathf.Max(viewership, 0);
	}

	// 視聴者を増やす
	public void IncreaseViewership(int num) {
		viewership += num;
	}

	// シールドを張るかの判定
	public bool IsShieldUp(int limit) {
		if(viewership - prevViewership >= 2000) {
			prevViewership = viewership;
			return true;
		}

		prevViewership = viewership;
		return false;
	}

	public void UpdateBuff(Player player) {
		if(viewership > buffViewerShip[2]) {
			player.SetDefaultTrickAddCount(2);
		} else if(viewership > buffViewerShip[1]) {
			player.SetStanTime(0.5f);
		} else if (viewership > buffViewerShip[0]) {
			player.SetAccelerationRaito(1.3f);
		}
	}

}
