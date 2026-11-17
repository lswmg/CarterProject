using Unity.Netcode;
using UnityEngine;

namespace CarterProject.Player
{
public class SprintSystem : NetworkBehaviour
{
[Header("Stamina")]
[SerializeField] private float maxStamina = 100f;
[SerializeField] private float drainRate = 20f;
[SerializeField] private float recoveryRate = 15f;

public NetworkVariable<float> Stamina = new(
100f,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server
);

private bool sprintRequested;

public bool CanSprint =>
Stamina.Value > 5f;

public void SetSprint(bool active)
{
if (IsOwner)
sprintRequested = active;
}

private void Update()
{
if (!IsServer || !IsSpawned)
return;

if (sprintRequested && Stamina.Value > 0f)
{
Stamina.Value = Mathf.Max(
0f,
Stamina.Value - drainRate * Time.deltaTime
);
}
else
{
Stamina.Value = Mathf.Min(
maxStamina,
Stamina.Value + recoveryRate * Time.deltaTime
);
}
}
}
}
