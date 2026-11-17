using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using CarterProject.Player;

namespace CarterProject.Horror.AI
{
public enum MonsterState : byte
{
Patrol,
Investigate,
Chase,
Search,
Attack
}

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterAI : NetworkBehaviour
{
[Header("Movement")]
[SerializeField] private NavMeshAgent agent;
[SerializeField] private Transform[] patrolPoints;
[SerializeField] private float patrolSpeed = 2f;
[SerializeField] private float chaseSpeed = 5f;
[SerializeField] private float searchSpeed = 3f;

[Header("Detection")]
[SerializeField] private Transform eyes;
[SerializeField] private float visionRange = 18f;
[SerializeField] private float fieldOfView = 110f;
[SerializeField] private float searchDuration = 8f;
[SerializeField] private LayerMask obstructionMask = ~0;

[Header("Combat")]
[SerializeField] private float attackRange = 1.8f;
[SerializeField] private float attackDamage = 20f;
[SerializeField] private float attackCooldown = 1.5f;

[Header("Investigation")]
[SerializeField] private float hearingRange = 12f;
[SerializeField] private float investigationDuration = 5f;

public NetworkVariable<MonsterState> State =
new(
MonsterState.Patrol,
NetworkVariableReadPermission.Everyone,
NetworkVariableWritePermission.Server
);

private CarterPlayer target;
private Vector3 lastKnownPosition;
private float searchTimer;
private float investigationTimer;
private float attackTimer;
private int patrolIndex;

private void Awake()
{
if (agent == null)
agent = GetComponent<NavMeshAgent>();
}

public override void OnNetworkSpawn()
{
if (!IsServer)
{
if (agent != null)
agent.enabled = false;

return;
}

if (agent == null || !agent.isOnNavMesh)
{
Debug.LogError(
"MonsterAI requires an enabled NavMeshAgent " +
"placed on a baked NavMesh."
);

enabled = false;
return;
}

agent.speed = patrolSpeed;
GoToNextPatrolPoint();
}

private void Update()
{
if (!IsServer || !IsSpawned || agent == null)
return;

attackTimer -= Time.deltaTime;

CarterPlayer visiblePlayer = FindVisiblePlayer();

if (visiblePlayer != null)
{
target = visiblePlayer;
lastKnownPosition = target.transform.position;
State.Value = MonsterState.Chase;
}

switch (State.Value)
{
case MonsterState.Patrol:
UpdatePatrol();
break;

case MonsterState.Investigate:
UpdateInvestigation();
break;

case MonsterState.Chase:
UpdateChase();
break;

case MonsterState.Search:
UpdateSearch();
break;

case MonsterState.Attack:
UpdateAttack();
break;
}
}

private CarterPlayer FindVisiblePlayer()
{
CarterPlayer[] players =
FindObjectsByType<CarterPlayer>(
FindObjectsSortMode.None
);

Vector3 origin = eyes != null
? eyes.position
: transform.position + Vector3.up * 1.5f;

CarterPlayer best = null;
float closestDistance = visionRange;

foreach (CarterPlayer player in players)
{
if (player == null ||
!player.IsSpawned ||
player.IsDowned.Value)
continue;

HidingSystem hiding =
player.GetComponent<HidingSystem>();

if (hiding != null && hiding.IsHiding.Value)
continue;

Vector3 destination =
player.transform.position + Vector3.up;

Vector3 direction = destination - origin;
float distance = direction.magnitude;

if (distance <= 0.1f ||
distance > visionRange)
continue;

float angle = Vector3.Angle(
transform.forward,
direction
);

if (angle > fieldOfView * 0.5f)
continue;

if (Physics.Raycast(
origin,
direction.normalized,
out RaycastHit hit,
distance,
obstructionMask,
QueryTriggerInteraction.Ignore))
{
CarterPlayer hitPlayer =
hit.collider.GetComponentInParent<CarterPlayer>();

if (hitPlayer != player)
continue;
}

if (distance < closestDistance)
{
closestDistance = distance;
best = player;
}
}

return best;
}

private void UpdatePatrol()
{
agent.speed = patrolSpeed;

if (patrolPoints == null ||
patrolPoints.Length == 0)
return;

if (!agent.pathPending &&
agent.remainingDistance < 0.5f)
{
GoToNextPatrolPoint();
}
}

private void GoToNextPatrolPoint()
{
if (patrolPoints == null ||
patrolPoints.Length == 0)
return;

Transform point = patrolPoints[patrolIndex];

if (point != null)
agent.SetDestination(point.position);

patrolIndex =
(patrolIndex + 1) % patrolPoints.Length;
}

private void UpdateInvestigation()
{
agent.speed = searchSpeed;
investigationTimer -= Time.deltaTime;

if (!agent.pathPending &&
agent.remainingDistance < 0.5f)
{
if (investigationTimer <= 0f)
{
BeginSearch();
}
}
}

private void UpdateChase()
{
if (target == null ||
!target.IsSpawned ||
target.IsDowned.Value)
{
target = null;
BeginSearch();
return;
}

HidingSystem hiding =
target.GetComponent<HidingSystem>();

if (hiding != null && hiding.IsHiding.Value)
{
target = null;
BeginSearch();
return;
}

agent.speed = chaseSpeed;

lastKnownPosition = target.transform.position;

float distance = Vector3.Distance(
transform.position,
lastKnownPosition
);

if (distance <= attackRange)
{
State.Value = MonsterState.Attack;
return;
}

agent.SetDestination(lastKnownPosition);
}

private void UpdateAttack()
{
if (target == null ||
!target.IsSpawned ||
target.IsDowned.Value)
{
target = null;
BeginSearch();
return;
}

float distance = Vector3.Distance(
transform.position,
target.transform.position
);

if (distance > attackRange + 0.4f)
{
State.Value = MonsterState.Chase;
return;
}

agent.ResetPath();

Vector3 direction =
target.transform.position - transform.position;

direction.y = 0f;

if (direction.sqrMagnitude > 0.01f)
{
transform.rotation = Quaternion.Slerp(
transform.rotation,
Quaternion.LookRotation(direction),
8f * Time.deltaTime
);
}

if (attackTimer <= 0f)
{
target.ApplyDamageServer(attackDamage);
attackTimer = attackCooldown;
}
}

private void BeginSearch()
{
State.Value = MonsterState.Search;
target = null;
searchTimer = searchDuration;

agent.speed = searchSpeed;
agent.SetDestination(lastKnownPosition);
}

private void UpdateSearch()
{
searchTimer -= Time.deltaTime;

if (!agent.pathPending &&
agent.remainingDistance < 0.5f)
{
if (searchTimer <= 0f)
{
State.Value = MonsterState.Patrol;
GoToNextPatrolPoint();
}
}
}

// Call this only from trusted server-side sound systems.
public void ReportNoiseServer(
Vector3 position,
float loudness)
{
if (!IsServer || !IsSpawned ||
loudness <= 0f)
return;

float effectiveRange =
hearingRange * Mathf.Clamp01(loudness);

if (Vector3.Distance(
transform.position,
position) > effectiveRange)
return;

if (State.Value == MonsterState.Chase ||
State.Value == MonsterState.Attack)
return;

lastKnownPosition = position;
target = null;

investigationTimer = investigationDuration;

State.Value = MonsterState.Investigate;

agent.speed = searchSpeed;
agent.SetDestination(position);
}
}
}

