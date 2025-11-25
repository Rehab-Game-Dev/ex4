using UnityEngine;
using UnityEngine.SceneManagement;

public class GoalTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController2D player = other.GetComponent<PlayerController2D>();

        if (player != null)
        {
            Debug.Log("YOU WIN!");

            // מציג חלון ניצחון
            WinMessageUI.ShowWin();

            // אפשר גם לעשות Pause למשחק
            Time.timeScale = 0f;
        }
    }
}
