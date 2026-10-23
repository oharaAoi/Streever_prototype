
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChaser : MonoBehaviour
{
    [Header("追跡対象")]
    [SerializeField]
    private Transform player;

    [Header("追跡設定")]
    [SerializeField]
    private float chaseSpeed = 12.0f;

    [SerializeField]
    private float chaseDistance = 200.0f;

    [SerializeField]
    private float stopDistance = 2.0f;

    [SerializeField]
    private float updateInterval = 0.1f;

    [Header("NavMesh設定")]
    [SerializeField]
    private float searchRadius = 5.0f;

	[Header("UI Prefab")]
	[SerializeField]
	private WorldTargetUI markerPrefab;

	[Header("Canvas")]
	[SerializeField]
	private RectTransform canvasRect;

	[Header("Camera")]
	[SerializeField]
	private Camera targetCamera;

	private NavMeshAgent agent;
    private float updateTimer;

    private WorldTargetUI marker;

    [Header("デバッグ")]
    [SerializeField]
    private bool showDebugPath = true;
    private void OnDrawGizmos()
    {
        if (!showDebugPath)
            return;

        NavMeshAgent navAgent =
            GetComponent<NavMeshAgent>();

        if (navAgent == null || !navAgent.isOnNavMesh)
            return;

        NavMeshPath path = navAgent.path;

        if (path.corners.Length < 2)
            return;

        // 経路を緑色で表示
        Gizmos.color = Color.green;

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            Gizmos.DrawLine(
                path.corners[i],
                path.corners[i + 1]
            );
        }
    }


    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.speed = chaseSpeed;
        agent.stoppingDistance = stopDistance;
        agent.acceleration = 30.0f;
        agent.angularSpeed = 360.0f;
    }

    private void Start()
    {
        if (player == null)
        {
            Player foundPlayer =
                FindFirstObjectByType<Player>();

            if (foundPlayer != null)
            {
                player = foundPlayer.transform;
            }
        }

        if (player == null)
        {
            Debug.LogError(
                "EnemyChaser: Playerが見つかりません");
            return;
        }

        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "EnemyChaser: EnemyがNavMesh上にいません");
            return;
        }

        Debug.Log("EnemyChaser: 追跡開始");

        // 追跡者のUIを表示
		if (markerPrefab != null &&
			canvasRect != null &&
			targetCamera != null) {

			marker = Instantiate(
				markerPrefab,
				canvasRect
			);

			// 配達先を追従対象に設定
			marker.Initialize(
				transform,
				targetCamera,
				canvasRect
			);
		}
	}

    private void Update()
    {
        if (player == null)
        {
            return;
        }

        if (!agent.isOnNavMesh)
        {
            return;
        }

        updateTimer += Time.deltaTime;

        if (updateTimer < updateInterval)
        {
            return;
        }

        updateTimer = 0.0f;

        ChasePlayer();

        UpdateMarkerScale();
	}


    private void ChasePlayer()
    {
        // XZ平面での距離
        Vector2 enemyXZ = new Vector2(
            transform.position.x,
            transform.position.z
        );

        Vector2 playerXZ = new Vector2(
            player.position.x,
            player.position.z
        );

        float distance = Vector2.Distance(enemyXZ, playerXZ);

        if (distance <= stopDistance)
        {
            StopChase();
            return;
        }

        // プレイヤーの高さは使用しない
        Vector3 targetPosition = new Vector3(
            player.position.x,
            transform.position.y,
            player.position.z
        );

        // NavMesh上の到達可能な地点を探す
        NavMeshPath path = new NavMeshPath();

        // 半径を徐々に広げて候補地点を探す
        float[] searchRadii = { 2f, 5f, 10f, 20f, 40f };

        foreach (float radius in searchRadii)
        {
            if (!NavMesh.SamplePosition(
                targetPosition,
                out NavMeshHit hit,
                radius,
                agent.areaMask))
            {
                continue;
            }

            // 完全な経路が存在するか確認
            if (agent.CalculatePath(hit.position, path) &&
                path.status == NavMeshPathStatus.PathComplete)
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                return;
            }
        }

        // 到達可能な場所が見つからない場合、
        // 前回の有効な経路を維持する
        Debug.LogWarning(
            "EnemyChaser: 到達可能なNavMeshが見つかりません"
        );
    }
    private void StopChase()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        // 鬼の移動を停止
        agent.isStopped = true;

        // 現在の経路を削除
        agent.ResetPath();
    }

    // markerの更新を行う
    private void UpdateMarkerScale() {
		// 画像の大きさを調整する
		float distance = Vector3.Distance(player.position, transform.position);

		// 距離0～10を0～1に変換
		float normalizedDistance = Mathf.InverseLerp(100.0f, 500.0f, distance);

		float scale = Mathf.Lerp(1.0f, 0.5f, normalizedDistance);

		marker.SetScale(Vector3.one * scale);
	}
}
