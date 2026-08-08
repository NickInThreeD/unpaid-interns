using Unity.Mathematics;

/// <summary>
/// Per-frame movement/look input for the local player, sampled directly from the new Input System
/// each frame and sent to the server. Replaces the ECS-authored <c>PlayerInput</c>/<c>ClientInput</c>
/// components (and their ghost-tick command-stream plumbing) deleted with Netcode for Entities.
/// </summary>
public struct PlayerInput : Unity.Netcode.INetworkSerializable
{
    public float2 MoveInput;
    public float2 LookYawPitchDegrees;
    public bool Jump;

    public void NetworkSerialize<T>(Unity.Netcode.BufferSerializer<T> serializer) where T : Unity.Netcode.IReaderWriter
    {
        serializer.SerializeValue(ref MoveInput);
        serializer.SerializeValue(ref LookYawPitchDegrees);
        serializer.SerializeValue(ref Jump);
    }
}
