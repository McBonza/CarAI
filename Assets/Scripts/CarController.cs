using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CarController : MonoBehaviour
{
    private float m_horizontalInput;
    private float m_verticalInput;

    private float m_steeringAngle;

    public Transform platform;
    public WheelCollider frontDriverW, frontPassengerW, rearDriverW, rearPassengerW;
    public Transform frontDriverT, frontPassengerT, rearDriverT, rearPassengerT;

    public float maxSteerAngle = 30.0f;

    public float motorForce = 50.0f;
    public float brakeForce = 50.0f;

    [HideInInspector] public float throttle = 0.0f;
    [HideInInspector] public float steering = 0.0f;
    [HideInInspector] public float brake = 0.0f;

    public string steeringText;
    public string throttleText;
    public string brakingText;

    public void GetInput()
    {
        m_horizontalInput = Input.GetAxis("Horizontal");
        m_verticalInput = Input.GetAxis("Vertical");
    }

    private void Steer()
    {
        m_steeringAngle = maxSteerAngle * steering;
        frontDriverW.steerAngle = m_steeringAngle;
        frontPassengerW.steerAngle = m_steeringAngle;
    }

    private void Accelerate()
    {
        frontDriverW.motorTorque = throttle * motorForce;
        frontPassengerW.motorTorque = throttle * motorForce;
        //rearDriverW.motorTorque = throttle * motorForce;
        //rearPassengerW.motorTorque = throttle * motorForce;
    }

    private void Brake()
    {
        frontDriverW.brakeTorque = brake * brakeForce;
        frontPassengerW.brakeTorque = brake * brakeForce;
        //rearDriverW.brakeTorque = brake * brakeForce;
        //rearPassengerW.brakeTorque = brake * brakeForce;
    }

    private void UpdateWheelPoses()
    {
        UpdateWheelPose(frontDriverW, frontDriverT);
        UpdateWheelPose(frontPassengerW, frontPassengerT);
        UpdateWheelPose(rearDriverW, rearDriverT);
        UpdateWheelPose(rearPassengerW, rearPassengerT);
    }

    private void UpdateWheelPose(WheelCollider collider, Transform transform)
    {
        Vector3 pos = transform.position;
        Quaternion quat = transform.rotation;

        collider.GetWorldPose(out pos, out quat);

        transform.position = pos;
        transform.rotation = quat;
    }

    private void FixedUpdate()
    {
        platform.position = new Vector3(transform.position.x, platform.position.y, transform.position.z);
        steering = Mathf.Clamp(steering, -1.0f, 1.0f);
        throttle = Mathf.Clamp(throttle, -1.0f, 1.0f);
        brake = Mathf.Clamp(brake, 0.0f, 1.0f);

        GetInput();
        Steer();
        Accelerate();
        Brake();
        UpdateWheelPoses();

        //groundTransform.position.x = transform.position.x;

        steeringText = "Steering: " + steering.ToString("0.00");


        throttleText = "Throttle: " + throttle.ToString("0.00");


        brakingText = "Brake: " + brake.ToString("0.00");

    }

    public float GetSpeed()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        return rb.velocity.magnitude * 5.0f;
    }
}
