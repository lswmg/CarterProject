using Unity.Netcode;
using UnityEngine;

namespace CarterProject.Player
{
public class FlashlightSystem : NetworkBehaviour
{
[Header("Flashlight")]
[SerializeField] private Light flashlight;

[Header("Battery")]
[SerializeField] private float maximumBattery = 100f;
[SerializeField] private float drainPerSecond = 2f;
[SerializeField] private float rechargePerSecond = 5f;

public NetworkVariable<float> Battery = new(
100f,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);

public NetworkVariable<bool> IsOn = new(
false,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server);

private void Update()
{
if (IsServer && IsSpawned)
{
UpdateBattery();
}

if (flashlight != null)
{
flashlight.enabled = IsOn.Value;
}
}

private void UpdateBattery()
{
if (IsOn.Value)
{
Battery.Value = Mathf.Max(
0f,
Battery.Value -
drainPerSecond * Time.deltaTime);

if (Battery.Value <= 0f)
IsOn.Value = false;
}
}

public void MobileFlashlightButton()
{
if (!IsOwner || !IsSpawned)
return;

ToggleFlashlightRpc();
}

[Rpc(SendTo.Server)]
private void ToggleFlashlightRpc(
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (Battery.Value <= 0f && !IsOn.Value)
return;

IsOn.Value = !IsOn.Value;
}

[Rpc(SendTo.Server)]
public void RechargeBatteryRpc(
float amount,
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (amount <= 0f || amount > 100f)
return;

Battery.Value = Mathf.Min(
maximumBattery,
Battery.Value + amount);
}
}
}
