using Unity.Netcode;
using UnityEngine;

namespace CarterProject.Player
{
public class CarterPlayer : NetworkBehaviour
{
public NetworkVariable<float> Health = new(
100f,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);

public NetworkVariable<float> Stamina = new(
100f,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);

public NetworkVariable<float> Battery = new(
100f,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);

public NetworkVariable<bool> IsDowned = new(
false,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);

public NetworkVariable<bool> FlashlightEnabled = new(
false,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);

private void Update()
{
if (!IsServer || !IsSpawned || IsDowned.Value)
return;

// Flashlight battery consumption
if (FlashlightEnabled.Value)
{
Battery.Value = Mathf.Max(
0f,
Battery.Value - 2f * Time.deltaTime
);

if (Battery.Value <= 0f)
FlashlightEnabled.Value = false;
}

// Stamina recovery
Stamina.Value = Mathf.Min(
100f,
Stamina.Value + 15f * Time.deltaTime
);
}

[Rpc(SendTo.Server)]
public void ToggleFlashlightRpc(
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (IsDowned.Value)
return;

if (Battery.Value <= 0f &&
!FlashlightEnabled.Value)
return;

FlashlightEnabled.Value =
!FlashlightEnabled.Value;
}

public void ApplyDamageServer(float amount)
{
if (!IsServer || amount <= 0f || IsDowned.Value)
return;

Health.Value = Mathf.Max(
0f,
Health.Value - amount
);

if (Health.Value <= 0f)
{
IsDowned.Value = true;
FlashlightEnabled.Value = false;
}
}

public void ReviveServer()
{
if (!IsServer || !IsDowned.Value)
return;

Health.Value = 35f;
IsDowned.Value = false;
}
}
}
