using Unity.Netcode;
using UnityEngine;

namespace CarterProject.Player
{
public class HidingSystem : NetworkBehaviour
{
public NetworkVariable<bool> IsHiding = new(
false,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server
);

private Transform currentHidingSpot;
private Vector3 previousPosition;

public void RequestHide(Transform hidingSpot)
{
if (!IsOwner || !IsSpawned || hidingSpot == null)
return;

EnterHidingRpc(hidingSpot.position);
}

public void RequestExit()
{
if (!IsOwner || !IsSpawned)
return;

ExitHidingRpc();
}

[Rpc(SendTo.Server)]
private void EnterHidingRpc(
Vector3 spotPosition,
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (IsHiding.Value)
return;

if (Vector3.Distance(
transform.position, spotPosition) > 3f)
return;

previousPosition = transform.position;

transform.position = spotPosition;
IsHiding.Value = true;
}

[Rpc(SendTo.Server)]
private void ExitHidingRpc(
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (!IsHiding.Value)
return;

transform.position =
previousPosition + transform.right * 1.5f;

IsHiding.Value = false;
}
}
}
