/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/26/2026
 * Last Modified:   08/26/2026 (Ryan)
 * Notes:           Handles collisions with other objects
*/
using System.Collections;
using UnityEngine;

namespace RyansLibrary.Physics
{
    public class CollisionReactor : MonoBehaviour
    {
        public void ElasticCollision(Collider colliderA, Collider colliderB)
        {
            // Sphere + Sphere collision
            if (colliderA is SphereCollider sphereA && colliderB is SphereCollider sphereB)
            {
                StartCoroutine(ElasticSphereCollisionCo(sphereA, sphereB));
            }
        }

        /// <summary>
        /// Handle an elastic collision between two spheres.
        /// </summary>
        private IEnumerator ElasticSphereCollisionCo(SphereCollider sphereA, SphereCollider sphereB)
        {
            ForceReceiver fRA = sphereA.GetComponent<ForceReceiver>();
            ForceReceiver fRB = sphereB.GetComponent<ForceReceiver>();

            // Extract parameters from force recievers
            Vector3 centerA = sphereA.bounds.center; // Convert center to world space
            Vector3 centerB = sphereB.bounds.center;
            float mA = fRA.Mass;
            float mB = fRB.Mass;
            Vector3 vA = fRA.Velocity;
            Vector3 vB = fRB.Velocity;
            Vector3 normal = (centerB - centerA).normalized;

            // Skip resolution if the spheres are already separating (or stationary)
            // along the normal, otherwise slow-moving balls that stay overlapping for
            // multiple ticks get their collision re-resolved repeatedly and gain energy.
            float closingSpeed = Vector3.Dot(vA - vB, normal);
            if (closingSpeed <= 0)
                yield break;

            // Final Velocity
            Vector3 vF = Vector3.zero;

            // Calculate elastic collision
            // Simplify calculation of velocity if masses are equal; velocity exchange
            vF = (mA == mB) ? ElasticCollision(vA, vB, normal) : ElasticCollision(mA, mB, vA, vB, normal);

            fRA.AddForce(vF - fRA.Velocity);

            // Push the spheres apart by their overlap so they don't remain in contact
            // and immediately re-trigger the same collision next tick.
            float intersection = (sphereA.radius + sphereB.radius) - Vector3.Distance(sphereA.bounds.center, sphereB.bounds.center);
            if (intersection > 0)
                sphereA.transform.position -= normal * (intersection * 0.5f);
        }

        private Vector3 ElasticCollision(float massA, float massB, Vector3 vA, Vector3 vB, Vector3 normal)
        {
            // normal = (posB - posA).normalized, pointing from A to B

            Vector3 vAn = Vector3.Dot(vA, normal) * normal;   // A's velocity along the normal
            Vector3 vAt = vA - vAn;                           // A's velocity tangent to the normal

            Vector3 vBn = Vector3.Dot(vB, normal) * normal;

            // Apply the 1D formula only to the normal components
            Vector3 vAnf = (((massA - massB) / (massA + massB)) * vAn) + (((2 * massB) / (massA + massB)) * vBn);

            return vAnf + vAt;
        }

        private Vector3 ElasticCollision(Vector3 vA, Vector3 vB, Vector3 normal)
        {
            // normal = (posB - posA).normalized, pointing from A to B
            Vector3 vAn = Vector3.Dot(vA, normal) * normal;   // A's velocity along the normal
            Vector3 vAt = vA - vAn;                           // A's velocity tangent to the normal

            Vector3 vBn = Vector3.Dot(vB, normal) * normal;

            // Apply the 1D formula only to the normal components
            Vector3 vAnf = vAn + vBn;

            return vAnf + vAt;
        }
    }
}
