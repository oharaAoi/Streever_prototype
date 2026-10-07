using UnityEngine;

public class DeliveryAddress : MonoBehaviour
{

    // 配達金額
    [SerializeField]
    private int deliveryFree = 1000;

    // 配達した際の増える視聴者数
    [SerializeField]
    private int increaseViewership = 10;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int GetDeliveryFree() { 
        return deliveryFree;
    }

	public int GetIncreaseViewership() {
		return increaseViewership;
	}
}
