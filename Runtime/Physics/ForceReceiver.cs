/*
 * Created By:      Ryan Carpenter
 * Date Created:    01/04/2025
 * Last Modified:   01/22/2025 (Ryan)
 * Notes:           Applies forces to an object
*/

using UnityEngine;

namespace RyansLibrary
{
    /// <summary>
    /// Returns the movement when the forces are applied to an object. Essetially
    /// makes the object have a rigidbody when they are not using Unity's physics system
    /// </summary>
    public class ForceReceiver : MonoBehaviour
    {
        [Header("Falling and Grounded")]
        [SerializeField] private float _gravityMultiplier = -9.81f;
        public float GravityMultiplier => _gravityMultiplier;
        // Highest velocity the object can reach before being stopped by air resistance
        [SerializeField] private float _terminalVelocity = 50f;
        [SerializeField] public bool HasGravity = true;

        [field: Header("Grounded Check")]
        [SerializeField] public bool EnableGroundCheck = true;
        [SerializeField] private float _groundRayFanAngleX = 45;
        [SerializeField] private float _groundRayCount = 5;
        [SerializeField] private float _groundFanCheckDistance = 0.1f;
        [SerializeField] private float _groundDownCheckDistance = 0.025f;

        [field: Header("Debugging")]
        [SerializeField] private bool _debug;
        [SerializeField] private bool _log = false;

        // Individual velocities of the object
        private float _velocityX = 0;
        public float VelocityX => _velocityX;
        private float _velocityY = 0;
        public float VelocityY => _velocityY;
        private float _velocityZ = 0;
        public float VelocityZ => _velocityZ;

        private Vector3 _impact = Vector3.zero;
        private Vector3 dampingVelocity = Vector3.zero;
        private float drag;

        public Vector3 Movement => _impact + Vector3.up * VelocityY;
        private bool _isGrounded = false;
        public bool IsGrounded => _isGrounded; // Character controllers ground check

        private Transform _transform;

        private void Awake()
        {
            _transform = GetComponent<Transform>();
        }

        private void Update()
        {
            if (_log) Debug.Log($"ForceReciever Movement: {Movement}");

            if (_log) Debug.Log("ForceReciever Impact: " + _impact);

            HandleForces();

            HandleGravity();

            _transform.position += new Vector3(_velocityX, _velocityY, _velocityZ) * Time.deltaTime;
        }

        public void AddForce(Vector3 force, float drag = 0.3f)
        {
            this.drag = drag;
            _impact += force;
        }

        private void HandleForces()
        {
            // Reduce any forces applied to the player a small bit every second
            _impact = Vector3.SmoothDamp(_impact, Vector3.zero, ref dampingVelocity, drag);
        }

        private void HandleGravity()
        {
            if (!HasGravity)
            {
                _velocityY = 0;
                return;
            }

            _isGrounded = CheckGrounded();
            if (_log) Debug.Log("IsGrounded: " + _isGrounded);

            // Conditionally Handle Gravity
            if (_isGrounded && (VelocityY <= 0))
                _velocityY = 0;                         // Does not have gravity
            else
            {
                if (Mathf.Abs(VelocityY) >= _terminalVelocity)   // If terminal velocity has been reached
                {
                    if (_log) Debug.Log("Terminal Velocity Reached");
                    return;
                }

                _velocityY += GravityMultiplier * Time.deltaTime;        // Calculate acceleration due to gravity 
            }
        }

        private bool CheckGrounded()
        {
            if (!EnableGroundCheck)
                return false;

            Vector3 rayOrigin = transform.position;
            RaycastHit hit;

            if (_debug) Debug.DrawRay(rayOrigin, Vector3.down * _groundDownCheckDistance, Color.red);
            // Check the first raycast
            if (Physics.Raycast(rayOrigin, Vector3.down, out hit, _groundDownCheckDistance))
                return true;

            float groundRayFanAngleY = 0;

            float segmentedFanAngle = _groundRayCount <= 0 ? 0 : (360 / _groundRayCount);

            // Check all fan raycasts 
            for (int i = 0; i < _groundRayCount; i++)
            {
                Vector3 rayDirection = Quaternion.Euler(_groundRayFanAngleX, groundRayFanAngleY, 0) * Vector3.down;
                rayDirection.Normalize();

                if (_debug) Debug.DrawRay(rayOrigin, rayDirection * _groundFanCheckDistance, Color.red);

                if (Physics.Raycast(rayOrigin, rayDirection, out hit, _groundFanCheckDistance))
                    return true;

                groundRayFanAngleY += segmentedFanAngle;
            }

            return false;
        }
    }
}
