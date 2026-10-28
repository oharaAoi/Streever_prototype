
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

        // 追跡者のUIマーカーを生成
        //if (markerPrefab != null &&
        //    canvasRect != null &&
        //    targetCamera != null)
        //{
        //    marker = Instantiate(
        //        markerPrefab,
        //        canvasRect
        //    );

        //    marker.Initialize(
        //        transform,
        //        targetCamera,
        //        canvasRect
        //    );
        //}

        // 最初の経路設定を遅らせない
        updateTimer = updateInterval;
    }

    private void Update()
    {
        // UIマーカー更新
        //UpdateMarkerScale();

        if (player == null ||
            agent == null ||
            !agent.isOnNavMesh)
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

        // 追跡可能距離を超えた場合
        if (distance > chaseDistance)
        {
            StopChase();
            return;
        }

        // プレイヤーの高さを含めて
        // NavMesh上の目的地を取得
        if (!NavMesh.SamplePosition(
            player.position,
            out NavMeshHit hit,
            searchRadius,
            agent.areaMask))
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

    private void UpdateMarkerScale()
    {
        if (marker == null || player == null)
        {
            return;
        }

        // プレイヤーとの距離
        float distance = Vector3.Distance(
            player.position,
            transform.position
        );

        // 距離100～500を0～1に変換
        float normalizedDistance = Mathf.InverseLerp(
            100.0f,
            500.0f,
            distance
        );

        // 距離に応じてマーカーの大きさを変更
        float scale = Mathf.Lerp(
            1.0f,
            0.5f,
            normalizedDistance
        );

        marker.SetScale(Vector3.one * scale);
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
