using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BuildingGenerator : MonoBehaviour
{
    [Header("生成数")]
    public int countX = 9;
    public int countZ = 11;

    [Header("ビル同士の間隔")]
    public float spacingX = 110f;
    public float spacingZ = 90f;

    [Header("ビルの横幅")]
    public Vector2 widthRange = new Vector2(40f, 60f);

    [Header("ビルの奥行き")]
    public Vector2 depthRange = new Vector2(40f, 60f);

    [Header("ビルの高さ")]
    public Vector2 heightRange = new Vector2(60f, 150f);

    [Header("中央道路の判定幅")]
    public float roadHalfWidth = 30f;

    [Header("中央道路の細さ")]
    [Range(0.5f, 1.2f)]
    public float roadScale = 0.8f;

	[Header("配達先の設定")]
	[SerializeField]
	private GameObject deliveryAddressPrefab;

	[Header("UI Prefab")]
	[SerializeField]
	private WorldTargetUI markerPrefab;

	[Header("Canvas")]
	[SerializeField]
	private RectTransform canvasRect;

	[Header("Camera")]
	[SerializeField]
	private Camera targetCamera;

	[Header("ポール")]
	[SerializeField]
	private GameObject pole;

	[SerializeField]
	private float poleOffset = 10.0f;

	[SerializeField]
	[Min(0)]
	private int deliveryAddressCount = 5;

	private readonly List<GameObject> buildings = new();
	private readonly List<DeliveryAddress> activeAddresses = new();

	private void Start() {
		InitializeBuildings();

		for (int i = 0; i < deliveryAddressCount; i++) {
			SpawnDeliveryAddress();
		}
	}

	private void InitializeBuildings() {
		buildings.Clear();

		foreach (Transform child in transform) {
			if (child.name.StartsWith("Building_")) {
				buildings.Add(child.gameObject);
			}
		}

		Debug.Log($"建物を{buildings.Count}個取得しました");
	}

	[ContextMenu("Generate Buildings")]
    public void GenerateBuildings()
    {
        ClearBuildings();

		for (int x = 0; x < countX; x++)
        {
            for (int z = 0; z < countZ; z++)
            {
                float indexX = x - (countX - 1) * 0.5f;
                float indexZ = z - (countZ - 1) * 0.5f;

                float posX = 0.0f;
                float posZ = 0.0f;

                // X方向
                if (indexX != 0.0f)
                {
                    posX =
                        Mathf.Sign(indexX) *
                        (
                            spacingX * roadScale +
                            (Mathf.Abs(indexX) - 1.0f) * spacingX
                        );
                }

                // Z方向
                if (indexZ != 0.0f)
                {
                    posZ =
                        Mathf.Sign(indexZ) *
                        (
                            spacingZ * roadScale +
                            (Mathf.Abs(indexZ) - 1.0f) * spacingZ
                        );
                }

                // 中央の十字部分にはビルを生成しない
                if (Mathf.Abs(posX) < roadHalfWidth ||
                    Mathf.Abs(posZ) < roadHalfWidth)
                {
                    continue;
                }

                float width =
                    Random.Range(widthRange.x, widthRange.y);

                float depth =
                    Random.Range(depthRange.x, depthRange.y);

                float height =
                    Random.Range(heightRange.x, heightRange.y);

                GameObject building =
                    GameObject.CreatePrimitive(PrimitiveType.Cube);

				building.name = $"Building_{x}_{z}";

                building.transform.SetParent(transform);

                building.transform.localPosition =
                    new Vector3(
                        posX,
                        height * 0.5f,
                        posZ
                    );

                building.transform.localScale =
                    new Vector3(
                        width,
                        height,
                        depth
                    );

				// リストに追加
				buildings.Add(building);

				// 建物1個につきPoleを1個生成
				SpawnPole(building, width, depth);
			}
        }
	}

    [ContextMenu("Clear Buildings")]
    public void ClearBuildings()
    {
		// 生成済みの建物を削除
		for (int i = transform.childCount - 1; i >= 0; i--) {
			DestroyImmediate(transform.GetChild(i).gameObject);
		}

		// 生成済みのUIを削除
		if (canvasRect != null) {
			WorldTargetUI[] markers =
				canvasRect.GetComponentsInChildren<WorldTargetUI>(true);

			foreach (WorldTargetUI marker in markers) {
				if (marker != null) {
					DestroyImmediate(marker.gameObject);
				}
			}
		}

		// 管理リストをクリア
		buildings.Clear();
		activeAddresses.Clear();
	}

	// 配達先を1個生成
	public void SpawnDeliveryAddress() {
		if (deliveryAddressPrefab == null || buildings.Count == 0)
			return;

		// 現在配達先が配置されていないビルを取得
		List<GameObject> candidates = new();

		foreach (GameObject building in buildings) {
			if (building == null)
				continue;

			bool hasAddress = false;

			foreach (DeliveryAddress address in activeAddresses) {
				if (address != null &&
					address.transform.parent == building.transform) {
					hasAddress = true;
					break;
				}
			}

			if (!hasAddress)
				candidates.Add(building);
		}

		if (candidates.Count == 0)
			return;

		// ランダムにビルを選択
		int index = Random.Range(0, candidates.Count);
		GameObject targetBuilding = candidates[index];

		// 配達先を生成
		GameObject deliveryAddress = Instantiate(
			deliveryAddressPrefab,
			targetBuilding.transform
		);

		deliveryAddress.transform.localPosition =
			new Vector3(0f, 0.5f, 0f);

		deliveryAddress.transform.localRotation =
			Quaternion.identity;

		// Prefabに付いているDeliveryAddressを取得
		DeliveryAddress addressComponent =
			deliveryAddress.GetComponent<DeliveryAddress>();

		if (addressComponent != null) {
			addressComponent.Initialize(this);
			activeAddresses.Add(addressComponent);
		}

		// UIをCanvasの子として生成
		if (markerPrefab != null &&
			canvasRect != null &&
			targetCamera != null) {

			WorldTargetUI marker = Instantiate(
				markerPrefab,
				canvasRect
			);

			// 配達先を追従対象に設定
			marker.Initialize(
				deliveryAddress.transform,
				targetCamera,
				canvasRect
			);
		}
	}

	// 配達完了時に呼び出す
	public void CompleteDelivery(DeliveryAddress address) {
		if (address == null)
			return;

		// 管理リストから削除
		if (!activeAddresses.Remove(address))
			return;

		// 配達先を削除
		Destroy(address.gameObject);

		// 新しい配達先を1個追加
		SpawnDeliveryAddress();
	}

	// 建物1個につきPoleを1個生成する
	private void SpawnPole(GameObject building, float width, float depth) {
		if (pole == null)
			return;

		float halfWidth = width * 0.5f;
		float halfDepth = depth * 0.5f;

		Vector3 localPosition = Vector3.zero;
		Quaternion rotation = Quaternion.identity;

		// 4つの壁からランダムに1つ選択
		int side = Random.Range(0, 4);

		switch (side) {
			case 0: // 前面
				localPosition = new Vector3(
					Random.Range(-halfWidth, halfWidth),
					0.0f,
					halfDepth + poleOffset
				);
				break;

			case 1: // 背面
				localPosition = new Vector3(
					Random.Range(-halfWidth, halfWidth),
					0.0f,
					-halfDepth - poleOffset
				);
				break;

			case 2: // 右側
				localPosition = new Vector3(
					halfWidth + poleOffset,
					0.0f,
					Random.Range(-halfDepth, halfDepth)
				);
				break;

			case 3: // 左側
				localPosition = new Vector3(
					-halfWidth - poleOffset,
					0.0f,
					Random.Range(-halfDepth, halfDepth)
				);
				break;
		}

		// 建物の底面中心を基準に配置
		Vector3 position = new Vector3(
			building.transform.position.x + localPosition.x,
			building.transform.position.y
				- building.transform.localScale.y * 0.5f,
			building.transform.position.z + localPosition.z
		);

		// Pole生成
		GameObject obj = Instantiate(pole, position, rotation, transform);
	}
}