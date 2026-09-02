/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/26/2026
 * Last Modified:   09/02/2026 (Ryan)
 * Notes:           Detects collisions with other objects
*/
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
            _previousPosition = transform.position;
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
            Collider[] collisions;

            // If object has movement then use continuous collision detection
            Vector3 effectorCenter = effectorSphereCollider.bounds.center; // Convert center to world space
            Vector3 movement = effectorCenter - _previousPosition;
            float distance = movement.magnitude;

            switch (_detectionType)
            {
                case CollisionCheckType.Discrete:
                    collisions = DiscreteSphereCollisionCheck(effectorCenter, effectorSphereCollider.radius);
                    break;
                case CollisionCheckType.Continuous:
                    collisions = ContinuousSphereCollisionCheck(effectorCenter, effectorSphereCollider.radius, movement, distance);
                    break;
                case CollisionCheckType.Dynamic:
                    if (distance > movementDetectPrecision)
                    {
                        // Continuous collision detection if balls are close
                        collisions = ContinuousSphereCollisionCheck(effectorCenter, effectorSphereCollider.radius, movement, distance);
                    }
                    else
                    {
                        // Half-asleep - balls that are not moving, so use discrete collision detection to save CPU cycles.
                        collisions = DiscreteSphereCollisionCheck(effectorCenter, effectorSphereCollider.radius);
                    }
                    break;
                default:
                    collisions = DiscreteSphereCollisionCheck(effectorCenter, effectorSphereCollider.radius);
                    break;
            }

            foreach (Collider effectedCollider in collisions)
            {
                if (effectedCollider == _collider)      // Prevent object from colliding with itself
                    continue;

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
        }

        private void CheckCollision(BoxCollider effectorBoxCollider)
        {
            // TODO: Implement box collision detection
        }

        private Collider[] ContinuousSphereCollisionCheck(Vector3 center, float radius, Vector3 movement, float distance)
        {
            Collider[] collisions;

            // Sweep from where the ball was last tick to where it is now, so a fast-moving
            // ball can't tunnel through another between two discrete position samples.
            RaycastHit[] hits = UnityEngine.Physics.SphereCastAll(_previousPosition, radius, movement.normalized, distance);
            collisions = new Collider[hits.Length];
            for (int i = 0; i < hits.Length; i++)
            {
                collisions[i] = hits[i].collider;
            }

            _previousPosition = center;

            return collisions;
        }

        private Collider[] DiscreteSphereCollisionCheck(Vector3 center, float radius)
        {
            // Ball didn't move this tick; a swept check needs a direction, so fall back to a stationary overlap check.
            return UnityEngine.Physics.OverlapSphere(center, radius);
        }
    }
}
