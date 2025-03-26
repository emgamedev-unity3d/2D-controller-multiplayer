using Unity.Netcode;
using UnityEngine;

public class Controller_1hour_Melv_Networked : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public bool isGrounded;
    public Collider2D groundedCollider;
    public ContactFilter2D groundedFilter;
    public Rigidbody2D.SlideMovement slideMovement = new();

    //Added after the timer
    Animator anim;
    SpriteRenderer sprite;
    int direction = 1;
    float horizontalInput;

    Rigidbody2D rb;

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
        if (horizontalInput * direction < 0f)
            FlipDirection();

        // If we're grounded then we can jump.
        // NOTE: We're using a Dynamic body, so we use linear-velocity for jumping.
        if (isGrounded && Input.GetButtonDown("Jump"))
            rb.linearVelocityY = jumpForce;
    }

    void FixedUpdate()
    {
        if (!IsOwner)
            return;

        // Are we grounded?
        isGrounded = groundedCollider.IsTouching(groundedFilter);

        UpdateSpriteColoring();

        // Do we have any user movement?
        var hasInputMovement = Mathf.Abs(horizontalInput) > 0.0001f;
        if (hasInputMovement)
        {
            // Yes, do calculate the horizontal speed.
            var horizontalSpeed = speed * horizontalInput;

            // Are we grounded and not jumping/falling?
            var isJumpingFalling = Mathf.Abs(rb.linearVelocityY) >= 0.01f;
            if (isGrounded && !isJumpingFalling)
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
            if (!isGrounded)
            {
                // Yes, so use Dynamic body motion.
                rb.linearVelocityX = horizontalSpeed;
            }
        }
    }

    void FlipDirection()
    {
        direction *= -1;

        if (direction > 0)
            sprite.flipX = false;
        else
            sprite.flipX = true;

        EverybodyElseFlipDirectionRpc();
    }

    [Rpc(
    target: SendTo.NotMe,
    Delivery = RpcDelivery.Reliable)]
    void EverybodyElseFlipDirectionRpc()
    {
        sprite.flipX = !sprite.flipX;
    }

    void UpdateSpriteColoring()
    {
        // Visually indicate if we're grounded or not.
        sprite.color = isGrounded ? Color.green : Color.red;

        EverybodyElseUpdateSpriteColoringRpc(isGrounded);
    }

    [Rpc(
        target: SendTo.NotMe,
        Delivery = RpcDelivery.Reliable)]
    void EverybodyElseUpdateSpriteColoringRpc(bool isGrounded)
    {
        // Visually indicate if we're grounded or not.
        sprite.color = isGrounded ? Color.green : Color.red;
    }
}