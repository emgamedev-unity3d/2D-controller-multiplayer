using Unity.Netcode;
using UnityEngine;

//NOTE:
// This code is written with presentation and educational purposes in mind,
//   not an example of real production-level code.
//
public class PlayerController_Networked : MonoBehaviour //NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;
    public float jumpColliderDisableTime = 0.1f;
    public ContactFilter2D groundFilter;
    public Rigidbody2D.SlideMovement slideMovement = new();

    SpriteRenderer sprite;
    Rigidbody2D rb;
    PlayerInput input;

    Animator Animator => animator;
    Animator animator;
    // TODO: uncomment to allow networked animations
    //OwnerNetworkAnimator ownerNetworkAnimator;

    int direction = 1;
    bool isGrounded;

    // TODO: uncomment to allow networked data
    //readonly NetworkVariable<int> direction = new(
    //    1,
    //    NetworkVariableReadPermission.Everyone,
    //    NetworkVariableWritePermission.Owner);
    //
    //private readonly NetworkVariable<bool> isGrounded = new(
    //    false,
    //    NetworkVariableReadPermission.Everyone,
    //    NetworkVariableWritePermission.Owner);

    private int Direction
    {
        get { return direction; }
        set { direction = value; }
    }
        
    private bool IsGrounded
    {
        get { return isGrounded; }
        set { isGrounded = value; }
    }

    bool hasDoubleJump;

    public Transform footFXPosition;
    public GameObject doubleJumpFX;
    public GameObject jumpDustFX;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<PlayerInput>();
        sprite = GetComponentInChildren<SpriteRenderer>(true);

        animator = GetComponentInChildren<Animator>(true);
        //ownerNetworkAnimator = GetComponent<OwnerNetworkAnimator>();

        // Set to true to enable use of netcode
#if false
        // do an initial check on sprite color, to make sure we spawn and
        //  start correctly
        if(!IsOwner && IsClient)
        {
            ApplySpriteColoringUpdates(isGrounded.Value);
        }
#endif
    }

    #region NETWORK_SPAWN
#if false
    public override void OnNetworkSpawn()
    {
        // Subscribe to the value changed events if we're not the owners of
        // the player object
        if (!IsOwner && IsClient)
        {
            direction.OnValueChanged += EverybodyElseFlipDirection;
            isGrounded.OnValueChanged += EverybodyElseUpdateSpriteColoring;
        }
    
        base.OnNetworkSpawn();
    }
#endif
    #endregion

    #region PHYSICS_UPDATE
    void FixedUpdate()
    {
        // TODO: uncomment to only update physics on owner's POV
        //if (!IsOwner)
        //    return;

        UpdateSpriteColoring();

        ProcessHorizontalMove();
        ProcessJump();

        //Added after the timer
        Animator.SetFloat("vSpeed", rb.linearVelocityY);
        Animator.SetBool("Grounded", IsGrounded);
    }

    void UpdateSpriteColoring()
    {
        IsGrounded = slideMovement.selectedCollider.IsTouching(groundFilter);

        ApplySpriteColoringUpdates(IsGrounded);
    }

    void ProcessHorizontalMove()
    {
        float xVel = input.horizontal * speed;

        //If on the ground, use Slide
        if (IsGrounded)
        {
            var slideVelocity = new Vector2(xVel, rb.linearVelocity.y);
            rb.Slide(slideVelocity, Time.fixedDeltaTime, slideMovement);
        }
        else
        {
            rb.linearVelocityX = xVel;
        }

        if (input.horizontal * Direction < 0f)
            FlipDirection();

        Animator.SetFloat("Speed", Mathf.Abs(xVel));
    }

    void ProcessJump()
    {
        if (input.jumpPressed && (IsGrounded || hasDoubleJump))
        {
            rb.linearVelocityY = 0f;
            rb.AddForceY(jumpForce, ForceMode2D.Impulse);

            if (IsGrounded)
            {
                slideMovement.selectedCollider.enabled = false;

                CancelInvoke("EnableCollider");
                Invoke("EnableCollider", jumpColliderDisableTime);

                IsGrounded = false;

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

    void EnableCollider()
    {
        slideMovement.selectedCollider.enabled = true;
    }
    #endregion

    #region SPRITE_VISUAL_UPDATES
    void FlipDirection()
    {
        Direction *= -1;

        if (Direction > 0)
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

    void ApplySpriteColoringUpdates(bool isGrounded)
    {
        if (IsGrounded)
        {
            hasDoubleJump = true;
            sprite.color = Color.green;
        }
        else
        {
            sprite.color = Color.red;
        }
    }
    #endregion

    #region JUMP_VFX
    void CreateFirstJumpFX()
    {
        // Added after the timer
        var fx = Instantiate(
            jumpDustFX,
            footFXPosition.position,
            Quaternion.identity);

        Destroy(fx, .5f);
    }

    //[Rpc(SendTo.NotMe, //send RPC to everyone except me
    //    Delivery = RpcDelivery.Unreliable)] 
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

    //[Rpc(SendTo.NotMe, Delivery = RpcDelivery.Unreliable)]
    void EverybodyElseCreateSecondJumpFX_Rpc()
    {
        CreateSecondJumpFX();
    }
    #endregion
}
