using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Controller_1day_Networked : NetworkBehaviour
{
    public bool drawDebugRaycasts = true;

    [Header("Run")]
    public float speed = 5f;
    public float sprintSpeed = 8f;
    float dashingPower = 24f;
    float dashingTime = .2f;
    float dashingCooldown = 1f;

    [Header("Jump")]
    public float jumpForce = 5f;
    public float doubleJumpForce = 10f;
    public float jumpHoldForce = 2f;
    public float jumpHoldDuration = .1f;
    public float coyoteDuration = .1f;
    public float maxFallSpeed = -20f;
    public float jumpColliderDisableTime = 0.1f;

    [Header("Wall Hang")]
    public float hangingJumpForce = 17f;
    public float eyeHeight = 1.5f;
    public float reachOffset = .7f;
    public float grabDistance = .4f;
    public float playerHeight = 1.8f;
    public float edgeGrabOffset = .4f;

    [Header("Physics")]
    public Collider2D groundedCollider;
    public ContactFilter2D groundFilter;
    public Rigidbody2D.SlideMovement slideMovement = new();   

    
    Rigidbody2D rb;
    PlayerInput input;

    int direction = 1;
    float jumpTime;
    float coyoteTime;

    public bool isGrounded;
    public bool isFirstJumping;
    public bool hasDoubleJump;
    public bool justBeganJump;
    public bool hasJustWallJumped;
    public bool canDash = true;
    public bool isDashing;
    public bool isHanging;
    

    //Cosmetics
    OwnerNetworkAnimator ownerNetworkAnimator;
    SpriteRenderer sprite;

    public Transform footFXPosition;
    public GameObject doubleJumpFX;
    public GameObject jumpDustFX;
    public GameObject dashDustFX;
    public TrailRenderer trail;


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

        PhysicsCheck();

        ProcessHorizontalMove();
        ProcessJump();

        // VFX
        SetColor();
        ownerNetworkAnimator.Animator.SetFloat("vSpeed", rb.linearVelocityY);
        ownerNetworkAnimator.Animator.SetBool("Grounded", isGrounded);
    }

    void PhysicsCheck()
    {
        if(!justBeganJump)
            isGrounded = groundedCollider.IsTouching(groundFilter);

        if (isGrounded)
        {
            hasDoubleJump = true;
            isFirstJumping = false;
            coyoteTime = Time.time + coyoteDuration;
            rb.linearVelocityY = 0f;
        }

        //Determine the direction of the wall grab attempt
        Vector2 grabDir = new Vector2(direction, 0f);

        //Cast three rays to look for a wall grab
        RaycastHit2D blockedCheck = RaycastHelper(new Vector2(edgeGrabOffset * direction, playerHeight), grabDir, grabDistance);
        RaycastHit2D ledgeCheck = RaycastHelper(new Vector2(reachOffset * direction, playerHeight), Vector2.down, grabDistance);
        RaycastHit2D wallCheck = RaycastHelper(new Vector2(edgeGrabOffset * direction, eyeHeight), grabDir, grabDistance);


        //If the player is off the ground AND is not hanging AND is falling AND
        //found a ledge AND found a wall AND the grab is NOT blocked...
        if (!isGrounded && !isHanging && rb.linearVelocityY < 0f &&
            ledgeCheck && wallCheck && !blockedCheck)
        {
            //...we have a ledge grab. Record the current position...
            Vector3 pos = transform.position;
            //...move the distance to the wall (minus a small amount)...
            pos.x += (wallCheck.distance - .0f) * direction;
            //...move the player down to grab onto the ledge...
            pos.y -= ledgeCheck.distance;
            //...apply this position to the platform...
            transform.position = pos;
            //...set the rigidbody to static...
            rb.bodyType = RigidbodyType2D.Static;
            //...finally, set isHanging to true
            isHanging = true;
            hasDoubleJump = true;

            ownerNetworkAnimator.Animator.SetBool("isHanging", true);
        }
    }

    void ProcessHorizontalMove()
    {
        if (input.dashPressed && canDash)
        {
            StartCoroutine(Dash());
            return;
        }

        if (isDashing || isHanging || hasJustWallJumped)
            return;

        float xVel;
        
        if(input.dashHeld)
            xVel = input.horizontal * sprintSpeed;
        else
            xVel = input.horizontal * speed;


        //If on the ground, use Slide
        if (isGrounded)
        {
            var slideVelocity = new Vector2(xVel, rb.linearVelocity.y);
            rb.Slide(slideVelocity, Time.fixedDeltaTime, slideMovement);

            print(slideVelocity.x);

            //rb.linearVelocityX = 0f;
        }
        else
        {
            rb.linearVelocityX = xVel;
        }

        if (input.horizontal * direction < 0f)
            FlipDirection();            

        ownerNetworkAnimator.Animator.SetFloat("Speed", Mathf.Abs(xVel));
    }

    void EnableMove()
    {
        hasJustWallJumped = false;
    }

    void ProcessJump()
    {
        if (isDashing)
            return;

        if (isHanging)
        {
            if (input.crouchPressed)
            {
                isHanging = false;
                ownerNetworkAnimator.Animator.SetBool("isHanging", false);

                rb.bodyType = RigidbodyType2D.Dynamic;
                return;
            }
            if (input.jumpPressed)
            {
                isHanging = false;
                ownerNetworkAnimator.Animator.SetBool("isHanging", false);

                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.AddForceY(hangingJumpForce, ForceMode2D.Impulse);
                
                rb.AddForceX(10f, ForceMode2D.Impulse);
                hasJustWallJumped = true;
                Invoke("EnableMove", .1f);
                return;
            }
        }

        // Initial Jump
        if (input.jumpPressed && !isFirstJumping && (isGrounded || coyoteTime > Time.time))
        {
            isGrounded = false;
            isFirstJumping = true;

            jumpTime = Time.time + jumpHoldDuration;

            rb.AddForceY(jumpForce, ForceMode2D.Impulse);

            //Escape ground collision
            justBeganJump = true;           
            CancelInvoke("EnableCollider");
            Invoke("EnableCollider", jumpColliderDisableTime);

            //VFX
            var fx = Instantiate(jumpDustFX, footFXPosition.position, Quaternion.identity);
            Destroy(fx, .5f);
        }
        // Holding jump button
        else if (isFirstJumping && input.jumpHeld)
        {
            rb.AddForceY(jumpHoldForce, ForceMode2D.Impulse);

            if (jumpTime <= Time.time)
                isFirstJumping = false;
        }
        // Let go of jump button while on initial jump
        else if (isFirstJumping && !input.jumpHeld)
        {
            isFirstJumping = false;
        }
        // Double jump
        else if (input.jumpPressed && hasDoubleJump)
        {
            rb.linearVelocityY = 0f;
            rb.AddForceY(doubleJumpForce, ForceMode2D.Impulse);

            hasDoubleJump = false;

            // VFX
            var fx = Instantiate(doubleJumpFX, footFXPosition.position, Quaternion.identity);
            Destroy(fx, .5f);
        }

        if (rb.linearVelocityY < maxFallSpeed)
            rb.linearVelocityY = maxFallSpeed;
    }

    void EnableCollider()
    {
        justBeganJump = false;
    }

    IEnumerator Dash()
    {
        canDash = false;
        isDashing = true;
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(direction * dashingPower, 0f);

        //VFX
        trail.emitting = true;
        ownerNetworkAnimator.SetTrigger("Dash");
        ownerNetworkAnimator.Animator.SetBool("isDashing", true);
        var dust = Instantiate(dashDustFX, footFXPosition.position, Quaternion.identity);
        dust.transform.localScale = new Vector3(direction, 1, 1);
        Destroy(dust, .5f);

        yield return new WaitForSeconds(dashingTime);

        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = originalGravity;
        isDashing = false;

        // VFX
        trail.emitting = false;
        ownerNetworkAnimator.Animator.SetBool("isDashing", false);

        yield return new WaitForSeconds(dashingCooldown);
        canDash = true;
    }

    void SetColor()
    {
        if (isHanging)
            sprite.color = Color.cyan;
        else if (isDashing)
            sprite.color = Color.green;
        else if (!isGrounded)
            sprite.color = Color.red;
        else
            sprite.color = Color.white;
    }

    void FlipDirection()
    {
        direction *= -1;

        if (direction > 0)
            sprite.flipX = false;
        else
            sprite.flipX = true;
    }

    //These two Raycast methods wrap the Physics2D.Raycast() and provide some extra
    //functionality
    RaycastHit2D RaycastHelper(Vector2 offset, Vector2 rayDirection, float length)
    {
        //Call the overloaded Raycast() method using the ground layermask and return 
        //the results
        return RaycastHelper(offset, rayDirection, length, slideMovement.layerMask);
    }

    RaycastHit2D RaycastHelper(Vector2 offset, Vector2 rayDirection, float length, LayerMask mask)
    {
        //Record the player's position
        Vector2 pos = transform.position;

        //Send out the desired raycasr and record the result
        RaycastHit2D hit = Physics2D.Raycast(pos + offset, rayDirection, length, mask);

        //If we want to show debug raycasts in the scene...
        if (drawDebugRaycasts)
        {
            //...determine the color based on if the raycast hit...
            Color color = hit ? Color.red : Color.green;
            //...and draw the ray in the scene view
            Debug.DrawRay(pos + offset, rayDirection * length, color);
        }

        //Return the results of the raycast
        return hit;
    }
}
