using Unity.Netcode;
using UnityEngine;

namespace CarterProject.Player
{
public class ReviveSystem : NetworkBehaviour
{
[Header("Revival Settings")]
[SerializeField] private float reviveDuration = 5f;
[SerializeField] private float reviveDistance = 2.5f;

private CarterPlayer targetPlayer;
private NetworkObject targetObject;

private float reviveProgress;
private bool isReviving;

public float ReviveProgress =>
reviveDuration <= 0f
? 0f
: Mathf.Clamp01(reviveProgress / reviveDuration);

private void Update()
{
if (!IsServer || !IsSpawned || !isReviving)
return;

if (targetPlayer == null ||
targetObject == null ||
!targetObject.IsSpawned ||
!targetPlayer.IsDowned.Value)
{
StopReviving();
return;
}

float distance = Vector3.Distance(
transform.position,
targetObject.transform.position
);

if (distance > reviveDistance)
{
StopReviving();
return;
}

reviveProgress += Time.deltaTime;

if (reviveProgress >= reviveDuration)
{
targetPlayer.ReviveServer();
StopReviving();
}
}

public void BeginRevive(NetworkObject target)
{
if (!IsOwner || !IsSpawned || target == null)
return;

BeginReviveRpc(
new NetworkObjectReference(target)
);
}

public void CancelRevive()
{
if (!IsOwner || !IsSpawned)
return;

CancelReviveRpc();
}

[Rpc(SendTo.Server)]
private void BeginReviveRpc(
NetworkObjectReference targetReference,
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (isReviving || reviveDuration <= 0f)
return;

if (!targetReference.TryGet(
out NetworkObject target))
return;

if (target == NetworkObject)
return;

CarterPlayer player =
target.GetComponent<CarterPlayer>();

if (player == null || !player.IsDowned.Value)
return;

float distance = Vector3.Distance(
transform.position,
target.transform.position
);

if (distance > reviveDistance)
return;

targetObject = target;
targetPlayer = player;

reviveProgress = 0f;
isReviving = true;
}

[Rpc(SendTo.Server)]
private void CancelReviveRpc(
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

StopReviving();
}

private void StopReviving()
{
isReviving = false;
reviveProgress = 0f;

targetPlayer = null;
targetObject = null;
}
}
}
