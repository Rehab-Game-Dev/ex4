
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpImpulse = 10f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Fall Damage (Broken Leg) - distance based")]
    [SerializeField] private float hurtDurationSeconds = 5f;
    [Tooltip("0.5 = 2x slower")]
    [SerializeField] private float hurtSpeedMultiplier = 0.5f;

    [Header("Spring Shoes")]
    [Tooltip("2 = double jump height")]
    [SerializeField] private float springJumpHeightMultiplier = 2f;
    [SerializeField] private bool springPreventsFallDamage = true;

    [Header("Pole Slide (AUTO + Sticky)")]
    [SerializeField] private Transform poleCheck;
    [SerializeField] private Vector2 poleCheckSize = new Vector2(0.2f, 0.9f);
    [SerializeField] private LayerMask poleLayer;

    [Tooltip("Max fall speed while sliding (positive number).")]
    [SerializeField] private float slideMaxFallSpeed = 6f;

    [Tooltip("Minimum downward speed while sliding (positive number). Prevents 'hugging' the pole in air.")]
    [SerializeField] private float slideMinDownSpeed = 1.5f;

    [Tooltip("Gravity scale while sliding (lower = slower fall).")]
    [SerializeField] private float slideGravityScale = 0.3f;

    [Tooltip("How strongly you snap to the pole X while sliding (bigger = stickier).")]
    [SerializeField] private float poleSnapSpeed = 25f;

    [Header("Debug")]
    [SerializeField] private bool debugShowOnScreen = true;
    [SerializeField] private bool debugLogLanding = true;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float baseGravityScale;

    private bool isGrounded;
    private bool wasGrounded;

    private Collider2D poleTouchNow;
    private Collider2D latchedPole;
    private bool isSliding;

    // Peak height tracking for fall distance
    private float peakYWhileAirborne;

    // Timers/states
    private float hurtTimer;
    private bool springShoesActive;
    private float springShoesTimer;

    // Wind
    private float windForceX;
    private float windDrag01;

    // Once you ever touched/slid a pole during this airtime, the next landing is SAFE
    private bool usedPoleThisAirTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        baseGravityScale = rb.gravityScale;
        rb.freezeRotation = true;
    }

    private void Update()
    {
        wasGrounded = isGrounded;
        isGrounded = CheckGrounded();
        poleTouchNow = FindPoleTouching();

        UpdatePeakY();
        UpdateTimers();

        HandleLandingFallDamage();
        HandleAutoPoleSlideState();
        HandleJumpInput();
        UpdateFacing();

        // Clear the "used pole" flag only after we've been grounded for >1 frame
        if (isGrounded && wasGrounded)
            usedPoleThisAirTime = false;
    }

    private void FixedUpdate()
    {
        ApplyHorizontalMovement();
        ApplyWindForces();
        ApplyPoleSlidePhysics();
    }

    private bool CheckGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private Collider2D FindPoleTouching()
    {
        if (poleCheck == null) return null;
        return Physics2D.OverlapBox(poleCheck.position, poleCheckSize, 0f, poleLayer);
    }

    private void UpdateTimers()
    {
        if (hurtTimer > 0f) hurtTimer -= Time.deltaTime;

        if (springShoesActive && springShoesTimer > 0f)
        {
            springShoesTimer -= Time.deltaTime;
            if (springShoesTimer <= 0f)
                springShoesActive = false;
        }
    }

    private void UpdatePeakY()
    {
        float y = transform.position.y;

        // DON'T reset peak on the landing frame
        if (isGrounded && wasGrounded)
        {
            peakYWhileAirborne = y;
            return;
        }

        // Just left ground
        if (wasGrounded && !isGrounded)
            peakYWhileAirborne = y;

        // Track max height while airborne
        if (!isGrounded && y > peakYWhileAirborne)
            peakYWhileAirborne = y;
    }

    private float GetCurrentJumpHeightDistance()
    {
        // h = v^2 / (2g), with impulse giving deltaV = impulse/mass.
        float heightMultiplier = springShoesActive ? springJumpHeightMultiplier : 1f;
        float impulseMultiplier = Mathf.Sqrt(Mathf.Max(0.01f, heightMultiplier));

        float v0 = (jumpImpulse * impulseMultiplier) / Mathf.Max(0.0001f, rb.mass);
        float g = Mathf.Abs(Physics2D.gravity.y) * Mathf.Max(0.0001f, baseGravityScale);

        return (v0 * v0) / (2f * g);
    }

    private void HandleLandingFallDamage()
    {
        // Landing frame only
        if (!wasGrounded && isGrounded)
        {
            // SAFE if you used a pole at any point during this airtime
            if (usedPoleThisAirTime)
            {
                if (debugLogLanding)
                    Debug.Log("[Landing] SAFE (used pole this airtime) -> no fall damage", this);
                return;
            }

            // SAFE if spring shoes prevent fall damage
            if (springShoesActive && springPreventsFallDamage)
            {
                if (debugLogLanding)
                    Debug.Log("[Landing] SAFE (spring shoes) -> no fall damage", this);
                return;
            }

            float landingY = transform.position.y;
            float fallDistance = peakYWhileAirborne - landingY;
            float jumpHeightDistance = GetCurrentJumpHeightDistance();

            bool damaged = fallDistance > jumpHeightDistance;
            if (damaged) hurtTimer = hurtDurationSeconds;

            if (debugLogLanding)
                Debug.Log($"[Landing] fallDistance={fallDistance:0.00}, jumpHeight={jumpHeightDistance:0.00}, damaged={damaged}, hurtTimer={hurtTimer:0.00}", this);
        }
    }

    private void HandleAutoPoleSlideState()
    {
        if (isGrounded)
        {
            isSliding = false;
            latchedPole = null;
            rb.gravityScale = baseGravityScale;
            return;
        }

        // In air: touch pole => latch and start sliding automatically
        if (!isSliding && poleTouchNow != null)
        {
            isSliding = true;
            latchedPole = poleTouchNow;

            // Mark this airtime as safe
            usedPoleThisAirTime = true;
        }

        // If we left the pole collider and do NOT want to keep sliding, you can uncomment this:
        // if (isSliding && poleTouchNow == null) { isSliding = false; latchedPole = null; rb.gravityScale = baseGravityScale; }

        // Safety: if latched pole disappeared
        if (isSliding && latchedPole == null)
        {
            isSliding = false;
            rb.gravityScale = baseGravityScale;
        }
    }

    private void HandleJumpInput()
    {
        // Optional: allow jumping off the pole (still counts as "usedPoleThisAirTime")
        if (!Input.GetKeyDown(KeyCode.Space)) return;

        if (isGrounded || isSliding)
        {
            isSliding = false;
            latchedPole = null;
            rb.gravityScale = baseGravityScale;
            DoJump();
        }
    }

    private void DoJump()
    {
        float heightMultiplier = springShoesActive ? springJumpHeightMultiplier : 1f;
        float impulseMultiplier = Mathf.Sqrt(Mathf.Max(0.01f, heightMultiplier));

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * (jumpImpulse * impulseMultiplier), ForceMode2D.Impulse);
    }

    private float CurrentSpeedMultiplier()
    {
        return (hurtTimer > 0f) ? hurtSpeedMultiplier : 1f;
    }

    private void ApplyHorizontalMovement()
    {
        float input = Input.GetAxisRaw("Horizontal");
        float speedMul = CurrentSpeedMultiplier();

        // While sliding, ignore almost all horizontal movement (pole controls X)
        if (isSliding) speedMul *= 0.05f;

        rb.linearVelocity = new Vector2(input * moveSpeed * speedMul, rb.linearVelocity.y);
    }

    private void ApplyWindForces()
    {
        if (Mathf.Abs(windForceX) > 0.001f)
            rb.AddForce(new Vector2(windForceX, 0f), ForceMode2D.Force);

        if (windDrag01 > 0f)
        {
            Vector2 v = rb.linearVelocity;
            v.x *= Mathf.Clamp01(1f - windDrag01);
            rb.linearVelocity = v;
        }
    }

    private void ApplyPoleSlidePhysics()
    {
        if (!isSliding) return;
        if (latchedPole == null) return;

        rb.gravityScale = slideGravityScale;

        // Snap X to pole center (sticky feel)
        float targetX = latchedPole.bounds.center.x;
        float newX = Mathf.Lerp(rb.position.x, targetX, poleSnapSpeed * Time.fixedDeltaTime);
        rb.position = new Vector2(newX, rb.position.y);

        // IMPORTANT FIX:
        // If the player is pressing into the pole, friction can "hold" them in place and cancel gravity.
        // Force a minimum downward speed so they ALWAYS slide down.
        float y = rb.linearVelocity.y;
        y = Mathf.Min(y, -slideMinDownSpeed);     // ensure at least some downward movement
        y = Mathf.Max(y, -slideMaxFallSpeed);     // but don't exceed max fall speed (too fast)

        rb.linearVelocity = new Vector2(0f, y);
    }

    private void UpdateFacing()
    {
        if (sr == null) return;

        float vx = rb.linearVelocity.x;
        if (Mathf.Abs(vx) > 0.05f)
            sr.flipX = vx < 0f;
    }

    private void OnGUI()
    {
        if (!debugShowOnScreen) return;

        float vx = rb.linearVelocity.x;
        float vy = rb.linearVelocity.y;
        float speed = Mathf.Sqrt(vx * vx + vy * vy);

        GUI.Label(new Rect(10, 10, 1100, 22), $"Speed: {speed:0.00}   vx: {vx:0.00}   vy: {vy:0.00}");
        GUI.Label(new Rect(10, 32, 1100, 22), $"HurtTimer: {Mathf.Max(0f, hurtTimer):0.00}s   SpeedMult: {CurrentSpeedMultiplier():0.00}   Grounded: {isGrounded}");
        GUI.Label(new Rect(10, 54, 1100, 22), $"SpringShoes: {springShoesActive}   AutoPoleSliding: {isSliding}   usedPoleThisAirTime: {usedPoleThisAirTime}");
    }

    // ===== Public API =====
    // durationSeconds <= 0 => infinite
    public void ActivateSpringShoes(float durationSeconds)
    {
        springShoesActive = true;
        springShoesTimer = durationSeconds;
    }

    public void SetWind(float forceX, float drag01)
    {
        windForceX = forceX;
        windDrag01 = Mathf.Clamp01(drag01);
    }

    public void ClearWind()
    {
        windForceX = 0f;
        windDrag01 = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        if (poleCheck != null)
            Gizmos.DrawWireCube(poleCheck.position, poleCheckSize);
    }
}
