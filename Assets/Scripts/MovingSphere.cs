using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	Rigidbody body;
	Renderer renderer;

	[SerializeField, Range(0f, 100f)]
	float maxSpeed = 10f;
	[SerializeField, Range(0f, 100f)]
	float maxAcceleration = 10f;
	[SerializeField, Range(0f, 100f)]
	float maxAirAcceleration = 1f;
	Vector3 velocity;

	[SerializeField, Range(0f, 10f)]
	float jumpHeight = 2f;
	[SerializeField, Range(0, 5)]
	int maxAirJumps;
	bool desiredJump;
	int jumpPhase;
	int groundContactCount;

	bool OnGround => groundContactCount > 0;

	Vector3 contactNormal;

	// 1.0 means walls
	// 0.0 means floors
	[SerializeField, Range(0f, 1f)]
	float maxJumpAngle = 0.44f; 

	Vector3 inputVelocity;

	void Awake()
	{
		body = GetComponent<Rigidbody>();
		renderer = GetComponent<Renderer>();
	}

	void Update()
	{
		Vector2 playerInput = new Vector2(
				Input.GetAxis("Horizontal"),
				Input.GetAxis("Vertical")
				);
		playerInput = Vector2.ClampMagnitude(playerInput, 1f);

		desiredJump |= Input.GetButtonDown("Jump");

		inputVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

		Color purple = Color.red + Color.blue;
		renderer.material.SetColor(
				"_BaseColor", purple - (purple * (groundContactCount * 0.33f))
				);
	}

	void FixedUpdate()
	{
		UpdateState();
		AdjustVelocity();

		if(desiredJump)
		{
			desiredJump = false;
			Jump();
		}

		body.velocity = velocity;
		ClearState();
	}

	void ClearState() {
		groundContactCount = 0;
		contactNormal = Vector3.zero;
	}


	void UpdateState()
	{
		velocity = body.velocity;
		if(OnGround)
		{
			jumpPhase = 0;
			if(groundContactCount > 1)
				contactNormal.Normalize();
		}
		else
			contactNormal = Vector3.up;
	}

	void Jump()
	{
		if(OnGround || jumpPhase < maxAirJumps)
		{
			jumpPhase += 1;
			float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
			float alignedSpeed = Vector3.Dot(velocity, contactNormal);
			if(alignedSpeed > 0f)
				jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
			velocity += contactNormal * jumpSpeed;
		}
	}

	void OnCollisionStay(Collision collision) {
		EvaluateCollision(collision);  
	}

	void OnCollisionEnter(Collision collision) {
		EvaluateCollision(collision);  
	}

	void EvaluateCollision(Collision collision) {
		for(int i = 0; i < collision.contactCount; i++)
		{
			Vector3 normal = collision.GetContact(i).normal;
			if(normal.y >= (1f - maxJumpAngle))
			{
				groundContactCount += 1;
				contactNormal += normal;
			}
		}
	}

	void AdjustVelocity()
	{
		Vector3 xAxis = ProjectOnContactPlane(Vector3.right).normalized;
		Vector3 zAxis = ProjectOnContactPlane(Vector3.forward).normalized;

		Vector3 currentVel = new Vector3(
				Vector3.Dot(velocity, xAxis),
				0f,
				Vector3.Dot(velocity, zAxis)
				);

		float maxSpeedChange = Time.deltaTime;
		if(OnGround)
			maxSpeedChange *= maxAcceleration;
		else
			maxSpeedChange *= maxAirAcceleration;

		Vector3 newVel = new Vector3(
				Mathf.MoveTowards(currentVel.x, inputVelocity.x, maxSpeedChange),
				0f,
				Mathf.MoveTowards(currentVel.z, inputVelocity.z, maxSpeedChange)
				);


		velocity += xAxis * (newVel.x - currentVel.x) + zAxis * (newVel.z - currentVel.z);
	}

	Vector3 ProjectOnContactPlane(Vector3 vector)
	{
		return vector - contactNormal * Vector3.Dot(vector, contactNormal);
	}
}
