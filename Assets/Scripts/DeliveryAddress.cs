using UnityEngine;

public class DeliveryAddress : MonoBehaviour
{

    // 配達金額
    [SerializeField]
    private int deliveryFree = 1000;

    // 配達した際の増える視聴者数
    [SerializeField]
    private int increaseViewership = 10;

	private BuildingGenerator generator;

	private bool isCompleted = false;

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public void Initialize(BuildingGenerator generator) {
		this.generator = generator;
	}

	private void OnTriggerEnter(Collider other) {
		if (isCompleted)
			return;

		// プレイヤーかどうかを判定
		if (!other.CompareTag("Player"))
			return;

		isCompleted = true;

		// 配達完了を通知
		generator?.CompleteDelivery(this);
	}

	public int GetDeliveryFree() { 
        return deliveryFree;
    }

	public int GetIncreaseViewership() {
		return increaseViewership;
	}
}
