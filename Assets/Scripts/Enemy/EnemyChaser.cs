
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChaser : MonoBehaviour
{
    [Header("追跡対象")]
    [SerializeField] private Transform player;

    [Header("追跡設定")]
    [SerializeField] private float chaseSpeed = 12.0f;
    [SerializeField] private float chaseDistance = 200.0f;
    [SerializeField] private float stopDistance = 2.0f;

    [SerializeField, Min(0.01f)]
    private float updateInterval = 0.3f;

    [SerializeField, Min(0f)]
    private float resumeMargin = 0.75f;

    [SerializeField, Min(0f)]
    private float destinationThreshold = 1.0f;

    [Header("NavMesh設定")]
    [SerializeField, Min(0.1f)]
    private float searchRadius = 5.0f;

    [Header("UI Prefab")]
    [SerializeField] private WorldTargetUI markerPrefab;

    [Header("Canvas")]
    [SerializeField] private RectTransform canvasRect;

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;

    [Header("デバッグ")]
    [SerializeField] private bool showDebugPath = true;

    private NavMeshAgent agent;
    private WorldTargetUI marker;

    private float updateTimer;

    private bool hasDestination;
    private Vector3 lastDestination;

    private NavMeshPath reusablePath;

    [Header("追跡・巡回の切り替え")]
    [SerializeField, Min(0f)] private float chaseRestartDelay = 5f;
    [SerializeField, Min(0f)] private float patrolStartDelay = 3f;
    [SerializeField, Min(0.1f)] private float patrolRadius = 30f;
    [Tooltip("巡回先までの最低直線距離。Patrol Radiusはこの値以上で使用されます。")]
    [SerializeField, Min(0f)] private float patrolMinDistance = 10f;
    [SerializeField, Min(0.1f)] private float patrolSpeed = 6f;
    [SerializeField, Min(0.1f)] private float patrolArrivalDistance = 1f;

    private bool isChasing;
    private float nonChaseTimer;
    private float outsideRangeTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        reusablePath = new NavMeshPath();

        agent.speed = chaseSpeed;
        agent.stoppingDistance = stopDistance;
        agent.acceleration = 30.0f;
        agent.angularSpeed = 360.0f;
    }

    private void Start()
    {
        // プレイヤーを取得
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
                "EnemyChaser: Playerが見つかりません",
                this
            );
            return;
        }

        // NavMesh上にいるか確認
        if (!agent.isOnNavMesh)
        {
            Debug.LogError(
                "EnemyChaser: EnemyがNavMesh上にいません",
                this
            );
        }
        else
        {
            Debug.Log(
                "EnemyChaser: 追跡開始",
                this
            );
        }

        agent.speed = patrolSpeed;
        agent.stoppingDistance = patrolArrivalDistance;

        // 最初の経路設定を遅らせない
        updateTimer = updateInterval;
    }

    private void Update()
    {
        if (player == null ||
            agent == null ||
            !agent.isOnNavMesh)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        if (isChasing)
        {
            outsideRangeTimer = distance >= chaseDistance
                ? outsideRangeTimer + Time.deltaTime : 0f;
            if (distance >= chaseDistance && outsideRangeTimer >= patrolStartDelay)
            {
                SetChasing(false);
            }
        }
        else
        {
            nonChaseTimer += Time.deltaTime;
            if (distance <= chaseDistance || nonChaseTimer >= chaseRestartDelay)
            {
                SetChasing(true);
            }
        }

        updateTimer += Time.deltaTime;
        if (updateTimer < updateInterval)
        {
            return;
        }

        updateTimer = 0f;
        if (isChasing)
        {
            ChasePlayer();
        }
        else
        {
            Patrol();
        }
    }

    private void SetChasing(bool chasing)
    {
        isChasing = chasing;
        outsideRangeTimer = 0f;
        nonChaseTimer = 0f;
        StopChase();
        agent.speed = chasing ? chaseSpeed : patrolSpeed;
        agent.stoppingDistance = chasing ? stopDistance : patrolArrivalDistance;
        updateTimer = updateInterval;
    }

    private void Patrol()
    {
        if (agent.pathPending)
        {
            return;
        }

        if (hasDestination && agent.hasPath &&
            agent.pathStatus == NavMeshPathStatus.PathComplete &&
            agent.remainingDistance > patrolArrivalDistance)
        {
            return;
        }

        // 到達可能なランダム地点へ向かい、到着後に次の地点を選ぶ。
        for (int i = 0; i < 10; i++)
        {
            // 最低距離から巡回半径までの範囲で候補を選ぶ。
            float minDistance = Mathf.Max(patrolMinDistance, patrolArrivalDistance + 0.1f);
            float maxDistance = Mathf.Max(patrolRadius, minDistance);
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Mathf.Sqrt(Random.Range(minDistance * minDistance, maxDistance * maxDistance));
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Vector3 candidate = transform.position + new Vector3(offset.x, 0f, offset.y);
            if (!NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRadius, agent.areaMask) ||
                // NavMesh補正後も水平距離が設定範囲内か確認する。
                Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                    new Vector2(hit.position.x, hit.position.z)) < minDistance ||
                Vector2.Distance(new Vector2(transform.position.x, transform.position.z),
                    new Vector2(hit.position.x, hit.position.z)) > maxDistance ||
                !agent.CalculatePath(hit.position, reusablePath) ||
                reusablePath.status != NavMeshPathStatus.PathComplete)
            {
                continue;
            }

            if (agent.SetPath(reusablePath))
            {
                agent.isStopped = false;
                lastDestination = hit.position;
                hasDestination = true;
                return;
            }
        }
    }
    private void ChasePlayer()
    {
        // 高低差を含めた距離
        float distance = Vector3.Distance(
            transform.position,
            player.position
        );

        // 停止中の処理
        if (agent.isStopped)
        {
            // 停止距離に余裕を持たせる
            if (distance <= stopDistance + resumeMargin)
            {
                return;
            }
        }
        else if (distance <= stopDistance)
        {
            StopChase();
            return;
        }

        // プレイヤーの現在位置を追跡する。
        if (!NavMesh.SamplePosition(player.position, out NavMeshHit hit, searchRadius, agent.areaMask))
        {
            return;
        }

        Vector3 destination = hit.position;

        // 停止状態から復帰するか
        bool restarting = agent.isStopped;

        agent.isStopped = false;

        // 目的地がほとんど変わらない場合は
        // 経路を再計算しない
        if (!restarting &&
            hasDestination &&
            agent.hasPath &&
            Vector3.Distance(
                lastDestination,
                destination
            ) < destinationThreshold)
        {
            return;
        }

        // 経路を計算
        if (!agent.CalculatePath(
            destination,
            reusablePath))
        {
            return;
        }

        // 完全な経路か確認
        if (reusablePath.status !=
            NavMeshPathStatus.PathComplete)
        {
            return;
        }

        // 目的地を更新
        if (agent.SetDestination(destination))
        {
            lastDestination = destination;
            hasDestination = true;
        }
    }

    private void StopChase()
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return;
        }

        // 移動停止
        agent.isStopped = true;

        // 経路を削除
        agent.ResetPath();

        hasDestination = false;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugPath)
        {
            return;
        }

        NavMeshAgent navAgent =
            GetComponent<NavMeshAgent>();

        if (navAgent == null || !navAgent.isOnNavMesh)
        {
            return;
        }

        Vector3[] corners = navAgent.path.corners;

        // 経路を緑色で表示
        Gizmos.color = Color.green;

        for (int i = 0; i < corners.Length - 1; i++)
        {
            Gizmos.DrawLine(
                corners[i],
                corners[i + 1]
            );
        }
    }
}
