using UnityEngine;

public class GameManager : MonoBehaviour
{

    [SerializeField]
    private int gameTime = 120;

    private bool isClear = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		isClear = false;
	}

    // Update is called once per frame
    void Update()
    {
        if(gameTime > Time.time) {
            isClear = true;
		}
    }
}
