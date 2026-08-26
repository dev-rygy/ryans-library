/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/26/2026
 * Last Modified:   08/26/2026 (Ryan)
 * Notes:           Detects collisions with other objects
*/
using UnityEngine;
using UnityEngine.Events;

namespace RyansLibrary.Physics
{
    public class CollisionDetector : MonoBehaviour
    {
        [Header("Continuous Collision Detection")]
        [Tooltip("The amount of movement needed on an object to toggle a continuous collision detection")]
        [SerializeField] private float movementDetectPrecision = 0.0001f;

        [Header("Collision Events")]
        [SerializeField] private UnityEvent<Collider, Collider> _onCollision;

        private Collider _collider;
        private Vector3 _previousPosition;

        private void Start()
        {
            _collider = GetComponent<Collider>();
            _previousPosition = transform.position;
        }

        private void FixedUpdate()
        {
            CheckCollision();
        }

        private void CheckCollision()
        {
            if (_collider == null)
                return;

            if (_collider is SphereCollider sphereCollider)
                CheckSphereCollision(sphereCollider);
            else
                Debug.Log("Collision type has either not been implemented yet or is not valid.");

        }

        /// <summary>
        /// Detect collisions with sphere
        /// </summary>
        /// <param name="effectorSphereCollider">The collider this script is attached to.</param>
        private void CheckSphereCollision(SphereCollider effectorSphereCollider)
        {
            Collider[] collisions;

            // If object has movement then use continuous collision detection
            Vector3 effectorCenter = effectorSphereCollider.bounds.center; // Convert center to world space
            Vector3 movement = effectorCenter - _previousPosition;
            float distance = movement.magnitude;

            if (distance > movementDetectPrecision)
            {
                // Continuous collision detection if balls are close
                collisions = ContinuousSphereCollisionCheck(effectorCenter, effectorSphereCollider.radius, movement, distance);
            }
            else
            {
                // Ball didn't move this tick; a swept check needs a direction, so fall back to a stationary overlap check.
                collisions = DiscreteSphereCollisionCheck(effectorCenter, effectorSphereCollider.radius);
            }

            foreach (Collider effectedCollider in collisions)
            {
                if (effectedCollider == _collider)      // Prevent object from colliding with itself
                    continue;

                if (effectedCollider is SphereCollider effectedSphereCollider)   //  Sphere + Sphere collision
                {
                    Vector3 effectedCenter = effectedSphereCollider.bounds.center; // Convert center to world space
                    Vector3 normal = (effectedCenter - effectorCenter).normalized;

                    //StartCoroutine(HandleSphereCollision(effectorSphereCollider, effectedSphereCollider));
                    _onCollision?.Invoke(effectorSphereCollider, effectedSphereCollider);
                }
            }
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
