using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;

public class Sniper : MonoBehaviour
{

    [SerializeField]
    private GameObject linePrefab;

    [SerializeField, Header("発射するまでの時間")]
    private float fireTime;

    [SerializeField, Header("狙った敵をスタンさせる時間")]
    private float stanTime;

	// ------------------------------------------------------------------

	private GameObject target;

    private LineRenderer shotLine;

    private GameObject line;

	private float fireTimer = 0;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
		fireTimer = 0;
	}

    // Update is called once per frame
    void Update()
    {

        if (!shotLine.enabled) {
			fireTimer = 0;
		}

        // ターゲットが見つかっている場合
        if (target) {
            fireTimer += Time.deltaTime;
            if(fireTimer >= fireTime) {
                fireTimer = 0;
                
                // playerをスタンさせる
                Player player = target.GetComponent<Player>();
                player.Stan(stanTime);
            }

			shotLine.SetPosition(1, target.transform.position);
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
