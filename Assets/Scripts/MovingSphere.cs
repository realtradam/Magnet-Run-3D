using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingSphere : MonoBehaviour
{
	[SerializeField, Range(1f, 100f)]
	float maxSpeed = 10f;
	[SerializeField, Range(1f, 100f)]
	float maxAcceleration = 10f;

	[SerializeField, Range(0f, 1f)]
	float bounciness = 0.5f;

	Rect allowedArea = new Rect(-4.5f, -4.5f, 9f, 9f);

	Vector3 velocity;

	// Start is called before the first frame update
	void Start()
	{

	}

	// Update is called once per frame
	void Update()
	{
		Vector2 playerInput = new Vector2(
				Input.GetAxis("Horizontal"),
				Input.GetAxis("Vertical")
				);
		playerInput = Vector2.ClampMagnitude(playerInput, 1f);

		Vector3 acceleration = new Vector3(
					playerInput.x,
					0f,
					playerInput.y
					) * maxAcceleration;
		velocity += acceleration * Time.deltaTime;
		velocity = Vector3.ClampMagnitude(velocity, maxSpeed);
		Vector3 displacement = velocity * Time.deltaTime;

		Vector3 newPosition = transform.localPosition + displacement;

		if(newPosition.x < allowedArea.xMin)
		{
			newPosition.x = allowedArea.xMin;
			velocity.x = -velocity.x * bounciness;
		}
		else if(newPosition.x > allowedArea.xMax)
		{
			newPosition.x = allowedArea.xMax;
			velocity.x = -velocity.x * bounciness;
		}
		if(newPosition.z < allowedArea.yMin)
		{
			newPosition.z = allowedArea.yMin;
			velocity.z = -velocity.z * bounciness;
		}
		else if(newPosition.z > allowedArea.yMax)
		{
			newPosition.z = allowedArea.yMax;
			velocity.z = -velocity.z * bounciness;
		}

		transform.localPosition = newPosition;
	}
}
