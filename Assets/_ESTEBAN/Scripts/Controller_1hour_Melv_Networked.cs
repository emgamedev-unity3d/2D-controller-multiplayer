using Unity.Netcode;
using UnityEngine;

public class Controller_1hour_Melv_Networked : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;

    private readonly NetworkVariable<bool> isGrounded = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    public Collider2D groundedCollider;
    public ContactFilter2D groundedFilter;
    public Rigidbody2D.SlideMovement slideMovement = new();

    //Added after the timer
    Animator anim;
    SpriteRenderer sprite;
    readonly NetworkVariable<int> direction = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    float horizontalInput;

    Rigidbody2D rb;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner && IsClient)
        {
            direction.OnValueChanged += EverybodyElseFlipDirection;
            isGrounded.OnValueChanged += EverybodyElseUpdateSpriteColoring;
        }

        base.OnNetworkSpawn();
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        //Added after the timer
        anim = GetComponentInChildren<Animator>(true);
        sprite = GetComponentInChildren<SpriteRenderer>(false);
    }


    void Update()
    {
        if (!IsOwner)
            return;

        // Gather horizontal movement.
        horizontalInput = Input.GetAxisRaw("Horizontal");

        // Flip appropriately.
        if (horizontalInput * direction.Value < 0f)
            FlipDirection();

        // If we're grounded then we can jump.
        // NOTE: We're using a Dynamic body, so we use linear-velocity for jumping.
        if (isGrounded.Value && Input.GetButtonDown("Jump"))
            rb.linearVelocityY = jumpForce;
    }

    void FixedUpdate()
    {
        if (!IsOwner)
            return;

        // Are we grounded?
        isGrounded.Value = groundedCollider.IsTouching(groundedFilter);

        UpdateSpriteColoring();

        // Do we have any user movement?
        var hasInputMovement = Mathf.Abs(horizontalInput) > 0.0001f;
        if (hasInputMovement)
        {
            // Yes, do calculate the horizontal speed.
            var horizontalSpeed = speed * horizontalInput;

            // Are we grounded and not jumping/falling?
            var isJumpingFalling = Mathf.Abs(rb.linearVelocityY) >= 0.01f;
            if (isGrounded.Value && !isJumpingFalling)
            {
                // Yes, so perform a slide.
                var slideVelocity = new Vector2(horizontalSpeed, rb.linearVelocity.y);
                rb.Slide(slideVelocity, Time.fixedDeltaTime, slideMovement);

                Debug.Log(slideVelocity);

                // Reset any Dynamic motion.
                rb.linearVelocity = Vector2.zero;

                return;
            }

            // Are we in the air?
            // NOTE: We only do this if we want horizontal movement when jumping/falling.
            if (!isGrounded.Value)
            {
                // Yes, so use Dynamic body motion.
                rb.linearVelocityX = horizontalSpeed;
            }
        }
    }

    void FlipDirection()
    {
        direction.Value *= -1;

        if (direction.Value > 0)
            sprite.flipX = false;
        else
            sprite.flipX = true;
    }

    void EverybodyElseFlipDirection(int oldValue, int newValue)
    {
        if (newValue > 0)
            sprite.flipX = false;
        else
            sprite.flipX = true;
    }

    void UpdateSpriteColoring()
    {
        // Visually indicate if we're grounded or not.
        sprite.color = isGrounded.Value ? Color.green : Color.red;
    }

    void EverybodyElseUpdateSpriteColoring(bool oldValue, bool newValue)
    {
        // Visually indicate if we're grounded or not.
        sprite.color = newValue ? Color.green : Color.red;
    }
}