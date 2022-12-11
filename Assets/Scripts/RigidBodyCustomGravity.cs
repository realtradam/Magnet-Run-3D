using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidBodyCustomGravity : MonoBehaviour
{
	Rigidbody body;
	Renderer renderer;
	float floatDelay = 0f;

	void Awake()
	{
		body = GetComponent<Rigidbody>();
		renderer = GetComponent<Renderer>();
		body.useGravity = false;
	}

	void FixedUpdate()
	{
		if(body.IsSleeping())
		{
			renderer.material.SetColor(
					"_BaseColor", 
					Color.gray
					);
			floatDelay = 0f;
			return;
		}

		if(body.velocity.sqrMagnitude < 0.0005f)
		{
			// disable interpolation when ready to sleep
			body.interpolation = RigidbodyInterpolation.None;
			renderer.material.SetColor(
					"_BaseColor", 
					Color.yellow
					);
			floatDelay += Time.deltaTime;
			if(floatDelay >= 1f)
				return;
		}
		else
			floatDelay = 0f;

		// enable interpolation when not sleeping
		body.interpolation = RigidbodyInterpolation.Interpolate;

		renderer.material.SetColor(
				"_BaseColor", 
				Color.red
				);

		body.AddForce(
				CustomGravity.GetGravity(body.position),
				ForceMode.Acceleration
				);
	}
}
