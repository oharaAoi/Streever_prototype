
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnemyCollision : MonoBehaviour
{
    [Header("遷移先のシーン名")]
    [SerializeField]
    private string nextSceneName = "ResultScene";

    [Header("プレイヤーとの接触判定")]
    [SerializeField]
    private string playerTag = "Player";

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        // すでにシーン遷移中なら処理しない
        if (isTransitioning)
        {
            return;
        }

        // プレイヤーか確認
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        isTransitioning = true;

        Debug.Log("鬼に捕まりました");

        // シーン遷移
        SceneManager.LoadScene(nextSceneName);
    }
}
