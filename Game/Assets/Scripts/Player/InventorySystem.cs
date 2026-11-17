using Unity.Netcode;
using UnityEngine;

namespace CarterProject.Player
{
public enum ItemType : byte
{
None,
Medkit,
Battery,
Key,
Flashlight
}

public struct InventorySlot :
INetworkSerializable, System.IEquatable<InventorySlot>
{
public ItemType Item;
public int Amount;

public InventorySlot(ItemType item, int amount)
{
Item = item;
Amount = amount;
}

public void NetworkSerialize<T>(
BufferSerializer<T> serializer)
where T : IReaderWriter
{
serializer.SerializeValue(ref Item);
serializer.SerializeValue(ref Amount);
}

public bool Equals(InventorySlot other)
{
return Item == other.Item &&
Amount == other.Amount;
}
}

public class InventorySystem : NetworkBehaviour
{
private const int SlotCount = 8;

public NetworkList<InventorySlot> Slots;

private void Awake()
{
Slots = new NetworkList<InventorySlot>();
}

public override void OnNetworkSpawn()
{
if (IsServer && Slots.Count == 0)
{
for (int i = 0; i < SlotCount; i++)
{
Slots.Add(new InventorySlot(
ItemType.None, 0));
}
}
}

[Rpc(SendTo.Server)]
public void PickupItemRpc(
ItemType item,
int amount,
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (item == ItemType.None ||
!System.Enum.IsDefined(typeof(ItemType), item) ||
amount <= 0 || amount > 10)
return;

for (int i = 0; i < Slots.Count; i++)
{
if (Slots[i].Item == item)
{
InventorySlot slot = Slots[i];
slot.Amount += amount;
Slots[i] = slot;
return;
}
}

for (int i = 0; i < Slots.Count; i++)
{
if (Slots[i].Item == ItemType.None)
{
Slots[i] = new InventorySlot(item, amount);
return;
}
}
}

[Rpc(SendTo.Server)]
public void RemoveItemRpc(
ItemType item,
int amount,
RpcParams rpcParams = default)
{
if (rpcParams.Receive.SenderClientId != OwnerClientId)
return;

if (amount <= 0 || amount > 10)
return;

for (int i = 0; i < Slots.Count; i++)
{
if (Slots[i].Item != item)
continue;

InventorySlot slot = Slots[i];

if (slot.Amount < amount)
return;

slot.Amount -= amount;

if (slot.Amount == 0)
slot.Item = ItemType.None;

Slots[i] = slot;
return;
}
}

public bool HasItem(ItemType item)
{
foreach (InventorySlot slot in Slots)
{
if (slot.Item == item && slot.Amount > 0)
return true;
}

return false;
}
}
}
