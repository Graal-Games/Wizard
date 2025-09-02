using System; 
using Unity.Netcode;

// Your enum is fine
public enum EffectType
{
    Slow,
    Stun,
    Burn,
    Shield
}

// 1. Add IEquatable<StatusEffect> to the struct definition
public struct StatusEffect : INetworkSerializable, IEquatable<StatusEffect>
{
    public EffectType Type;
    public float Duration;
    public float DamagePerSecond;
    public ulong AttackerId;

    // Your NetworkSerialize method is correct and stays the same
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref Duration);
        serializer.SerializeValue(ref DamagePerSecond);
        serializer.SerializeValue(ref AttackerId);
    }

    // 2. Add the required Equals method
    public bool Equals(StatusEffect other)
    {
        // This method just checks if every field is the same in both structs.
        return Type == other.Type &&
               Duration == other.Duration &&
               DamagePerSecond == other.DamagePerSecond &&
               AttackerId == other.AttackerId;
    }
}