using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WindZone2D : MonoBehaviour
{
    [Header("Wind Settings")]
    [Tooltip("Positive pushes right, negative pushes left.")]
    [SerializeField] private float windForceX = -20f;

    [Tooltip("Extra damping on the player's X velocity (0..1).")]
    [Range(0f, 1f)]
    [SerializeField] private float windDrag = 0.08f;

    private void Reset()
    {
        var bc = GetComponent<BoxCollider2D>();
        bc.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController2D>();
        if (player != null)
            player.SetWind(windForceX, windDrag);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController2D>();
        if (player != null)
            player.ClearWind();
    }
}
