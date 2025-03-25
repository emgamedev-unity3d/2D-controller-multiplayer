using UnityEngine;
using Unity.Netcode;

public class Controller_10min_Cosmetic_Networked : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;

    //Added after the timer
    OwnerNetworkAnimator ownerNetworkAnim;
    SpriteRenderer sprite;
    int direction = 1;

    Rigidbody2D rb;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        //Added after the timer
        ownerNetworkAnim = GetComponent<OwnerNetworkAnimator>();
        sprite = GetComponentInChildren<SpriteRenderer>(true);
    }


	void Update()
	{
        if (!IsOwner)
            return;

        if (Input.GetButtonDown("Jump"))
        {
            rb.AddForceY(jumpForce, ForceMode2D.Impulse);

            //Added after the timer
            ownerNetworkAnim.SetTrigger("Jump");
        }
    }

	  void FixedUpdate()
    {
        if (!IsOwner)
            return;

        var velocity = speed * Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(velocity, rb.linearVelocityY);

        //Added after the timer
        if (Input.GetAxis("Horizontal") * direction < 0f)
            FlipDirection();

        ownerNetworkAnim.Animator.SetFloat(
            "Speed",
            Mathf.Abs(Input.GetAxis("Horizontal")));
    }

    //Added after the timer
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
        target:SendTo.NotMe,
        Delivery = RpcDelivery.Reliable)]
    void EverybodyElseFlipDirectionRpc()
    {
        sprite.flipX = !sprite.flipX;
    }
}