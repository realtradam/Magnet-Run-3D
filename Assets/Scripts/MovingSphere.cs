using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	// if we want input relative to a
	// camera(or some other arbitrary object) we
	// put that object here
	[SerializeField]
	Transform playerInputSpace = default;
	[SerializeField]
	Transform ball = default;

	// "self" objects
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
	[SerializeField]
	LayerMask stairsMask = -1;


	int stepsSinceLastGrounded;
	int stepsSinceLastJump;

	Vector3 contactNormal;
	Vector3 steepNormal;

	int groundContactCount;
	int steepContactCount;

	bool OnGround => groundContactCount > 0;
	bool OnSteep => steepContactCount > 0;

	Vector3 upAxis;
	Vector3 rightAxis;
	Vector3 forwardAxis;

	// 1.0 means walls
	// 0.0 means floors
	[SerializeField, Range(0f, 1f)]
	float maxSlopeAngle = 0.44f;
	float minGroundDotProduct;

	[SerializeField, Range(0f, 1f)]
	float maxStairAngle = 0.6f;
	float minStairsDotProduct;

	[SerializeField, Min(0f)]
	float ballAlignSpeed = 180f;

	[SerializeField, Min(0.1f)]
	float ballRadius = 0.5f;

	Vector3 lastContactNormal;

	Vector3 inputVelocity;

	void Awake()
	{
		body = GetComponent<Rigidbody>();
		renderer = ball.GetComponent<Renderer>();
		body.useGravity = false;
	}

	void Update()
	{
		Vector2 playerInput = new Vector2(
				Input.GetAxis("Horizontal"),
				Input.GetAxis("Vertical")
				);
		playerInput = Vector2.ClampMagnitude(playerInput, 1f);

		desiredJump |= Input.GetButtonDown("Jump");

		if(playerInputSpace)
		{
			rightAxis = ProjectDirectionOnPlane(playerInputSpace.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(playerInputSpace.forward, upAxis);
		}
		else
		{
			rightAxis = ProjectDirectionOnPlane(Vector3.right, upAxis);
			forwardAxis = ProjectDirectionOnPlane(Vector3.forward, upAxis);
		}
		inputVelocity = new Vector3(playerInput.x, 0f, playerInput.y) * maxSpeed;

		Color purple = Color.red + Color.blue;
		//renderer.material.SetColor(
		//		"_BaseColor", purple - (purple * (groundContactCount * 0.33f))
		//		);
		//renderer.material.SetColor(
		//		"_BaseColor", OnGround ? purple * 0.9f : purple * 0.1f
		//		);
		UpdateBall();
	}


	void FixedUpdate()
	{
		Vector3 gravity = CustomGravity.GetGravity(body.position, out upAxis);
		UpdateState();
		AdjustVelocity();

		if(desiredJump)
		{
			desiredJump = false;
			Jump(gravity);
		}

		velocity += gravity * Time.deltaTime;

		body.velocity = velocity;
		ClearState();
	}

	void ClearState() {
		lastContactNormal = contactNormal;
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
			contactNormal = upAxis;
	}

	void UpdateBall()
	{
		Vector3 movement = body.velocity * Time.deltaTime;
		float distance = movement.magnitude;
		if(distance < 0.001f)
			return;
		float angle = distance * (180f / Mathf.PI) / ballRadius;
		Vector3 rotationAxis =
			Vector3.Cross(lastContactNormal, movement).normalized;
		Quaternion rotation =
			Quaternion.Euler(rotationAxis * angle) * ball.localRotation;
		if(ballAlignSpeed > 0f)
			rotation = AlignBallRotation(rotationAxis, rotation, distance);
		ball.localRotation = rotation;
	}

	Quaternion AlignBallRotation(Vector3 rotationAxis, Quaternion rotation, float traveledDistance)
	{
		Vector3 ballAxis = ball.up;
		float dot = Mathf.Clamp(Vector3.Dot(ballAxis, rotationAxis), -1f, 1f);
		float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;
		float maxAngle = ballAlignSpeed * traveledDistance;

		Quaternion newAlignment =
			Quaternion.FromToRotation(ballAxis, rotationAxis) * rotation;
		if(angle <= maxAngle)
			return newAlignment;
		else
			return Quaternion.SlerpUnclamped(
					rotation,
					newAlignment,
					maxAngle / angle
					);
	}

	void Jump(Vector3 gravity)
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
		float jumpSpeed = Mathf.Sqrt(2f * gravity.magnitude * jumpHeight);
		jumpDirection = (jumpDirection + upAxis).normalized;
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
					-upAxis,
					out RaycastHit hit,
					raycastProbeDistance,
					probeMask
					))
			return false;
		float upDot = Vector3.Dot(upAxis, hit.normal);
		if(upDot < GetMinDot(hit.collider.gameObject.layer))
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
			float upDot = GetMinDot(collision.gameObject.layer);
			if(upDot >= minDot)
			{
				groundContactCount += 1;
				contactNormal += normal;
			}
			else if(upDot > -0.01f)
			{
				steepContactCount += 1;
				steepNormal += normal;
			}
		}
	}

	void AdjustVelocity()
	{
		Vector3 xAxis = ProjectDirectionOnPlane(rightAxis, contactNormal);
		Vector3 zAxis = ProjectDirectionOnPlane(forwardAxis, contactNormal);

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

	Vector3 ProjectDirectionOnPlane(Vector3 direction, Vector3 normal)
	{
		return (direction - normal * Vector3.Dot(direction, normal)).normalized;
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
			float upDot = Vector3.Dot(upAxis, steepNormal);
			if(upDot >= minGroundDotProduct)
			{
				groundContactCount = 1;
				contactNormal = steepNormal;
				return true;
			}
		}
		return false;
	}
}
