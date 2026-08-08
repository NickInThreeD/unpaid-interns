using Unity.Netcode;
using UnityEngine;

namespace Unity.MP_FPS
{
    /// <summary>Despawns a networked effect after <see cref="Lifetime"/> seconds. Server-authoritative
    /// so the despawn replicates to every client, unlike <see cref="DestroyAfterDelay"/> which is
    /// purely local.</summary>
    public class NetworkedTimedEffect : NetworkBehaviour
    {
        public float Lifetime = 3f;
        private float _time;

        private void Update()
        {
            if (!IsServer)
                return;

            _time += Time.deltaTime;

            if (_time >= Lifetime)
            {
                NetworkObject.Despawn();
            }
        }
    }
}
