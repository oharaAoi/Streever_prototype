
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyChaser : MonoBehaviour {
	public enum EnemyType {
		Chase,     // 現在位置を追跡
		Intercept   // 未来位置に先回り
	}

	[Header("AIの役割")]
	[SerializeField] private EnemyType enemyType = EnemyType.Chase;

	[Header("迎撃設定")]
	[SerializeField, Min(0.1f)] private float maxPredictionTime = 4f;
	[SerializeField, Min(0f)] private float predictionOffset = 1.5f;
	[SerializeField, Min(0.1f)] private float minPredictSpeed = 0.5f;
	[SerializeField, Min(0f)] private float interceptTimeMargin = 0.25f;

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
	private Rigidbody playerRigidbody;
	private Vector3 previousPlayerPosition;
	private Vector3 measuredVelocity;
	private bool hasPreviousPlayerPosition;
	private Vector3 debugInterceptPosition;
	private bool hasDebugInterceptPosition;

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

	private void Awake() {
		agent = GetComponent<NavMeshAgent>();

		reusablePath = new NavMeshPath();

		agent.speed = chaseSpeed;
		agent.stoppingDistance = stopDistance;
		agent.acceleration = 30.0f;
		agent.angularSpeed = 360.0f;
	}

	private void Start() {
		// プレイヤーを取得
		if (player == null) {
			Player foundPlayer =
				FindFirstObjectByType<Player>();

			if (foundPlayer != null) {
				player = foundPlayer.transform;
			}
		}

		if (player == null) {
			Debug.LogError(
				"EnemyChaser: Playerが見つかりません",
				this
			);
			return;
		}

		playerRigidbody = player.GetComponent<Rigidbody>();
		previousPlayerPosition = player.position;
		hasPreviousPlayerPosition = true;

		// NavMesh上にいるか確認
		if (!agent.isOnNavMesh) {
			Debug.LogError(
				"EnemyChaser: EnemyがNavMesh上にいません",
				this
			);
		} else {
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

	private void Update() {
		if (player == null ||
			agent == null ||
			!agent.isOnNavMesh) {
			return;
		}

		// Rigidbodyがないプレイヤーにも対応するため位置差分から速度を推定する
		if (hasPreviousPlayerPosition && Time.deltaTime > 0f)
			measuredVelocity = (player.position - previousPlayerPosition) / Time.deltaTime;
		previousPlayerPosition = player.position;
		hasPreviousPlayerPosition = true;

		float distance = Vector3.Distance(transform.position, player.position);
		if (isChasing) {
			outsideRangeTimer = distance >= chaseDistance
				? outsideRangeTimer + Time.deltaTime : 0f;
			if (distance >= chaseDistance && outsideRangeTimer >= patrolStartDelay) {
				SetChasing(false);
			}
		} else {
			nonChaseTimer += Time.deltaTime;
			if (distance <= chaseDistance || nonChaseTimer >= chaseRestartDelay) {
				SetChasing(true);
			}
		}

		updateTimer += Time.deltaTime;
		if (updateTimer < updateInterval) {
			return;
		}

		updateTimer = 0f;
		if (isChasing) {
			if (enemyType == EnemyType.Intercept)
				InterceptPlayer();
			else {
				hasDebugInterceptPosition = false;
				ChasePlayer();
			}
		} else {
			Patrol();
		}
	}

	private void SetChasing(bool chasing) {
		isChasing = chasing;
		outsideRangeTimer = 0f;
		nonChaseTimer = 0f;
		StopChase();
		agent.speed = chasing ? chaseSpeed : patrolSpeed;
		agent.stoppingDistance = chasing ? stopDistance : patrolArrivalDistance;
		updateTimer = updateInterval;
	}

	private void Patrol() {
		if (agent.pathPending) {
			return;
		}

		if (hasDestination && agent.hasPath &&
			agent.pathStatus == NavMeshPathStatus.PathComplete &&
			agent.remainingDistance > patrolArrivalDistance) {
			return;
		}

		// 到達可能なランダム地点へ向かい、到着後に次の地点を選ぶ。
		for (int i = 0; i < 10; i++) {
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
				reusablePath.status != NavMeshPathStatus.PathComplete) {
				continue;
			}

			if (agent.SetPath(reusablePath)) {
				agent.isStopped = false;
				lastDestination = hit.position;
				hasDestination = true;
				return;
			}
		}
	}
	private void ChasePlayer() {
		// プレイヤーに近い場合は停止。既存の停止・再開の挙動を維持する。
		if (ShouldStopNearPlayer())
			return;

		MoveToDestination(player.position);
	}

	private void InterceptPlayer() {
		// 接近できた場合は追跡役と同じように停止
		if (ShouldStopNearPlayer())
			return;

		Vector3 velocity = playerRigidbody != null
			? playerRigidbody.linearVelocity
			: measuredVelocity;
		velocity.y = 0f;

		float playerSpeed = velocity.magnitude;
		if (playerSpeed < minPredictSpeed) {
			hasDebugInterceptPosition = false;
			MoveToDestination(player.position);
			return;
		}

		// 敵とプレイヤーの距離から、無理のない予測秒数を求める
		float horizontalDistance = Vector2.Distance(
			new Vector2(transform.position.x, transform.position.z),
			new Vector2(player.position.x, player.position.z));
		float predictTime = Mathf.Clamp(
			horizontalDistance / Mathf.Max(agent.speed, 0.01f),
			0f, maxPredictionTime);

		Vector3 predicted = player.position +
			velocity * predictTime + velocity.normalized * predictionOffset;

		hasDebugInterceptPosition = true;
		debugInterceptPosition = predicted;

		// 迎撃地点が有効かと、そこにプレイヤーより先に着けるかを調べる
		if (NavMesh.SamplePosition(predicted, out NavMeshHit hit,
				searchRadius, agent.areaMask) &&
			agent.CalculatePath(hit.position, reusablePath) &&
			reusablePath.status == NavMeshPathStatus.PathComplete) {
			float pathLength = 0f;
			Vector3[] corners = reusablePath.corners;
			for (int i = 1; i < corners.Length; i++)
				pathLength += Vector3.Distance(corners[i - 1], corners[i]);

			float enemyArrival = pathLength / Mathf.Max(agent.speed, 0.01f);
			float playerArrival = Vector3.Distance(
				player.position, hit.position) / playerSpeed;

			// 先に到着できる場合のみ迎撃する。無理なら通常追跡へ戻る。
			if (enemyArrival + interceptTimeMargin < playerArrival) {
				MoveToDestination(hit.position);
				return;
			}
		}

		MoveToDestination(player.position);
	}

	private bool ShouldStopNearPlayer() {
		float distance = Vector3.Distance(transform.position, player.position);
		if (agent.isStopped)
			return distance <= stopDistance + resumeMargin;

		if (distance <= stopDistance) {
			StopChase();
			return true;
		}
		return false;
	}

	// 追跡・迎撃で共通の経路設定
	private void MoveToDestination(Vector3 target) {
		if (!NavMesh.SamplePosition(target, out NavMeshHit hit,
				searchRadius, agent.areaMask))
			return;

		Vector3 destination = hit.position;
		bool restarting = agent.isStopped;
		agent.isStopped = false;

		if (!restarting && hasDestination && agent.hasPath &&
			Vector3.Distance(lastDestination, destination) < destinationThreshold)
			return;

		if (!agent.CalculatePath(destination, reusablePath) ||
			reusablePath.status != NavMeshPathStatus.PathComplete)
			return;

		if (agent.SetDestination(destination)) {
			lastDestination = destination;
			hasDestination = true;
		}
	}

	private void StopChase() {
		if (agent == null || !agent.isOnNavMesh) {
			return;
		}

		// 移動停止
		agent.isStopped = true;

		// 経路を削除
		agent.ResetPath();

		hasDestination = false;
	}

	private void OnDrawGizmos() {
		if (!showDebugPath) {
			return;
		}

		if (Application.isPlaying && enemyType == EnemyType.Intercept &&
			hasDebugInterceptPosition) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(debugInterceptPosition, 1.5f);
			Gizmos.DrawLine(transform.position, debugInterceptPosition);
		}

		NavMeshAgent navAgent =
			GetComponent<NavMeshAgent>();

		if (navAgent == null || !navAgent.isOnNavMesh) {
			return;
		}

		Vector3[] corners = navAgent.path.corners;

		// 経路を緑色で表示
		Gizmos.color = Color.green;

		for (int i = 0; i < corners.Length - 1; i++) {
			Gizmos.DrawLine(
				corners[i],
				corners[i + 1]
			);
		}
	}
}
