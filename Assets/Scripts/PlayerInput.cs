﻿// This script handles inputs for the player. It serves two main purposes: 1) wrap up
// inputs so swapping between mobile and standalone is simpler and 2) keeping inputs
// from Update() in sync with FixedUpdate()

using UnityEngine;

//We first ensure this script runs before all other player scripts to prevent laggy
//inputs
[DefaultExecutionOrder(-100)]
public class PlayerInput : MonoBehaviour
{
	[HideInInspector] public float horizontal;		//Float that stores horizontal input
	[HideInInspector] public bool jumpHeld;			//Bool that stores jump pressed
	[HideInInspector] public bool jumpPressed;		//Bool that stores jump held
	[HideInInspector] public bool crouchHeld;		//Bool that stores crouch pressed
	[HideInInspector] public bool crouchPressed;    //Bool that stores crouch held
	[HideInInspector] public bool dashPressed;      //Bool that stores dash pressed
	[HideInInspector] public bool dashHeld;			//Bool that stores dash held

	bool readyToClear;								//Bool used to keep input in sync


	void Update()
	{
		//Clear out existing input values
		ClearInput();

		//Process keyboard, mouse, gamepad (etc) inputs
		ProcessInputs();

		//Clamp the horizontal input to be between -1 and 1
		horizontal = Mathf.Clamp(horizontal, -1f, 1f);
	}

	void FixedUpdate()
	{
		//In FixedUpdate() we set a flag that lets inputs to be cleared out during the 
		//next Update(). This ensures that all code gets to use the current inputs
		readyToClear = true;
	}

	void ClearInput()
	{
		//If we're not ready to clear input, exit
		if (!readyToClear)
			return;

		//Reset all inputs
		horizontal		= 0f;
		jumpPressed		= false;
		jumpHeld		= false;
		crouchPressed	= false;
		crouchHeld		= false;
		dashPressed		= false;
		dashHeld		= false;

		readyToClear	= false;
	}

	void ProcessInputs()
	{
		//Accumulate horizontal axis input
		horizontal		+= Input.GetAxisRaw("Horizontal");

		//Accumulate button inputs
		jumpPressed		= jumpPressed || Input.GetButtonDown("Jump");
		jumpHeld		= jumpHeld || Input.GetButton("Jump");

		crouchPressed	= crouchPressed || Input.GetKeyDown(KeyCode.S);
		crouchHeld		= crouchHeld || Input.GetKey(KeyCode.S);

		dashPressed		= dashPressed || Input.GetKeyDown(KeyCode.LeftShift);
		dashHeld		= dashHeld || Input.GetKey(KeyCode.LeftShift);
	}
}
