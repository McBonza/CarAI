using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AIDriver1 : MonoBehaviour
{
    public string actionText;
    public string speedText;
    public string severityText;
    public PathCreation.PathCreator pathCreator;
    public CarController carController;


    [Header("Master Handles")]
    [Tooltip("How patient it will be about surrounding cars.")]
    [Range(0.0f, 1.0f)] public float patience = 0.5f; 
    [Tooltip("How reckless it will be with its maneuver and decision making.")]
    [Range(0.0f, 1.0f)] public float recklessness = 0.0f; 

    [Header("Road and Navigation")]
    [Tooltip("Target velocity of the car (cruise speed).")]
    [Range(0.0f, 65.0f)] public float targetSpeed = 65.0f;
    [Tooltip("How fast it changes lanes.")]
    [Range(0.0f, 10.0f)] public float changeLaneSpeed = 2.0f;
    [Tooltip("Current lane occupied by the car.")]
    public bool useRightLane = true;
    [Tooltip("Offset of the car from the very center of the road.")]
    [Range(0.0f, 2.0f)] public float laneOffset = 0.0f;
    [Tooltip("Length of road considered to account for curves.")]
    [Range(1.0f, 10.0f)] public float roadLookAheadDistance = 6.0f;
    [Tooltip("Maximum curve severity tolerated before action is to be taken.")]
    [Range(1.0f, 50.0f)] public float maximumCurveSeverity = 20.0f;

    [Header("Neighboring Cars Checks")]
    [Tooltip("Length of road considered to account for other cars.")]
    [Range(1.0f, 10.0f)] public float carLookAheadDistance = 12.0f;
    [Tooltip("Minimum distance to closest car required to trigger a reaction.")]
    [Range(1.0f, 40.0f)] public float dangerThreshold = 5.0f; 
    [Tooltip("Minimum distance to closest car required to trigger an extreme reaction.")]
    [Range(1.0f, 40.0f)] public float emergencyThreshold = 4.0f; 
    [Tooltip("Cap for the rate at which the considered car is closing in relatively to self. (eg. a closing rate higher than 20 will be considered as 20).")]
    [Range(1.0f, 40.0f)] public float closingRateCap = 20.0f; 
    [Tooltip("Modifier to change when the closest carshould stop being considered (higher means faster exit).")]
    [Range(1.0f, 40.0f)] public float dangerExitModifier = 2.0f;

    [Header("Latteral Checks")]
    [Tooltip("Distance to check to the front of the car when considering the other lane.")]
    [Range(1.0f, 40.0f)] public float latteralDistanceCheckFront = 12.0f;
    [Tooltip("Angle between the nearest car in front of self with direction of self.")]
    [Range(1.0f, 360.0f)] public float latteralAngleCheckFront = 30.0f;
    [Tooltip("Distance to check to the back of the car when considering the other lane.")]
    [Range(1.0f, 40.0f)] public float latteralDistanceCheckBack = 4.0f;
    [Tooltip("Angle between the nearest car behind of self with direction of self.")]
    [Range(1.0f, 360.0f)] public float latteralAngleCheckBack = 160.0f; 

    [Tooltip("Modifier to how abrupt changes in throttles are.")]
    [Range(0.0f, 1.0f)] public float throttleAdjustmentSpeed = 0.1f; 
    [Tooltip("Modifier to the intensity of braking.")]
    [Range(0.0f, 1.0f)] public float brakingSpeed = 1.0f;

    [Header("Stop Signs Handles")]
    [Tooltip("Strength of break at stop sign.")]
    [Range(1.0f, 40.0f)] public float brakeAtStopSignStrength = 10f;
    [Tooltip("Minimum distance to react to stop sign.")]
    [Range(1.0f, 40.0f)] public float brakeAtStopSignDistance = 40f;
    [Tooltip("Speed at which the car will approach the sign.")]
    [Range(1.0f, 40.0f)] public float targetCruiseSpeedJustBeforeSign = 15f;

    [Header("Lane Change Handles")]
    [Tooltip("Delay before the car decides to change lanes.")]
    [Range(0.0f, 10.0f)] public float maxLaneChangeDelay = 4.0f;
    [Tooltip("Countdown of the current delay timer.")]
    public float laneChangeDelay = 4.0f;

    private Vector3 targetPoint = Vector3.zero;
    private float laneModifier = 1.0f;

    private GameObject nearestStopSign = null;

    [Range(1.0f, 10.0f)] public float stopDuration = 1.0f; // at sign
    private float stopTime = 0.0f;

    [HideInInspector] private float actualTargetSpeed;

    [HideInInspector] public float distanceAlongPath;

    [HideInInspector] public bool useManualPedalInput = false;
    [HideInInspector] public float manualThrottleInput = 0.0f;
    [HideInInspector] public float manualBrakeInput = 0.0f;


    enum State : ushort
    {
        Accelerate,
        Cruise,
        SlowForCurve,
        MaintainDistance,
        ChangeLanes,
        StopAtSign,
        WaitAtStopSign
    }
    private State currentState = State.Accelerate;


    // PPRIVATE METHODS START

    private void FixedUpdate()
    {
        distanceAlongPath = pathCreator.path.GetClosestDistanceAlongPath(transform.position);
        Vector3 normal = pathCreator.path.GetNormalAtDistance(distanceAlongPath + roadLookAheadDistance);
        float speedRatio = carController.GetSpeed() / actualTargetSpeed;

        float curveSeverity = GetCurveSeverity() / (1 + recklessness);

        float signDistance = 0.0f;
        float distanceToSign = 0.0f;

        if (nearestStopSign != null)
        {
            signDistance = pathCreator.path.GetClosestDistanceAlongPath(nearestStopSign.transform.position);
            distanceToSign = signDistance - distanceAlongPath;

            if (distanceToSign < brakeAtStopSignDistance && currentState < State.StopAtSign)
            {
                currentState = State.StopAtSign;
            }
        }
        else
        {
            UpdateNearestStopSign();
        }

        GameObject dangerCar = ForwardCollisionDanger(useRightLane);
        float distanceToOtherCar = 10000.0f;
        if (dangerCar != null)
        {
            distanceToOtherCar = Vector3.Distance(dangerCar.transform.position, transform.position);

            CarController otherController = dangerCar.GetComponent<CarController>();
            if ((!nearestStopSign || distanceToSign > distanceToOtherCar) &&
                (distanceToOtherCar < dangerThreshold / (1 + 5.0f * recklessness) && (otherController.throttle < 0.8f || otherController.brake > 0.1f)) ||
                (distanceToOtherCar < emergencyThreshold - recklessness)
                )
            {
                currentState = State.MaintainDistance;
            }
        }


        switch (currentState)
        {
            case State.Accelerate:
                actualTargetSpeed = targetSpeed;
                carController.brake = 0.0f;

                if (curveSeverity > maximumCurveSeverity)
                {
                    currentState = State.SlowForCurve;
                }
                else if (speedRatio > 1.0f)
                {
                    currentState = State.Cruise;
                }

                    actionText = "Action: Accelerating";
                
                break;
            case State.Cruise:
                actualTargetSpeed = targetSpeed;
                if (curveSeverity > maximumCurveSeverity)
                {
                    currentState = State.SlowForCurve;
                }

                    actionText = "Action: Cruise";
                
                break;
            case State.SlowForCurve:
                if (curveSeverity > maximumCurveSeverity)
                {
                    actualTargetSpeed = Mathf.Clamp(actualTargetSpeed - curveSeverity * Time.deltaTime * 0.5f, targetSpeed / 2.0f, targetSpeed);
                    //actualTargetSpeed = Mathf.Clamp(actualTargetSpeed * (maximumCurveSeverity / curveSeverity), targetSpeed / 2.0f, targetSpeed);
                }
                else
                {
                    currentState = State.Accelerate;
                }

                    actionText = "Action: Slowing for curve";
               
                break;
            case State.MaintainDistance:
                if (!dangerCar)
                {
                    currentState = State.Accelerate;
                    break;
                }
                float otherSpeed = dangerCar.GetComponent<CarController>().GetSpeed();
                float closingRate = carController.GetSpeed() - otherSpeed - 3;
                if (closingRate < 0) closingRate = 0; // if the other car is getting further away
                if (closingRate > closingRateCap) closingRate = closingRateCap; // capped

                actualTargetSpeed = Mathf.Min(targetSpeed - dangerThreshold + distanceToOtherCar + 4.0f, otherSpeed + 1.0f); //jeremy
                float emergencyDistance = emergencyThreshold - 1;
                if (distanceToOtherCar < emergencyDistance) // PANIC!
                {
                    actualTargetSpeed = targetSpeed * recklessness / 2.0f;
                    carController.brake = 1.0f - recklessness * recklessness;
                }
                else// if (carController.GetSpeed() > 15.0f)
                {
                    if (otherSpeed > 15.0f)
                        actualTargetSpeed = Mathf.Min(otherSpeed - 10.0f / distanceToOtherCar, targetSpeed) * (1 - recklessness) + targetSpeed * recklessness;
                    else
                        actualTargetSpeed = Mathf.Min(otherSpeed - 1.0f / distanceToOtherCar, targetSpeed) * (1 - recklessness) + targetSpeed * recklessness;

                    //Debug.Log(actualTargetSpeed);

                    /*                         (closingRate )^2
                                           _______________________ =  break strength
                                            (distanceToOtherCar)^3  
                    */

                    carController.brake = ((closingRate) * (closingRate)) / ((distanceToOtherCar) * (distanceToOtherCar) * (distanceToOtherCar));
                    carController.brake = Mathf.Pow(carController.brake, (1 + 2.0f * recklessness));
                }


                if (distanceToOtherCar > dangerThreshold / dangerExitModifier)// && targetSpeed - actualTargetSpeed < 3.0f)
                {
                    currentState = State.Accelerate;
                }

                    severityText = "Lane Delay: " + laneChangeDelay;
                
                if (distanceToOtherCar < carLookAheadDistance && targetSpeed > otherSpeed)
                {
                    laneChangeDelay -= Time.deltaTime;

                    if (laneChangeDelay < 0.0f && !IsChangingLanes())
                    {
                        if (!LateralCollisionDanger())
                        {
                            currentState = State.ChangeLanes;

                            useRightLane = !useRightLane;
                        }
                        laneChangeDelay = maxLaneChangeDelay + 2.0f * patience;
                    }


                }
                else if (laneChangeDelay < maxLaneChangeDelay)
                {
                    laneChangeDelay += Time.deltaTime / 10.0f;
                }

                    actionText = "Action: Maintain Distance";
                
                break;
            case State.ChangeLanes:
                if ((useRightLane && laneModifier >= 1.0f) || (!useRightLane && laneModifier <= -1.0f))
                {
                    currentState = State.Accelerate;
                }

                if (Mathf.Abs(laneModifier) > 0.5f)
                {
                    actualTargetSpeed = targetSpeed;
                }

                    actionText = "Action: Changing Lanes";
                
                break;
            case State.StopAtSign:

                actualTargetSpeed = distanceToSign + 15.0f;
                carController.brake += (brakeAtStopSignStrength / (distanceToSign)) * Time.deltaTime * brakingSpeed;

                if (carController.GetSpeed() < targetCruiseSpeedJustBeforeSign + (10.0f * recklessness))
                {
                    actualTargetSpeed = targetCruiseSpeedJustBeforeSign + (10.0f * recklessness);
                    carController.brake = 0;
                }

                if (distanceToSign < targetCruiseSpeedJustBeforeSign / 3.0f)
                {
                    actualTargetSpeed = 0.0f;
                    carController.brake = 1.0f;
                }

                if (carController.GetSpeed() < 0.1f)
                {
                    stopTime = stopDuration;
                    currentState = State.WaitAtStopSign;
                }

                    actionText = "Action: Stopping at sign";
                
                break;
            case State.WaitAtStopSign:
                stopTime -= Time.deltaTime;
                if (stopTime <= 0.0f)
                {
                    carController.brake = 0.0f;
                    nearestStopSign = null;
                    currentState = State.Accelerate;
                }

                    actionText = "Action: Waiting at sign";
                
                break;
        }

        AdjustThrottle();
        AdjustSteering(normal);

        if (useManualPedalInput)
        {
            AdjustManualThrottle();
            AdjustManualBrake();
        }
        //UpdateSensors();

            speedText = "Speed: " + carController.GetSpeed().ToString("0.00") + "/" + actualTargetSpeed.ToString("0.00");
        
    }

    public bool IsChangingLanes()
    {
        if (Mathf.Abs(laneModifier) < 1.0f)
        {
            return true;
        }
        return false;
    }

    private void UpdateSensors()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 velocity = rb.velocity;
        velocity *= Time.deltaTime;
        Vector3 futurePos = new Vector3(transform.position.x, transform.position.y + 0.2f, transform.position.z) + velocity;

        Vector3 front = futurePos + transform.forward * 0.5f;
        Vector3 back = futurePos - transform.forward * 0.5f;

        RaycastHit hit;

        Physics.Raycast(front, transform.right, out hit, 1.5f);
        Debug.DrawRay(front, transform.right * 1.5f, Color.Lerp(Color.white, Color.red, hit.distance / 1.5f));

        Physics.Raycast(front, -transform.right, out hit, 1.5f);
        Debug.DrawRay(front, -transform.right * 1.5f, Color.Lerp(Color.white, Color.red, hit.distance / 1.5f));

        Physics.Raycast(back, transform.right, out hit, 1.5f);
        Debug.DrawRay(back, transform.right * 1.5f, Color.Lerp(Color.white, Color.red, hit.distance / 1.5f));

        Physics.Raycast(back, -transform.right, out hit, 1.5f);
        Debug.DrawRay(back, -transform.right * 1.5f, Color.Lerp(Color.white, Color.red, hit.distance / 1.5f));

        Physics.Raycast(futurePos, transform.forward, out hit, 3.0f);
        Debug.DrawRay(futurePos, transform.forward * 3.0f, Color.Lerp(Color.white, Color.red, hit.distance / 3.0f));
    }

    private void AdjustSteering(Vector3 normal)
    {
        if (!useRightLane && laneModifier > -1.0f)
        {
            laneModifier -= changeLaneSpeed * Time.deltaTime;
        }
        else if (useRightLane && laneModifier < 1.0f)
        {
            laneModifier += changeLaneSpeed * Time.deltaTime;
        }
        targetPoint = pathCreator.path.GetPointAtDistance(distanceAlongPath + roadLookAheadDistance) + normal * laneOffset * laneModifier;
        Vector3 dirToTarget = targetPoint - transform.position;
        dirToTarget = dirToTarget.normalized;

        Vector3 localTarget = transform.InverseTransformPoint(targetPoint);
        float angle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

        //Debug.DrawRay(transform.position, dirToTarget, Color.red);
        //Debug.DrawRay(transform.position, transform.forward, Color.yellow);

        carController.steering = angle / carController.maxSteerAngle;
    }

    private float GetCurveSeverity()
    {
        Vector3 normal = pathCreator.path.GetNormalAtDistance(distanceAlongPath + roadLookAheadDistance * 4.0f);
        return Vector3.Angle(normal, transform.right) * carController.GetSpeed() / 45.0f;
    }

    private void AdjustThrottle()
    {
        float speedRatio = 100000000.0f;
        if (carController.GetSpeed() > 0)
            speedRatio = actualTargetSpeed / carController.GetSpeed();

        if (actualTargetSpeed == 0.0f)
        {
            carController.throttle = 0.0f;
            //            carController.brake = 1.0f;
        }
        else if (speedRatio < 1.0f)
        {

            // Reduce throttle
            if (carController.brake < 0.1f) //residual brake
                carController.throttle = Mathf.Clamp(carController.throttle - (1 / speedRatio) * throttleAdjustmentSpeed * Time.deltaTime * (1.0f + recklessness) + 0.001f, 0.0f, 1.0f);
            else
                carController.throttle = 0.0f;
        }
        else
        {
            // Increase Throttle
            if (carController.brake < 0.1f) //residual brake
                carController.throttle = Mathf.Clamp(carController.throttle + (speedRatio) * throttleAdjustmentSpeed * Time.deltaTime * (1.0f + 10.0f * recklessness) * (2.0f - patience) + 0.001f, 0.0f, 1.0f);
            else
                carController.throttle = 0.0f;
        }
    }


    private void AdjustManualThrottle()
    {
        carController.throttle = manualThrottleInput;
    }

    private void AdjustManualBrake()
    {
        carController.brake = manualBrakeInput;
    }


    private void UpdateNearestStopSign()
    {
        GameObject[] objs = GameObject.FindGameObjectsWithTag("StopSign");

        float nearestDistance = 0.0f;

        float myDistance = pathCreator.path.GetClosestDistanceAlongPath(transform.position);

        foreach (GameObject obj in objs)
        {
            float distance = pathCreator.path.GetClosestDistanceAlongPath(obj.transform.position);
            if (distance > myDistance + 5.0f && (distance < nearestDistance || nearestDistance == 0.0f))
            {
                nearestStopSign = obj;
                nearestDistance = distance;
            }
        }

        if (nearestDistance == 0.0f)
        {
            nearestStopSign = null;
        }
    }

    private GameObject LateralCollisionDanger()
    {
        GameObject result = null;
        float closestDistance = 100000.0f;
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Car");
        foreach (GameObject obj in objs)
        {
            if (obj != gameObject)
            {
                AIDriver1 otherCar = obj.GetComponent<AIDriver1>();

                Vector3 toOtherCar = (otherCar.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toOtherCar);

                float distance = Vector3.Distance(otherCar.transform.position, transform.position);

                float speedDiff = (targetSpeed - actualTargetSpeed);
                float impatience = (patience * 10.0f);
                //Debug.Log(obj.name + " - Angle: " + angle + "\tDistance: " + distance + "\tDiff: " + speedDiff + "\tImpatience: " + impatience);

                if (speedDiff > impatience)
                {
                    if (distance < latteralDistanceCheckBack - (recklessness * 3.0f) && angle > latteralAngleCheckFront/3.75f)
                    {
                        closestDistance = distance;
                        result = obj;
                    }
                }
                else if ((distance < latteralDistanceCheckBack - (recklessness * 3.0f) && angle < latteralAngleCheckBack) || // Check slightly behind us
                    (distance < latteralDistanceCheckFront - (recklessness * 10.0f) && angle > latteralAngleCheckFront) // Check in front
                    )
                {
                    closestDistance = distance;
                    result = obj;
                }
            }
        }
        return result;
    }

    private GameObject ForwardCollisionDanger(bool considerRightLine)
    {
        GameObject result = null;
        float closestDistance = 100000.0f;
        GameObject[] objs = GameObject.FindGameObjectsWithTag("Car");
        foreach (GameObject obj in objs)
        {
            if (obj != gameObject)
            {
                AIDriver1 otherCar = obj.GetComponent<AIDriver1>();

                Vector3 toOtherCar = (otherCar.transform.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, toOtherCar);

                if ((otherCar.useRightLane == considerRightLine || obj.GetComponent<AIDriver1>().IsChangingLanes()) && Mathf.Abs(angle) < 100.0f)
                {
                    //float distance = otherCar.distanceAlongPath - distanceAlongPath;
                    float distance = Vector3.Distance(otherCar.transform.position, transform.position);

                    if (distance > 0.0f && distance < closestDistance)
                    {
                        closestDistance = distance;
                        result = obj;
                    }
                }
            }
        }
        return result;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(targetPoint, 0.3f);

        if (nearestStopSign != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(nearestStopSign.transform.position, 1.0f);
        }
    }

    // PPRIVATE METHODS END


    // PUBLIC METHODS START (FOR USER ACCESS)

    public void SetUseManualInput(bool val)
    {
        useManualPedalInput = val;
    }

    public void SetManualThrottle(float val)
    {
        manualThrottleInput = val;
    }

    public void SetManualBrake(float val)
    {
        manualBrakeInput = val;
    }

    public void SetRecklessness(float val)
    {
        recklessness = val;
    }

    public void SetPatience(float val)
    {
        patience = val;
    }

    public void ForceChangeLane()
    {
        useRightLane = !useRightLane;
    }

    public void SetLaneChangeSpeed(float val)
    {
        changeLaneSpeed = 0.5f + 4.5f * val; // speed range 0.5 to 5
        maxLaneChangeDelay = 10.0f - 9.5f * val; // speed range 0.5 to 5
    }

    public void SetDesiredCruiseSpeed(float val)
    {
        targetSpeed = 65.0f * val;
    }

    // PUBLIC METHODS END (FOR USER ACCESS)
}
