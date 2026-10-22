using UnityEngine;
using UnityEngine.UI;

public class Sniper : MonoBehaviour
{

    [SerializeField]
    private GameObject linePrefab;

    [SerializeField]
    private GameObject chargeGauge;

    [SerializeField, Header("発射するまでの時間")]
    private float fireTime;

    [SerializeField, Header("狙った敵をスタンさせる時間")]
    private float stanTime;

	[SerializeField, Header("チャージゲージをどの程度の近さにするか")]
	private float chargeGaugeLength = 0.9f;

	// ------------------------------------------------------------------

	private GameObject target;

    private LineRenderer shotLine;

    private GameObject line;

    private Image chargeCircleImage;

	private float fireTimer = 0;

	private Camera mainCamera;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		fireTimer = 0;

		Canvas canvas = FindAnyObjectByType<Canvas>();
		if (chargeGauge) {
            GameObject circle = Instantiate(chargeGauge, canvas.transform);
			chargeCircleImage = circle.GetComponent<Image>();
        }

		mainCamera = Camera.main;
	}

    // Update is called once per frame
    void Update()
    {

        if(shotLine == null) {
            return;
        }

        if (!shotLine.enabled) {
			fireTimer = 0;
			chargeCircleImage.enabled = false;
        } else {
			chargeCircleImage.enabled = true;
		}

        // ターゲットが見つかっている場合
        if (target) {
            fireTimer += Time.deltaTime;
            if (fireTimer >= fireTime) {
                fireTimer = 0;

                // playerをスタンさせる
                Player player = target.GetComponent<Player>();
                player.Stan(stanTime);
            }

            shotLine.SetPosition(1, target.transform.position);
        }

        // ゲージをチャージする
        if (chargeCircleImage != null) {
			chargeCircleImage.fillAmount = fireTimer / fireTime;

			Vector3 centerPosition = Vector3.Lerp(target.transform.position, transform.position, chargeGaugeLength);

			Vector3 screenPosition =
			mainCamera.WorldToScreenPoint(centerPosition);

			chargeCircleImage.rectTransform.position = screenPosition;
		}
    }

    // ターゲットを設定する
    public void AimTarget(GameObject targetTransform) {
        target = targetTransform;

        if (target) {
			// 線の生成
			line = Instantiate(linePrefab,
                        new Vector3(0, 0, 0),
                        Quaternion.identity);

			// LineRenderer取得
			shotLine = line.GetComponent<LineRenderer>();

			shotLine.SetPosition(0, transform.position);
			shotLine.SetPosition(1, target.transform.position);
        } else {
			fireTimer = 0;

            Destroy(line);
		}
	}


}
