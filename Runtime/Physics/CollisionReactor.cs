/*
 * Created By:      Ryan Carpenter
 * Date Created:    08/26/2026
 * Last Modified:   09/02/2026 (Ryan)
 * Notes:           Handles elastic collisions with other objects
*/
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
                ElasticSphereToSphereCollision(sphereA, sphereB);
            }

            // Sphere + Box collision
            else if (colliderA is SphereCollider sphere && colliderB is BoxCollider box)
            {
                ElasticSphereToBoxCollision(sphere, box);
            }
        }

        /// <summary>
        /// Handle an elastic collision between two spheres.
        /// </summary>
        private void ElasticSphereToSphereCollision(SphereCollider sphereA, SphereCollider sphereB)
        {
            ForceReceiver fRA = sphereA.GetComponent<ForceReceiver>();
            ForceReceiver fRB = sphereB.GetComponent<ForceReceiver>();

            if (fRA == null || fRB == null)
                return;

            // Extract parameters from force recievers; mass is infinite for static objects
            Vector3 centerA = sphereA.bounds.center; // Convert center to world space
            Vector3 centerB = sphereB.bounds.center;
            float mA = fRA.Mass;
            float mB = fRB.Mass;
            Vector3 vA = fRA.Velocity;
            Vector3 vB = fRB.Velocity;
            Vector3 normal = (centerB - centerA).normalized;

            // Skip resolution if the objects are already separating or stationary
            if (IsMovingAway(vA, vB, normal))
                return;

            // Final Velocity
            Vector3 vF = Vector3.zero;

            // Calculate elastic collision
            // Simplify calculation of velocity if masses are equal; velocity exchange
            vF = (mA == mB) ? ElasticCollision(vA, vB, normal) : ElasticCollision(mA, mB, vA, vB, normal);

            fRA.AddForce(vF - fRA.Velocity);

            // Push the spheres apart by their overlap so they don't remain in contact
            float intersection = (sphereA.radius + sphereB.radius) - Vector3.Distance(sphereA.bounds.center, sphereB.bounds.center);
            if (intersection > 0)
                sphereA.transform.position -= normal * (intersection * 0.5f);
        }

        /// <summary>
        /// Handle an elastic collision between a sphere and a box
        /// </summary>
        private void ElasticSphereToBoxCollision(SphereCollider sphere, BoxCollider box)
        {
            ForceReceiver fRA = sphere.GetComponent<ForceReceiver>();
            ForceReceiver fRB = box.GetComponent<ForceReceiver>();

            if (fRA == null || fRB == null)
                return;

            // Extract parameters from force recievers; mass is infinite for static objects
            float mA = fRA.Mass;
            float mB = fRB.Mass;
            Vector3 vA = fRA.Velocity;
            Vector3 vB = fRB.Velocity;

            // Unlike two spheres, the contact normal depends on which face of the box the
            // sphere is nearest, so it has to be derived from the box geometry.
            GetSphereBoxContactNormal(box, sphere.bounds.center, out Vector3 normal, out float surfaceDistance);

            // Skip resolution if the objects are already separating or stationary
            if (IsMovingAway(vA, vB, normal))
                return;

            // Final Velocity
            Vector3 vF = Vector3.zero;

            // Calculate elastic collision
            // Simplify calculation of velocity if masses are equal; velocity exchange
            vF = (mA == mB) ? ElasticCollision(vA, vB, normal) : ElasticCollision(mA, mB, vA, vB, normal);

            fRA.AddForce(vF - vA);

            // Push the sphere and box apart by their overlap so they don't remain in contact
            float intersection = sphere.radius - surfaceDistance;
            if (intersection > 0)
                sphere.transform.position -= normal * (fRB.IsStatic ? intersection : intersection * 0.5f);
        }

        /// <summary>
        /// Find the contact normal between a sphere centre and a box. The normal points from the
        /// sphere toward the box, matching the sphere to sphere convention.
        /// </summary>
        /// <param name="surfaceDistance">Negative when the sphere's centre is inside the box.</param>
        private void GetSphereBoxContactNormal(BoxCollider box, Vector3 sphereCenter, out Vector3 normal, out float surfaceDistance)
        {
            // *** Scenario 1: The sphere's centre is outside the box, so ClosestPoint returns a point on the
            Vector3 normalBallToWall = box.ClosestPoint(sphereCenter) - sphereCenter;
            surfaceDistance = normalBallToWall.magnitude;

            // If the sphere's centre is outside the box
            if (surfaceDistance > Mathf.Epsilon)
            {
                normal = normalBallToWall / surfaceDistance;
                return;
            }

            // *** Scenario 2: The sphere's centre is inside the box, so ClosestPoint returns the centre itself.
            // Fall back to the nearest face - measure how deep the centre sits on each local axis and escape through the shallowest.
            // - box.center because in case collider is offset from transform origin; handles offset colliders correctly.
            Vector3 localCenter = box.transform.InverseTransformPoint(sphereCenter) - box.center;
            Vector3 halfExtents = box.size * 0.5f; // Radius/half of the box so to speak

            // Find the sphere's distance from each face of the box along each axis.
            // The smallest distance is the nearest face.
            Vector3 scale = box.transform.lossyScale;
            float depthX = (halfExtents.x - Mathf.Abs(localCenter.x)) * Mathf.Abs(scale.x);
            float depthY = (halfExtents.y - Mathf.Abs(localCenter.y)) * Mathf.Abs(scale.y);
            float depthZ = (halfExtents.z - Mathf.Abs(localCenter.z)) * Mathf.Abs(scale.z);

            // Points from the box centre out toward the sphere
            Vector3 localNormal;

            if (depthX <= depthY && depthX <= depthZ)   // X is the shallowest axis
            {
                // Normal = (1, 0, 0) or (-1, 0, 0) depending on which side of the box the sphere is on.
                localNormal = new Vector3(Mathf.Sign(localCenter.x), 0f, 0f);
                surfaceDistance = -depthX;
            }
            else if (depthY <= depthZ)                  // Y is the shallowest axis
            {
                // Normal = (0, 1, 0) or (0, -1, 0) depending on which side of the box the sphere is on.
                localNormal = new Vector3(0f, Mathf.Sign(localCenter.y), 0f);
                surfaceDistance = -depthY;
            }
            else                                        // Z is the shallowest axis
            {
                // Normal = (0, 0, 1) or (0, 0, -1) depending on which side of the box the sphere is on.
                localNormal = new Vector3(0f, 0f, Mathf.Sign(localCenter.z));
                surfaceDistance = -depthZ;
            }

            // Flip to match the sphere - box convention used above.
            normal = -box.transform.TransformDirection(localNormal).normalized;
        }

        /// <summary>
        /// Elastic collision between two objects of arbitrary mass. 
        /// The normal is the direction from A to B, and only the normal components of the velocities are affected
        /// </summary>
        /// <param name="massA">Mass of object A</param>
        /// <param name="massB">Mass of object B</param>
        /// <param name="vA">Velocity of object A</param>
        /// <param name="vB">Velocity of object B</param>
        /// <param name="normal">Normal vector of the collision</param>
        /// <returns></returns>
        private Vector3 ElasticCollision(float massA, float massB, Vector3 vA, Vector3 vB, Vector3 normal)
        {
            Vector3 vAn = Vector3.Dot(vA, normal) * normal;   // A's velocity along the normal
            Vector3 vAt = vA - vAn;                           // A's velocity tangent to the normal

            Vector3 vBn = Vector3.Dot(vB, normal) * normal;

            // Written as a mass ratio rather than the textbook (mA - mB) / (mA + mB) so that an immovable body can pass
            // massRatio = 1 - masses are equal
            // massRatio = 0 - B is infinitely heavier than A
            // massRatio = infinity - A is infinitely heavier than B
            float massRatio = massA / massB;

            // Elastic collision formula for 1D along the normal, applied only to the normal components of the velocities.
            Vector3 vAnf = (((massRatio - 1f) / (massRatio + 1f)) * vAn) + ((2f / (massRatio + 1f)) * vBn);

            return vAnf + vAt;
        }

        /// <summary>
        /// Perfectly elastic collision between two objects of equal mass.
        /// </summary>
        /// <param name="vA">Velocity of object A</param>
        /// <param name="vB">Velocity of object B</param>
        /// <param name="normal">Normal vector of the collision</param>
        private Vector3 ElasticCollision(Vector3 vA, Vector3 vB, Vector3 normal)
        {
            Vector3 vAn = Vector3.Dot(vA, normal) * normal;   // A's velocity along the normal
            Vector3 vAt = vA - vAn;                           // A's velocity tangent to the normal

            Vector3 vBn = Vector3.Dot(vB, normal) * normal;

            // Apply the 1D formula only to the normal components. At equal mass the general
            // formula's coefficients collapse to 0 and 1, so the two velocities swap along the normal.
            Vector3 vAnf = vBn;

            return vAnf + vAt;
        }

        private bool IsMovingAway(Vector3 vA, Vector3 vB, Vector3 normal)
        {
            // Skip resolution if the objects are already separating or stationary
            float closingSpeed = Vector3.Dot(vA - vB, normal);
            if (closingSpeed <= 0)
                return true;

            return false;
        }
    }
}
