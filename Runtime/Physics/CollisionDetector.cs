/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/26/2026
 * Last Modified:   09/02/2026 (Ryan)
 * Notes:           Detects collisions with other objects
*/
using System;
using UnityEngine;
using UnityEngine.Events;

namespace RyansLibrary.Physics
{
    // Ensure this script runs before any ForceReciever script
    [DefaultExecutionOrder(-100)]
    public class CollisionDetector : MonoBehaviour
    {
        private enum CollisionCheckType
        {
            Discrete,
            Continuous,
            Dynamic
        }

        private bool _isEnabled = true;

        [Header("Collision Type")]
        [Tooltip("The amount of movement needed on an object to toggle a continuous collision detection")]
        [SerializeField] private CollisionCheckType _detectionType = CollisionCheckType.Discrete;
        [SerializeField] private float movementDetectPrecision = 0.0001f;

        [Header("Collision Events")]
        [SerializeField] private UnityEvent<Collider, Collider> _onCollision;

        [Header("Debug")]
        [SerializeField] private bool _debug;

        private Collider _collider;
        private ForceReceiver _forceReceiver;
        private Vector3 _previousPosition;

        private void Start()
        {
            _collider = GetComponent<Collider>();
            _forceReceiver = GetComponent<ForceReceiver>();

            _previousPosition = (_collider is SphereCollider sphere) ? transform.TransformPoint(sphere.center) : transform.position;
        }

        private void FixedUpdate()
        {
            CheckCollision();
        }

        private void CheckCollision()
        {
            if (!_isEnabled)        // TODO: Only enable this when the cue hits a ball to save on performance and bandwith
                return;

            if (_collider == null || _forceReceiver == null)
                return;

            if (_forceReceiver.IsStatic)    // Static objects don't need to check for collisions
                return;

            if (_collider is SphereCollider sphereCollider)
                CheckCollision(sphereCollider);
            else if (_collider is BoxCollider boxCollider)       // TODO: Not implemented yet
                CheckCollision(boxCollider);
            else
                Debug.Log("Collision type has either not been implemented yet or is not valid.");

        }

        /// <summary>
        /// Detect collisions with sphere
        /// </summary>
        /// <param name="effectorSphereCollider">The collider this script is attached to.</param>
        private void CheckCollision(SphereCollider effectorSphereCollider)
        {
            // If object has movement then use continuous collision detection
            // Centre from the transform, not Collider.bounds, so it matches the reactor and reflects any push made earlier this step
            Vector3 effectorCenter = transform.TransformPoint(effectorSphereCollider.center); // Convert center to world space
            Vector3 movement = effectorCenter - _previousPosition;
            float distance = movement.magnitude;

            switch (_detectionType)
            {
                case CollisionCheckType.Discrete:
                    DiscreteSphereCollisionCheck(effectorSphereCollider, effectorCenter);
                    break;
                case CollisionCheckType.Continuous:
                    ContinuousSphereCollisionCheck(effectorSphereCollider, effectorCenter, movement, distance);
                    break;
                case CollisionCheckType.Dynamic:
                    if (distance > movementDetectPrecision)
                    {
                        // Continuous collision detection if balls are close
                        ContinuousSphereCollisionCheck(effectorSphereCollider, effectorCenter, movement, distance);
                    }
                    else
                    {
                        // Half-asleep - balls that are not moving, so use discrete collision detection to save CPU cycles.
                        DiscreteSphereCollisionCheck(effectorSphereCollider, effectorCenter);
                    }
                    break;
                default:
                    DiscreteSphereCollisionCheck(effectorSphereCollider, effectorCenter);
                    break;
            }
        }

        private void InvokeCollision(SphereCollider effectorSphereCollider, Collider effectedCollider)
        {
            if (effectedCollider == _collider)      // Prevent object from colliding with itself
                return;

            if (effectedCollider is SphereCollider effectedSphereCollider)   //  Sphere + Sphere collision
            {
                _onCollision?.Invoke(effectorSphereCollider, effectedSphereCollider);
                if (_debug) Debug.Log($"Collision detected between {effectorSphereCollider.name} and {effectedSphereCollider.name}");
            }

            if (effectedCollider is BoxCollider effectedBoxCollider)   //  Sphere + Box collision
            {
                _onCollision?.Invoke(effectorSphereCollider, effectedBoxCollider);
                if (_debug) Debug.Log($"Collision detected between {effectorSphereCollider.name} and {effectedBoxCollider.name}");
            }
        }

        private void CheckCollision(BoxCollider effectorBoxCollider)
        {
            // TODO: Implement box collision detection
        }

        private void ContinuousSphereCollisionCheck(SphereCollider effectorSphereCollider, Vector3 center, Vector3 movement, float distance)
        {
            // Sweep from where the ball was last tick to where it is now
            Vector3 direction = movement / distance;
            RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(_previousPosition, effectorSphereCollider.radius, direction, distance);

            // Sort hits by nearest first as the first contact along the sweep is the one that actually happened
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            Vector3 velocityAtStart = _forceReceiver.Velocity;
            Vector3 finalCenter = center;

            // Pass 1: contacts already touching at the start of the sweep; resolve them where the ball is now
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider != _collider && hit.distance <= 0f)
                    InvokeCollision(effectorSphereCollider, hit.collider);
            }

            // If any of those changed this ball's velocity, the sweep path is out of date, so don't move back along it
            if (_forceReceiver.Velocity != velocityAtStart)
            {
                _previousPosition = center;
                return;
            }

            // Pass 2: the nearest contact along the sweep that the reactor actually resolves
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider == _collider || hit.distance <= 0f)
                    continue;

                // Resolving from the end-of-step position gets the contact normal wrong once the ball
                // has passed the other's centre, so move back to where the contact happened first.
                Vector3 originalPosition = transform.position;
                Vector3 contactCenter = _previousPosition + direction * hit.distance;
                transform.position += contactCenter - center;

                InvokeCollision(effectorSphereCollider, hit.collider);

                // The reactor changes velocity when it resolves a hit, so a change means it acted:
                // stay at the contact point and ignore the rest of the sweep.
                if (_forceReceiver.Velocity != velocityAtStart)
                {
                    finalCenter = contactCenter;
                    break;
                }

                // Nothing resolved (e.g. the other object was moving away, or has no ForceReceiver),
                // so put the ball back and keep checking.
                transform.position = originalPosition;
            }

            _previousPosition = finalCenter;
        }

        private void DiscreteSphereCollisionCheck(SphereCollider effectorSphereCollider, Vector3 center)
        {
            // Ball didn't move this tick; a swept check needs a direction, so fall back to a stationary overlap check.
            foreach (Collider effectedCollider in UnityEngine.Physics.OverlapSphere(center, effectorSphereCollider.radius))
                InvokeCollision(effectorSphereCollider, effectedCollider);

            _previousPosition = center;
        }
    }
}
