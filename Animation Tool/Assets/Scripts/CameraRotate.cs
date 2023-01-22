
//In a file MaxCamera.cs
using UnityEngine;
using System.Collections;

public class CameraRotate : MonoBehaviour
{
	public Transform target;

	public Vector3 targetOffset;
	public float distance = 5.0f;

	public float maxDistance = 20;
	public float minDistance = .6f;

	public float xSpeed = 200.0f;
	public float ySpeed = 200.0f;

	public int yMinLimit = -80;
	public int yMaxLimit = 80;

	public int zoomRate = 40;

	public float panSpeed = 0.3f;

	public float zoomDampening = 5.0f;

	private float xDeg = 0.0f;
	private float yDeg = 0.0f;
	private float currentDistance;
	private float desiredDistance;

	void Start()
	{
		//Vector3 angles = transform.eulerAngles;
		//xDeg = angles.x;
		//yDeg = angles.y;

		currentDistance = distance;
		desiredDistance = distance;
	}

	/**
     * Camera logic on LateUpdate to only update after all character movement logic has been handled.
     */
	void LateUpdate()
	{
		// Don't do anything if target is not defined
		if (!target)
			return;

		//// If Control and Alt and Middle button? ZOOM!
		//if (Input.GetMouseButton(2) || Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.LeftControl))
  //      {
		//	desiredDistance -= Input.GetAxis("Mouse Y") * Time.deltaTime * zoomRate * 0.125f * Mathf.Abs(desiredDistance);
		//}
        // If middle mouse and left alt are selected? ORBIT
         if (Input.GetMouseButton(0)|| Input.GetMouseButton(1)|| Input.GetKey(KeyCode.LeftArrow)|| Input.GetKey(KeyCode.RightArrow)|| Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S))
        {
			xDeg += Input.GetAxis("Mouse X") * xSpeed * 0.02f;
			yDeg -= Input.GetAxis("Mouse Y") * ySpeed * 0.02f;
		}
        // otherwise if middle mouse is selected, we pan by way of transforming the target in screenspace
  //      else if (Input.GetMouseButton(2))
		//{
		//	//grab the rotation of the camera
		//	target.rotation = transform.rotation;
		//	target.Translate(Vector3.right * -Input.GetAxis("Mouse X") * panSpeed);
		//	target.Translate(transform.up * -Input.GetAxis("Mouse Y") * panSpeed, Space.World);
		//}

		// affect the desired Zoom distance if we roll the scrollwheel
		desiredDistance -= Input.GetAxis("Mouse ScrollWheel") * Time.deltaTime * zoomRate * Mathf.Abs(desiredDistance);

		//clamp the zoom min/max
		desiredDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);

		//Clamp the vertical axis for the orbit
		yDeg = ClampAngle(yDeg, yMinLimit, yMaxLimit);

		// set camera rotation
		Quaternion rotation = Quaternion.Euler(yDeg, xDeg, 0);

		// For smoothing of the zoom, lerp distance
		currentDistance = Mathf.Lerp(currentDistance, desiredDistance, Time.deltaTime * zoomDampening);

		// keep within legal limits
		currentDistance = Mathf.Clamp(currentDistance, minDistance, maxDistance);

		// calculate position based on the new currentDistance
		Vector3 position = target.position - (rotation * Vector3.forward * currentDistance + targetOffset);

		transform.rotation = rotation;
		transform.position = position;
	}

	private static float ClampAngle(float angle, float min, float max)
	{
		if (angle < -360)
			angle += 360;
		if (angle > 360)
			angle -= 360;
		return Mathf.Clamp(angle, min, max);
	}
}