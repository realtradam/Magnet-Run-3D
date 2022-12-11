using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GravitySphere : GravitySource
{
	[SerializeField]
	float gravity = 0.981f;

	[SerializeField, Min(0f)]
	float outerRadius = 10f;
	[SerializeField, Min(0f)]
	float outerFalloffRadius = 15f;

	public override Vector3 GetGravity(Vector3 position)
	{
		Vector3 vector = transform.position - position;
		float distance = vector.magnitude;
		if(distance > outerFalloffRadius)
		{
			return Vector3.zero;
		}
		float g = gravity / distance;
		return g * vector;
	}

	void OnDrawGizmos()
	{
		Vector3 p = transform.position;
		Gizmos.color = Color.yellow;
		Gizmos.DrawWireSphere(p, outerRadius);
		if(outerFalloffRadius > outerRadius)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(p, outerFalloffRadius);
		}
	}

	void Awake()
	{
		OnValidate();
	}

	void OnValidate()
	{
		outerFalloffRadius = Mathf.Max(outerFalloffRadius, outerRadius);
	}

}
