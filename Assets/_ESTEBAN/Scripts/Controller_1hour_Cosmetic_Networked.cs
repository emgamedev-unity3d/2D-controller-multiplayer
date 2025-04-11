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

    readonly NetworkVariable<int> direction = new(
        1,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    private readonly NetworkVariable<bool> isGrounded = new(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner);

    bool hasDoubleJump;

    //Added after the timer
    public Transform footFXPosition;
    public GameObject doubleJumpFX;
    public GameObject jumpDustFX;


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
        input = GetComponent<PlayerInput>();

        ownerNetworkAnimator = GetComponent<OwnerNetworkAnimator>();
        sprite = GetComponentInChildren<SpriteRenderer>(true);

        // do an initial check on sprite color, to make sure we spawn and
        //  start correctly
        if(!IsOwner && IsClient)
        {
            ApplySpriteColoringUpdates(isGrounded.Value);
        }
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
        ownerNetworkAnimator.Animator.SetBool("Grounded", isGrounded.Value);
    }

    void UpdateSpriteColoring()
    {
        isGrounded.Value = slideMovement.selectedCollider.IsTouching(groundFilter);

        ApplySpriteColoringUpdates(isGrounded.Value);
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

    void ProcessHorizontalMove()
    {
        float xVel = input.horizontal * speed;

        //If on the ground, use Slide
        if (isGrounded.Value)
        {
            var slideVelocity = new Vector2(xVel, rb.linearVelocity.y);
            rb.Slide(slideVelocity, Time.fixedDeltaTime, slideMovement);
        }
        else
        {
            rb.linearVelocityX = xVel;
        }

        if (input.horizontal * direction.Value < 0f)
            FlipDirection();

        ownerNetworkAnimator.Animator.SetFloat("Speed", Mathf.Abs(xVel));
    }

    void ProcessJump()
    {
        if (input.jumpPressed && (isGrounded.Value || hasDoubleJump))
        {
            rb.linearVelocityY = 0f;
            rb.AddForceY(jumpForce, ForceMode2D.Impulse);

            if (isGrounded.Value)
            {
                slideMovement.selectedCollider.enabled = false;

                CancelInvoke("EnableCollider");
                Invoke("EnableCollider", jumpColliderDisableTime);

                isGrounded.Value = false;

                CreateFirstJumpFX();

                // tell the other players that we've made our first jump,
                // and that they should also spawn the FX
                EverybodyElseCreateFirstJumpFX_Rpc();
            }
            else
            {
                hasDoubleJump = false;

                CreateSecondJumpFX();

                // tell the other players that we've made our double jump,
                // and that they should also spawn the FX
                EverybodyElseCreateSecondJumpFX_Rpc();
            }
        }
    }

    void CreateFirstJumpFX()
    {
        // Added after the timer
        var fx = Instantiate(
            jumpDustFX,
            footFXPosition.position,
            Quaternion.identity);

        Destroy(fx, .5f);
    }

    [Rpc(SendTo.NotMe, //send RPC to everyone except me
        Delivery = RpcDelivery.Unreliable)] 
        // ^ non-critical message. Not the end of the world if
        //   a particle FX is not spawned since the purpose is just
        //   cosmetic, and does not affect important gameplay
    void EverybodyElseCreateFirstJumpFX_Rpc()
    {
        CreateFirstJumpFX();
    }

    void CreateSecondJumpFX()
    {
        // Added after the timer
        var fx = Instantiate(
            doubleJumpFX,
            footFXPosition.position,
            Quaternion.identity);

        Destroy(fx, .5f);
    }

    [Rpc(SendTo.NotMe, Delivery = RpcDelivery.Unreliable)]
    void EverybodyElseCreateSecondJumpFX_Rpc()
    {
        CreateSecondJumpFX();
    }

    void EnableCollider()
    {
        slideMovement.selectedCollider.enabled = true;
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

    void EverybodyElseUpdateSpriteColoring(bool oldValue, bool newValue)
    {
        ApplySpriteColoringUpdates(newValue);
    }
}
