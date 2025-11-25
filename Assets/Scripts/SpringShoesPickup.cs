using UnityEngine;

public class SpringShoesPickup : MonoBehaviour
{
    [Tooltip("How long the spring shoes last. Set to 0 or negative for infinite.")]
    [SerializeField] private float durationSeconds = 0f;

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController2D>();
        if (player == null) return;

        player.ActivateSpringShoes(durationSeconds);
        Destroy(gameObject);
    }
}
