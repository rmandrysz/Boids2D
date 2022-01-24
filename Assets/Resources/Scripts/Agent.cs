using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{   
    private Vector3 direction;

    [HideInInspector]
    public Vector3 flockmateCollisionAvoidance = Vector3.zero;
    public Vector3 averageFlockmateVelocity = Vector3.zero;
    public Vector3 averageFlockCenter = Vector3.zero;
    public int numFlockmates = 0;

    public bool debug = false;

    private float averageSpeed;
    private List<(string name, float magnitude, Vector3 direction)> accumulator;

    [Header ("References")]
    public AgentSettings settings;

    private void Awake()
    {
        ResetAccumulators();

        averageSpeed = (settings.minSpeed + settings.maxSpeed) / 2f;

        float x = Random.Range(-1f, 1f);
        float y = Random.Range(-1f, 1f);
        direction = new Vector3(x, y, 0f).normalized;

        direction *= averageSpeed;

    }

    public void AgentUpdate(float dt)
    {
        RotateInMoveDirection();
        Move(dt);
                    
        AvoidWalls();
        MatchVelocity();
        AvoidFlockmateCollisions();
        MoveToFlockCenter();

        UpdateDirection();
        ResetAccumulators();
    }

    private void Move(float dt)
    {
        transform.Translate(direction * dt, Space.World);

        direction = direction.normalized * averageSpeed;
    }

    private void AvoidFlockmateCollisions()
    {
        RequestDirection(settings.flockmateAvoidanceWeight * flockmateCollisionAvoidance, "Avoid Flockmates");
    }

    private void MatchVelocity()
    {
        if (numFlockmates == 0)
        {
            return;
        }
        averageFlockmateVelocity /= numFlockmates;
        RequestDirection(settings.velocityMatchingWeight * averageFlockmateVelocity, "Match Velocity");
    }

    private void MoveToFlockCenter()
    {
        if (numFlockmates == 0)
        {
            return;
        }
        averageFlockCenter /= numFlockmates;
        RequestDirection(settings.flockCenteringWeight * (averageFlockCenter - transform.position), "Move to Center");
        if(debug)
        {
            Debug.DrawRay(transform.position, averageFlockCenter - transform.position, Color.yellow);
        }
    }

    private void AvoidWalls()
    {
        Color color = Color.white;
        if (IsDetectingColision())
        {
            color = Color.red;
            RequestDirection(settings.obstacleAvoidanceWeight * FindDir(), "Avoid Obstacles");
        }
        if (debug)
        {
            Debug.DrawRay(transform.position, direction.normalized * settings.collisionAvoidDistance, color);
        }
    }

    private bool IsDetectingColision()
    {
        return Physics2D.CircleCast(transform.position, settings.circleCastRadius, direction, settings.collisionAvoidDistance, settings.obstacleLayer);
    }

    private Vector3 FindDir()
    {
        Vector3 dir = direction;

        foreach (var angle in AngleCalculator.angles)
        {
            dir = Quaternion.AngleAxis(angle, Vector3.forward) * direction.normalized;

            if(!Physics2D.CircleCast(transform.position, settings.circleCastRadius, dir, settings.collisionAvoidDistance, settings.obstacleLayer))
            {
                if (debug)
                {
                    Debug.DrawRay(transform.position, dir * settings.collisionAvoidDistance, Color.white);
                }
                return dir;
            }
            if (debug)
            {
                Debug.DrawRay(transform.position, dir * settings.collisionAvoidDistance, Color.red);
            }
        }
        
        return dir;
    }

    private void RequestDirection(Vector3 dir, string name)
    {
        accumulator.Add((name, dir.magnitude, dir.normalized));
    }

    private void UpdateDirection()
    {
        float magnitudeLeft = settings.maxInfluence;
        if(debug)
        {
            Debug.Log("Calculating Direction. Magnitude left: " + magnitudeLeft);
        }

        foreach (var request in accumulator)
        {
            if (magnitudeLeft >= request.magnitude)
            {
                direction += request.direction * request.magnitude;
                magnitudeLeft -= request.magnitude;
                if(debug)
                {
                    Debug.Log("Taking " + request.name + " into account. Request magnitude: " + request.magnitude +  ", magnitude left: " + magnitudeLeft);
                }
            }
            else if (magnitudeLeft > 0)
            {
                direction += request.direction * magnitudeLeft;
                magnitudeLeft = 0;
                if(debug)
                {
                    Debug.Log("Taking " + request.name + " into account. Request magnitude: " + request.magnitude +  ", Accumulator exceeded. No magnitude left");
                }
            }
            else 
            {
                if(debug)
                {
                    Debug.Log("Accumulator overflow, skipping request: " + request.name);
                }
            }
        }

        Vector3.ClampMagnitude(direction, settings.maxSpeed);
    }

    private void ResetAccumulators()
    {
        accumulator = new List<(string name, float magnitude, Vector3 direction)>();
        flockmateCollisionAvoidance = Vector3.zero;
        averageFlockCenter = Vector3.zero;
        averageFlockmateVelocity = Vector3.zero;
    }

    private void RotateInMoveDirection()
    {
        Vector3 targetPos = transform.position + (direction);
        var relativePos = targetPos - transform.position;

        var angle = Mathf.Atan2(relativePos.y, relativePos.x) * Mathf.Rad2Deg - 90f;
        var rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        transform.rotation = rotation;
    }

    public Vector3 GetDirection()
    {
        return direction;
    }
}
