using UnityEngine;
using Unity.Netcode;

public class Controller_10min_Networked : NetworkBehaviour
{
    public float speed = 5f;
    public float jumpForce = 5f;

    Rigidbody2D rb;


    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }


	void Update()
	{
        if (!IsOwner)
            return;

        if (Input.GetButtonDown("Jump"))
        {
            rb.AddForceY(jumpForce, ForceMode2D.Impulse);
        }
    }

	void FixedUpdate()
    {
        if (!IsOwner)
            return;

        var velocity = speed * Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(velocity, rb.linearVelocityY);
    }
}
