using UnityEngine;
using UnityEngine.InputSystem;

namespace RollABallCoinCollector
{
    /// <summary>Camera-relative rolling controls using the Unity Input System.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private Rigidbody playerBody;
        [SerializeField] private Camera movementCamera;
        [SerializeField] private float acceleration = 24f;
        [SerializeField] private float maximumSpeed = 9f;
        [SerializeField] private float stoppingDrag = 7f;

        private void Awake()
        {
            if (playerBody == null)
                playerBody = GetComponent<Rigidbody>();

            if (movementCamera == null && Camera.main != null)
                movementCamera = Camera.main;
        }

        public void Configure(Rigidbody body, Camera cameraToUse)
        {
            playerBody = body;
            movementCamera = cameraToUse;
        }

        private void FixedUpdate()
        {
            if (playerBody == null || (GameManager.Instance != null && !GameManager.Instance.IsPlaying))
                return;

            Vector2 input = ReadMovementInput();
            Vector3 forward = movementCamera != null ? movementCamera.transform.forward : Vector3.forward;
            Vector3 right = movementCamera != null ? movementCamera.transform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 moveDirection = right * input.x + forward * input.y;
            if (moveDirection.sqrMagnitude > 1f)
                moveDirection.Normalize();

            if (moveDirection.sqrMagnitude > 0.001f)
                playerBody.AddForce(moveDirection * acceleration, ForceMode.Acceleration);
            else
            {
                Vector3 horizontalVelocity = new Vector3(playerBody.linearVelocity.x, 0f, playerBody.linearVelocity.z);
                playerBody.AddForce(-horizontalVelocity * stoppingDrag, ForceMode.Acceleration);
            }

            Vector3 horizontalSpeed = new Vector3(playerBody.linearVelocity.x, 0f, playerBody.linearVelocity.z);
            if (horizontalSpeed.sqrMagnitude > maximumSpeed * maximumSpeed)
            {
                Vector3 limited = horizontalSpeed.normalized * maximumSpeed;
                playerBody.linearVelocity = new Vector3(limited.x, playerBody.linearVelocity.y, limited.z);
            }
        }

        private static Vector2 ReadMovementInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return Vector2.zero;

            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontal -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontal += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed)
                vertical -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed)
                vertical += 1f;

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }
    }
}
