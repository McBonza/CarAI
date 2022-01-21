using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FollowerCameraController : MonoBehaviour
{
    public Transform groundTransform;
    public List<GameObject> cars;
    private int targetIndex = 0;
    public List<Vector3> offsets;
    private int offsetIndex = 0;
    public float followSpeed = 10;
    public float lookSpeed = 10;

    public GameObject currentCar;
    public Text steeringText;
    public Text throttleText;
    public Text brakingText;
    public Text actionText;
    public Text speedText;
    public Text severityText;


    public void LookAtTarget()
    {
        Transform target = cars[targetIndex].transform;
        Vector3 lookDirection = target.position - transform.position;
        Quaternion rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        transform.rotation = Quaternion.Lerp(transform.rotation, rotation, lookSpeed * Time.deltaTime);
    }

    public void MoveToTarget()
    {
        Vector3 offset = offsets[offsetIndex];
        Transform target = cars[targetIndex].transform;
        Vector3 pos = target.position + target.forward * offset.z + target.right * offset.x + target.up * offset.y;
        transform.position = Vector3.Lerp(transform.position, pos, followSpeed * Time.deltaTime);
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.C))
        {
            offsetIndex++;
            if (offsetIndex >= offsets.Count)
            {
                offsetIndex = 0;
            }
        }

        if (Input.GetKeyUp(KeyCode.RightArrow))
        {
            targetIndex++;
            if (targetIndex >= cars.Count)
            {
                targetIndex = 0;
            }
            InstantUpdate();
        }
        else if (Input.GetKeyUp(KeyCode.LeftArrow))
        {
            targetIndex--;
            if (targetIndex < 0)
            {
                targetIndex = cars.Count - 1;
            }
            InstantUpdate();
        }

        if(Input.GetKeyUp(KeyCode.Q))
        {
            Application.Quit();
        }
    }

    private void InstantUpdate()
    {
        Transform target = cars[targetIndex].transform;
        Vector3 lookDirection = target.position - transform.position;
        transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);

        Vector3 offset = offsets[offsetIndex];
        Vector3 pos = target.position + target.forward * offset.z + target.right * offset.x + target.up * offset.y;
        transform.position = pos;

        currentCar = cars[targetIndex];
    }

    private void FixedUpdate()
    {
        LookAtTarget();
        MoveToTarget();
        UpdateUITexts();

        groundTransform.position = new Vector3(transform.position.x, groundTransform.position.y, transform.position.z);
    }


    //functions to affect currently followed car

    public void UpdateUITexts()
    {
        steeringText.text = currentCar.GetComponent<CarController>().steeringText;
        throttleText.text = currentCar.GetComponent<CarController>().throttleText;
        brakingText.text = currentCar.GetComponent<CarController>().brakingText;

        actionText.text = currentCar.GetComponent<AIDriver1>().actionText;
        speedText.text = currentCar.GetComponent<AIDriver1>().speedText;
        severityText.text = currentCar.GetComponent<AIDriver1>().severityText;
    }

    public void SetUseManualInput(bool val)
    {
        currentCar.GetComponent<AIDriver1>().SetUseManualInput(val);
    }

    public void SetManualThrottle(float val)
    {
        currentCar.GetComponent<AIDriver1>().SetManualThrottle(val);
    }

    public void SetManualBrake(float val)
    {
        currentCar.GetComponent<AIDriver1>().SetManualBrake(val);
    }

    public void SetRecklessness(float val)
    {
        currentCar.GetComponent<AIDriver1>().SetRecklessness(val);
    }

    public void SetPatience(float val)
    {
        currentCar.GetComponent<AIDriver1>().SetPatience(val);
    }

    public void ForceChangeLane()
    {
        currentCar.GetComponent<AIDriver1>().ForceChangeLane();
    }

    public void SetLaneChangeSpeed(float val)
    {
        currentCar.GetComponent<AIDriver1>().SetLaneChangeSpeed(val);
    }

    public void SetDesiredCruiseSpeed(float val)
    {
        currentCar.GetComponent<AIDriver1>().SetDesiredCruiseSpeed(val);
    }
}
