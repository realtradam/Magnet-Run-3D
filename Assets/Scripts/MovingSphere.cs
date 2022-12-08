using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	Rigidbody body;

	[SerializeField, Range(1f, 100f)]
	float maxSpeed = 10f;
	[SerializeField, Range(1f, 100f)]
	float maxAcceleration = 10f;
	[SerializeField, Range(1f, 100f)]
	float maxAirAcceleration = 1f;
	Vector3 velocity;

	[SerializeField, Range(0f, 10f)]
	float jumpHeight = 2f;
	[SerializeField, Range(0, 5)]
	int maxAirJumps;
	bool desiredJump;
	int jumpPhase;
	bool onGround;

	// 0.0 means walls
	// 1.0 means floors
	float maxJumpAngle = 0.1f; 

	Vector3 inputVelocity;

	void Awake()
	{
		body = GetComponent<Rigidbody>();
	}

	void Update()
	{
		Vector2 playerInput = new Vector2(
				Input.GetAxis("Horizontal"),
				Input.GetAxis("Vertical")
				);
		playerInput = Vector2.ClampMagnitude(playerInput, 1f);

		desiredJump |= Input.GetButtonDown("Jump");

		inputVelocity = new Vector3(playerInput.x, 0f, playerInput.y);

	}

	void FixedUpdate()
	{
		UpdateState();
		Vector3 acceleration = new Vector3(
				inputVelocity.x,
				0f,
				inputVelocity.z
				);

		if(onGround)
			acceleration *= maxAcceleration;
		else
			acceleration *= maxAirAcceleration;

		velocity += acceleration * Time.deltaTime;
		velocity = Vector3.ClampMagnitude(velocity, maxSpeed);

		if(desiredJump)
		{
			desiredJump = false;
			Jump();
		}

		body.velocity = velocity;

		onGround = false;
	}

	void UpdateState()
	{
		velocity = body.velocity;
		if(onGround)
			jumpPhase = 0;
	}

	void Jump()
	{
		if(onGround || jumpPhase < maxAirJumps)
		{
			jumpPhase += 1;
			float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
			if(velocity.y > 0f)
				jumpSpeed = Mathf.Max(jumpSpeed - velocity.y, 0f);
			velocity.y += jumpSpeed;
		}
	}

	void OnCollisionStay(Collision collision) {
		EvaluateCollision(collision);  
	}

	void OnCollisionEnter(Collision collision) {
		EvaluateCollision(collision);  
	}

	void EvaluateCollision(Collision collision) {
		for (int i = 0; i < collision.contactCount; i++) {
			Vector3 normal = collision.GetContact(i).normal;
			onGround |= normal.y >= maxJumpAngle;
		}
	}
}
