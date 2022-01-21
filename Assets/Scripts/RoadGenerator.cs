using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadGenerator : MonoBehaviour
{
    public PathCreation.PathCreator pathCreator;
    public PathCreation.Examples.RoadMeshCreator roadMeshCreator;
    public GameObject stopSignPrefab;

    private LinkedList<GameObject> stopSigns = new LinkedList<GameObject>();

    [Range(0.0f, 20.0f)] public float roadChaos = 10.0f;

    public void LateUpdate()
    {
        List<GameObject> extremeCars = FindExtremeCars();
        GenerateRoadSegment(extremeCars[1]);

        CleanupRoadSegments(extremeCars[0]);

        CreateStopSign();
        CleanupStopSigns(extremeCars[0]);

        roadMeshCreator.TriggerUpdate();
    }

    private void GenerateRoadSegment(GameObject farthestCar)
    {
        float distance = pathCreator.path.GetClosestDistanceAlongPath(farthestCar.transform.position);

        if (pathCreator.path.length - distance < 50.0f)
        {
            Vector3 lastPoint = pathCreator.path.GetPoint(pathCreator.path.NumPoints - 1);
            Vector3 tangent = pathCreator.path.GetTangent(pathCreator.path.NumPoints - 1);

            float randX = (Random.Range(0.0f, roadChaos) / 10.0f);
            float randZ = (Random.Range(0.0f, roadChaos) / 10.0f);
            Vector3 dir = new Vector3(tangent.x + randX, tangent.y, tangent.z + randZ).normalized;
            dir = dir * 10.0f;
            pathCreator.bezierPath.AddSegmentToEnd(lastPoint + dir);
        }
    }

    private void CleanupRoadSegments(GameObject nearestCar)
    {
        float distance = pathCreator.path.GetClosestDistanceAlongPath(nearestCar.transform.position);
        if (distance > 50.0f)
        {
            pathCreator.bezierPath.DeleteSegment(0);
        }
    }

    private void CreateStopSign()
    {
        if(Input.GetKeyUp(KeyCode.S))
        {
            GameObject[] objs = GameObject.FindGameObjectsWithTag("StopSign");

            Vector3 roadPoint = pathCreator.path.GetPoint(pathCreator.path.NumPoints - 1);
            Vector3 normal = pathCreator.path.GetNormal(pathCreator.path.NumPoints - 1);
            
            Vector3 point = new Vector3(roadPoint.x, 0.5f, roadPoint.z) + normal * 2f;
            GameObject newStopSign = Instantiate(stopSignPrefab, point, Quaternion.LookRotation(normal));
            stopSigns.AddLast(newStopSign);
        }
    }

    private void CleanupStopSigns(GameObject nearestCar)
    {
        if(stopSigns.Count > 0) {
            GameObject stopSign = stopSigns.First.Value;
            Vector3 roadPoint = pathCreator.path.GetPoint(pathCreator.path.NumPoints - 1);
            float distance = Vector3.Distance(stopSigns.First.Value.transform.position, roadPoint);
            if (Vector3.Distance(stopSign.transform.position, roadPoint) > 100.0f && Vector3.Distance(stopSign.transform.position, nearestCar.transform.position) > 50.0f)
            {
                Destroy(stopSigns.First.Value);
                stopSigns.RemoveFirst();
            }
        }
    }

    private List<GameObject> FindExtremeCars()
    {
        GameObject farthestCar = null;
        float farthestDistance = 0.0f;
        GameObject nearestCar = null;
        float nearestDistance = 0.0f;

        GameObject[] cars = GameObject.FindGameObjectsWithTag("Car");

        foreach (GameObject car in cars)
        {
            float distance = pathCreator.path.GetClosestDistanceAlongPath(car.transform.position);
            if(!farthestCar || distance > farthestDistance)
            {
                farthestDistance = distance;
                farthestCar = car;
            }

            if(!nearestCar || distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestCar = car;
            }
        }

        List<GameObject> result = new List<GameObject>();
        result.Add(nearestCar);
        result.Add(farthestCar);

        return result;
    }

    public void Awake()
    {
        pathCreator.InitializeEditorData(false);
    }
}
