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

	// dont snap to surface if going too fast
	[SerializeField, Range(0f, 100f)]
	float maxSnapSpeed = 100f;

	[SerializeField, Min(0f)]
	float raycastProbeDistance = 1f;
	[SerializeField]
	LayerMask probeMask = -1;
	LayerMask stairsMask = -1;


	int stepsSinceLastGrounded;
	int stepsSinceLastJump;

	Vector3 contactNormal;
	Vector3 steepNormal;

	int groundContactCount;
	int steepContactCount;

	bool OnGround => groundContactCount > 0;
	bool OnSteep => steepContactCount > 0;


	// 1.0 means walls
	// 0.0 means floors
	[SerializeField, Range(0f, 1f)]
	float maxSlopeAngle = 0.44f;
	float minGroundDotProduct;

	[SerializeField, Range(0f, 1f)]
	float maxStairAngle = 0.6f;
	float minStairsDotProduct;

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
		//renderer.material.SetColor(
		//		"_BaseColor", purple - (purple * (groundContactCount * 0.33f))
		//		);
		renderer.material.SetColor(
				"_BaseColor", OnGround ? purple * 0.9f : purple * 0.1f
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
		groundContactCount = steepContactCount = 0;
		contactNormal = steepNormal = Vector3.zero;
	}


	void UpdateState()
	{
		// if changed in editor, update these
		minGroundDotProduct = Mathf.Cos(maxSlopeAngle * 90 * Mathf.Deg2Rad);
		minStairsDotProduct = Mathf.Cos(maxStairAngle * 90 * Mathf.Deg2Rad);

		stepsSinceLastGrounded += 1;
		stepsSinceLastJump += 1;
		velocity = body.velocity;
		if(OnGround || SnapToGround() || CheckSteepContacts())
		{
			stepsSinceLastGrounded = 0;

			if(stepsSinceLastJump > 1)
				jumpPhase = 0;

			if(groundContactCount > 1)
				contactNormal.Normalize();
		}
		else
			contactNormal = Vector3.up;
	}

	void Jump()
	{
		Vector3 jumpDirection;

		if(OnGround)
			jumpDirection = contactNormal;
		else if(OnSteep)
		{
			jumpDirection = steepNormal;
			jumpPhase = 0;
		}
		else if((maxAirJumps > 0) && (jumpPhase <= maxAirJumps))
		{
			if(jumpPhase == 0)
				jumpPhase = 1;
			jumpDirection = contactNormal;
		}
		else
			return;

		stepsSinceLastJump = 0;
		jumpPhase += 1;
		float jumpSpeed = Mathf.Sqrt(-2f * Physics.gravity.y * jumpHeight);
		jumpDirection = (jumpDirection + Vector3.up).normalized;
		float alignedSpeed = Vector3.Dot(velocity, jumpDirection);
		if(alignedSpeed > 0f)
			jumpSpeed = Mathf.Max(jumpSpeed - alignedSpeed, 0f);
		velocity += jumpDirection * jumpSpeed;
	}

	bool SnapToGround()
	{
		if(stepsSinceLastGrounded > 1 || stepsSinceLastJump <= 2)
			return false;
		float speed = velocity.magnitude;
		if(speed > maxSnapSpeed)
			return false;
		if(!Physics.Raycast(
					body.position,
					Vector3.down,
					out RaycastHit hit,
					raycastProbeDistance,
					probeMask
					))
			return false;
		if(hit.normal.y < GetMinDot(hit.collider.gameObject.layer))
			return false;

		groundContactCount = 1;
		contactNormal = hit.normal;
		float dot = Vector3.Dot(velocity, hit.normal);
		if(dot > 0f)
			velocity = (velocity - hit.normal * dot).normalized * speed;
		return true;
	}

	void OnCollisionStay(Collision collision) {
		EvaluateCollision(collision);  
	}

	void OnCollisionEnter(Collision collision) {
		EvaluateCollision(collision);  
	}

	void EvaluateCollision(Collision collision) {
		float minDot = GetMinDot(collision.gameObject.layer);
		for(int i = 0; i < collision.contactCount; i++)
		{
			Vector3 normal = collision.GetContact(i).normal;
			if(normal.y >= minDot)
			{
				groundContactCount += 1;
				contactNormal += normal;
			}
			else if(normal.y > -0.01f)
			{
				steepContactCount += 1;
				steepNormal += normal;
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

	float GetMinDot(int layer)
	{
		if ((stairsMask & (1 << layer)) > 0)
			return minStairsDotProduct;
		else
			return minGroundDotProduct;
	}

	bool CheckSteepContacts()
	{
		if(steepContactCount > 1)
		{
			steepNormal.Normalize();
			if(steepNormal.y >= minGroundDotProduct)
			{
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}
}
