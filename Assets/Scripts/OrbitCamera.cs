using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitCamera : MonoBehaviour
{
	Camera regularCamera;

	[SerializeField]
	Transform focus = default;

	[SerializeField, Range(1f, 20f)]
	float distance = 5f;

	[SerializeField, Min(0f)]
	float focusLimitRadius = 1f;

	[SerializeField, Range(0f, 1f)]
	float focusCenteringSpeed = 0.5f;

	[SerializeField, Range(0f, 1f)]
	float rotationFlippingSpeed = 0.9f;

	[SerializeField, Range(1f, 360f)]
	float rotationSpeed = 90f;

	[SerializeField, Range(-89f, 89f)]
	float minVerticalAngle = -30f;
	[SerializeField, Range(-89f, 89f)]
	float maxVerticalAngle = 60f;

	[SerializeField, Min(0f)]
	float alignDelay = 5f;

	[SerializeField, Range(0f, 90f)]
	float alignSmoothRange = 45;

	float lastManualRotationTime;

	Vector3 focusPoint;
	Vector3 previousFocusPoint;

	Vector3 orbitAngles = new Vector2(45f, 0f);

	Quaternion gravityAlignment = Quaternion.identity;
	Quaternion orbitRotation;

	[SerializeField]
	LayerMask obstructionMask = -1;

	void Awake()
	{
		regularCamera = GetComponent<Camera>();
		focusPoint = focus.position;
		transform.localRotation = orbitRotation = Quaternion.Euler(orbitAngles);
		OnValidate();
	}

	void LateUpdate()
	{
		UpdateGravityAlignment();
		UpdateFocusPoint();

		if(ManualRotation() || AutomaticRotation())
		{
			ConstrainAngles();
			orbitRotation = Quaternion.Euler(orbitAngles);
		}

		Quaternion lookRotation = gravityAlignment * orbitRotation;

		Vector3 lookDirection = lookRotation * Vector3.forward;
		Vector3 lookPosition = focusPoint - lookDirection * distance;

		// --- Perform box cast between camera and focus target ---
		//	determine if anything is blocking the camera.
		//	If it is, place camera in front of that object.
		Vector3 rectOffset = lookDirection * regularCamera.nearClipPlane;
		Vector3 rectPosition = lookPosition + rectOffset;
		Vector3 castFrom = focus.position;
		Vector3 castLine = rectPosition - castFrom;
		float castDistance = castLine.magnitude;
		Vector3 castDirection = castLine / castDistance;

		if(Physics.BoxCast(
					castFrom,			// center
					CameraHalfExtends,	// halfExtents
					castDirection,		// direction
					out RaycastHit hit,	// hitInfo
					lookRotation,		// orientation
					castDistance,		// maxDistance
					obstructionMask		// layerMask
					)
		  )
		{
			rectPosition = castFrom + castDirection * hit.distance;
			lookPosition = rectPosition - rectOffset;
		}
		// --- ---

		transform.SetPositionAndRotation(lookPosition, lookRotation);
	}

	// Orients the camera depending on the direction of gravity.
	// Interpolates the rotation so that it is smooth.
	void UpdateGravityAlignment() {
		Vector3 fromUp = gravityAlignment * Vector3.up;
		Vector3 toUp = CustomGravity.GetUpAxis(focusPoint);
		float dot = Mathf.Clamp(
				Vector3.Dot(fromUp, toUp),
				-1f,
				1f
				);
		float angle = Mathf.Acos(dot) * Mathf.Rad2Deg;


		Quaternion newAlignment =
			Quaternion.FromToRotation(fromUp, toUp) * gravityAlignment;

		if(angle > 0.1f && rotationFlippingSpeed > 0f)
		{
			float t = Mathf.Pow(
					1f - rotationFlippingSpeed,
					Time.unscaledDeltaTime
					);
			gravityAlignment = Quaternion.SlerpUnclamped(
					gravityAlignment,
					newAlignment,
					1-t
					);
		}
		else
			gravityAlignment = newAlignment;
	}

	// Applies camera movement smoothing by interpolating
	// the current focus point and the actual focus point
	void UpdateFocusPoint()
	{
		previousFocusPoint = focusPoint;
		Vector3 targetPoint = focus.position;
		if(focusLimitRadius > 0f)
		{
			float t = 1f;
			float distance = Vector3.Distance(targetPoint, focusPoint);
			if(distance > 0.01f && focusCenteringSpeed > 0f)
			{
				t = Mathf.Pow(
						1f - focusCenteringSpeed,
						Time.unscaledDeltaTime
						);
			}
			if(distance > focusLimitRadius)
			{
				t = Mathf.Min(t, focusLimitRadius / distance);
			}
			focusPoint = Vector3.Lerp(
					focusPoint,
					targetPoint,
					1-t
					);
		}
		else
			focusPoint = targetPoint;
	}

	bool ManualRotation()
	{
		Vector2 input = new Vector2(
				Input.GetAxis("Vertical Camera"),
				Input.GetAxis("Horizontal Camera")
				);
		const float e = 0.001f;
		if(
				input.x < -e ||
				input.x > e ||
				input.y < -e ||
				input.y > e
		  )
		{
			orbitAngles += rotationSpeed * Time.unscaledDeltaTime * (Vector3)input;
			lastManualRotationTime = Time.unscaledTime;
			return true;
		}
		return false;
	}

	void ConstrainAngles()
	{
		orbitAngles.x = Mathf.Clamp(
				orbitAngles.x,
				minVerticalAngle,
				maxVerticalAngle);

		if(orbitAngles.y < 0f)
			orbitAngles.y += 360f;
		else if(orbitAngles.y >= 360f)
			orbitAngles.y -= 360f;
	}

	bool AutomaticRotation()
	{
		if(Time.unscaledTime - lastManualRotationTime < alignDelay)
			return false;

		Vector3 alignedDelta = Quaternion.Inverse(gravityAlignment) *
			(focusPoint - previousFocusPoint);
		Vector2 movement = new Vector2(
				alignedDelta.x,
				alignedDelta.z
				);
		float movementDeltaSqr = movement.sqrMagnitude;
		if(movementDeltaSqr < 0.0001f)
			return false;

		float headingAngle = GetAngle(movement / Mathf.Sqrt(movementDeltaSqr));
		float deltaAbs = Mathf.Abs(Mathf.DeltaAngle(orbitAngles.y, headingAngle));
		float rotationChange = rotationSpeed * Mathf.Min(Time.unscaledDeltaTime, movementDeltaSqr);
		if(deltaAbs < alignSmoothRange)
			rotationChange *= deltaAbs / alignSmoothRange;
		else if(180f - deltaAbs < alignSmoothRange)
			rotationChange *= (180f - deltaAbs) / alignSmoothRange;
		orbitAngles.y = Mathf.MoveTowardsAngle(orbitAngles.y, headingAngle, rotationChange);

		return true;
	}

	// Box cast requires a box that extends 1/2 in all directions
	// (width, height, depth)
	Vector3 CameraHalfExtends
	{
		get
		{
			Vector3 halfExtends;
			halfExtends.y =
				regularCamera.nearClipPlane *
				Mathf.Tan(0.5f * Mathf.Deg2Rad * regularCamera.fieldOfView);
			halfExtends.x = halfExtends.y * regularCamera.aspect;
			halfExtends.z = 0f;
			return halfExtends;
		}
	}

	static float GetAngle (Vector2 direction)
	{
		float angle = Mathf.Acos(direction.y) * Mathf.Rad2Deg;
		return direction.x < 0f ? 360f - angle : angle;
	}

	// Santizes inspector configuration
	void OnValidate()
	{
		if(maxVerticalAngle < minVerticalAngle)
			maxVerticalAngle = minVerticalAngle;
	}
}
