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
	float outerFalloffFactor;

	[SerializeField, Min(0f)]
	float innerFalloffRadius = 1f;
	[SerializeField, Min(0f)]
	float innerRadius = 5f;
	float innerFalloffFactor;

	public override Vector3 GetGravity(Vector3 position)
	{
		Vector3 vector = transform.position - position;
		float distance = vector.magnitude;
		if(distance > outerFalloffRadius || distance < innerFalloffRadius)
		{
			return Vector3.zero;
		}
		float g = gravity / distance;
		if(distance > outerRadius)
		{
			g *= 1f - (distance - outerRadius) * outerFalloffFactor;
		}
		else if(distance < innerRadius)
		{
			g *= -(1f - (distance - innerRadius) * innerFalloffFactor);
		}
		return g * vector;
	}

	void OnDrawGizmos()
	{
		Vector3 p = transform.position;
		if((innerFalloffRadius > 0f) && (innerFalloffRadius < innerRadius))
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(p, innerFalloffRadius);
		}
		Gizmos.color = Color.yellow;
		if(innerRadius > 0f && innerRadius < outerRadius)
		{
			Gizmos.DrawWireSphere(p, innerRadius);
		}
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
		innerRadius = Mathf.Max(innerRadius, innerFalloffRadius);
		innerFalloffRadius = Mathf.Max(innerFalloffRadius, 0f);
		outerRadius = Mathf.Max(outerRadius, innerRadius);
		outerFalloffRadius = Mathf.Max(outerFalloffRadius, outerRadius);

		innerFalloffFactor = 1f / (innerFalloffRadius - innerRadius);
		outerFalloffFactor = 1f / (outerFalloffRadius - outerRadius);
	}

}
