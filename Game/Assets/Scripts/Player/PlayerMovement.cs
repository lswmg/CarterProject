using Unity.Netcode;
using UnityEngine;

namespace CarterProject.Player
{
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
[SerializeField] private float walkSpeed = 3f;
[SerializeField] private float sprintSpeed = 5.5f;
[SerializeField] private float gravity = -20f;
[SerializeField] private Transform cameraTransform;

private CharacterController controller;
private Vector2 movementInput;
private bool sprintRequested;
private float verticalVelocity;

private void Awake()
{
controller = GetComponent<CharacterController>();
}

public void SetMovementInput(Vector2 input)
{
if (IsOwner)
movementInput = Vector2.ClampMagnitude(input, 1f);
}

public void SetSprint(bool active)
{
if (IsOwner)
sprintRequested = active;
}

private void Update()
{
if (!IsOwner || !IsSpawned)
return;

Vector3 direction =
new Vector3(movementInput.x, 0f, movementInput.y);

if (cameraTransform != null)
{
direction = cameraTransform.forward * direction.z
+ cameraTransform.right * direction.x;
direction.y = 0f;
}

direction = Vector3.ClampMagnitude(direction, 1f);

float speed = sprintRequested ? sprintSpeed : walkSpeed;

if (direction.sqrMagnitude > 0.01f)
{
transform.rotation = Quaternion.Slerp(
transform.rotation,
Quaternion.LookRotation(direction),
12f * Time.deltaTime
);
}

if (controller.isGrounded && verticalVelocity < 0f)
verticalVelocity = -2f;

verticalVelocity += gravity * Time.deltaTime;

Vector3 velocity =
direction * speed + Vector3.up * verticalVelocity;

controller.Move(velocity * Time.deltaTime);
}
}
}
