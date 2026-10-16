
using UnityEngine;

public class WorldTargetUI : MonoBehaviour {

	[Header("追従対象")]
	[SerializeField]
	private Transform target;

	[SerializeField]
	private Camera targetCamera;

	[Header("UI")]
	[SerializeField]
	private RectTransform marker;

	[SerializeField]
	private RectTransform canvasRect;

	[Header("設定")]
	[SerializeField]
	private Vector3 worldOffset = new Vector3(0, 2, 0);

	[SerializeField]
	private float screenMargin = 40.0f;

	public void Initialize(
	Transform target,
	Camera targetCamera,
	RectTransform canvasRect) {

		this.target = target;
		this.targetCamera = targetCamera;
		this.canvasRect = canvasRect;
	}

	private void LateUpdate() {
		if (target == null) {
			Destroy(gameObject);
			return;
		}

		UpdateMarkerPosition();
	}
	private void UpdateMarkerPosition() {
		if (target == null || targetCamera == null ||
			marker == null || canvasRect == null) {
			return;
		}

		// オブジェクトのワールド座標
		Vector3 worldPosition = target.position + worldOffset;

		// ワールド座標をビューポート座標に変換
		Vector3 viewportPos =
			targetCamera.WorldToViewportPoint(worldPosition);

		// カメラより後ろにある場合は方向を反転
		if (viewportPos.z < 0) {
			viewportPos.x = 1.0f - viewportPos.x;
			viewportPos.y = 1.0f - viewportPos.y;
		}

		// 画面内にいるか判定
		bool isOnScreen =
			viewportPos.z > 0 &&
			viewportPos.x >= 0 && viewportPos.x <= 1 &&
			viewportPos.y >= 0 && viewportPos.y <= 1;

		// 中心からの方向
		Vector2 direction = new Vector2(
			viewportPos.x - 0.5f,
			viewportPos.y - 0.5f
		);

		// Canvas上での座標
		Vector2 canvasPos = new Vector2(
			direction.x * canvasRect.rect.width,
			direction.y * canvasRect.rect.height
		);

		// 画面外の場合、端に固定
		if (!isOnScreen) {
			float halfWidth =
				canvasRect.rect.width * 0.5f - screenMargin;

			float halfHeight =
				canvasRect.rect.height * 0.5f - screenMargin;

			// 中心から伸ばした線と画面端との交点を求める
			float scaleX = Mathf.Abs(canvasPos.x) > 0.0001f
				? halfWidth / Mathf.Abs(canvasPos.x)
				: float.PositiveInfinity;

			float scaleY = Mathf.Abs(canvasPos.y) > 0.0001f
				? halfHeight / Mathf.Abs(canvasPos.y)
				: float.PositiveInfinity;

			float scale = Mathf.Min(scaleX, scaleY);

			// 方向を維持して画面端に配置
			if (direction.sqrMagnitude > 0.000001f) {
				canvasPos *= scale;
			} else {
				canvasPos = new Vector2(0, -halfHeight);
			}
		}

		// UI位置を更新
		marker.anchoredPosition = canvasPos;
	}
}
