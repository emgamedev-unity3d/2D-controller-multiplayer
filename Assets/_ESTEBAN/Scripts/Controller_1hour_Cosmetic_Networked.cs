using Unity.Netcode;
using UnityEngine;

public class Controller_1hour_Cosmetic_Networked : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float jumpColliderDisableTime = 0.1f;
    public ContactFilter2D groundFilter;
    public Rigidbody2D.SlideMovement slideMovement = new();

    OwnerNetworkAnimator ownerNetworkAnimator;
    SpriteRenderer sprite;
    Rigidbody2D rb;
    PlayerInput input;

    int direction = 1;
    bool isGrounded;
    bool hasDoubleJump;

    //Added after the timer
    public Transform footFXPosition;
    public GameObject doubleJumpFX;
    public GameObject jumpDustFX;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInput>();

        ownerNetworkAnimator = GetComponent<OwnerNetworkAnimator>();
        sprite = GetComponentInChildren<SpriteRenderer>(true);
    }

    void FixedUpdate()
    {
        if (!IsOwner)
            return;

        UpdateSpriteColoring();

        ProcessHorizontalMove();
        ProcessJump();

        //Added after the timer
        ownerNetworkAnimator.Animator.SetFloat("vSpeed", rb.linearVelocityY);
        ownerNetworkAnimator.Animator.SetBool("Grounded", isGrounded);
    }

    void UpdateSpriteColoring()
    {
        isGrounded = slideMovement.selectedCollider.IsTouching(groundFilter);

        ApplySpriteColoringUpdates(isGrounded);

        EverybodyElseUpdateSpriteColoringRpc(isGrounded);
    }

    void ApplySpriteColoringUpdates(bool isGrounded)
    {
        if (isGrounded)
        {
            hasDoubleJump = true;
            sprite.color = Color.green;
        }
        else
        {
            sprite.color = Color.red;
        }
    }

    [Rpc(
        target: SendTo.NotMe,
        Delivery = RpcDelivery.Reliable)]
    void EverybodyElseUpdateSpriteColoringRpc(bool isGrounded)
    {
        ApplySpriteColoringUpdates(isGrounded);
    }

    void ProcessHorizontalMove()
    {
        float xVel = input.horizontal * speed;

        //If on the ground, use Slide
        if (isGrounded)
        {
            var slideVelocity = new Vector2(xVel, rb.linearVelocity.y);
            rb.Slide(slideVelocity, Time.fixedDeltaTime, slideMovement);
        }
        else
        {
            rb.linearVelocityX = xVel;
        }

        if (input.horizontal * direction < 0f)
            FlipDirection();

        ownerNetworkAnimator.Animator.SetFloat("Speed", Mathf.Abs(xVel));
    }

    void ProcessJump()
    {
        if (input.jumpPressed && (isGrounded || hasDoubleJump))
        {
            rb.linearVelocityY = 0f;
            rb.AddForceY(jumpForce, ForceMode2D.Impulse);

            if (isGrounded)
            {
                slideMovement.selectedCollider.enabled = false;

                CancelInvoke("EnableCollider");
                Invoke("EnableCollider", jumpColliderDisableTime);

                isGrounded = false;

                // Added after the timer
                var fx = Instantiate(
                    jumpDustFX,
                    footFXPosition.position,
                    Quaternion.identity);

                Destroy(fx, .5f);
            }
            else
            {
                hasDoubleJump = false;

                // Added after the timer
                var fx = Instantiate(
                    doubleJumpFX,
                    footFXPosition.position,
                    Quaternion.identity);

                Destroy(fx, .5f);
            }
        }
    }

    void EnableCollider()
    {
        slideMovement.selectedCollider.enabled = true;
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
}
