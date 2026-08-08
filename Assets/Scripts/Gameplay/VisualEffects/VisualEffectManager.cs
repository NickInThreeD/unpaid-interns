using System;
using UnityEngine;

namespace Unity.MP_FPS
{
    /// <summary>
    /// Local VFX spawning helper. The old ghost-RPC broadcast (so a remote player's muzzle flash was
    /// visible to everyone) depended on <c>PlayerGhostManager</c> and the ghost-bridge RPC bus, both
    /// deleted with Netcode for Entities. Nothing calls into weapon firing yet — that networked
    /// broadcast needs to be rebuilt once a player registry and a weapon-firing system exist.
    /// </summary>
    public class VisualEffectManager : MonoBehaviour
    {
        public static VisualEffectManager Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
        }

        public async void SpawnMuzzleFlash(Transform spawnPoint, uint weaponId)
        {
            try
            {
                if (spawnPoint == null)
                {
                    Debug.Log("Cannot spawn muzzle flash: spawn point is null");
                    return;
                }

                var weaponData = WeaponManager.Instance.WeaponRegistry.GetWeaponData(weaponId);
                if (weaponData == null)
                {
                    Debug.Log("Cannot spawn muzzle flash: weapon data is null");
                    return;
                }

                try
                {
                    var vfxInstance = await weaponData.MuzzleFlashVfxPrefab.InstantiateAsync(
                        spawnPoint.position, spawnPoint.rotation, spawnPoint).Task;
                    if (vfxInstance == null)
                    {
                        Debug.LogWarning("Cannot spawn muzzle flash: vfx instance is null");
                        return;
                    }

                    vfxInstance.AddComponent<DestroyAfterDelay>().Lifetime = 0.5f;

                    vfxInstance.SetActive(true);

                    GameManager.Instance.SoundSystem.CreateEmitter(weaponData.WeaponFireSfx, spawnPoint);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to spawn muzzle flash: {e.Message}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error in SpawnMuzzleFlash: " + e.Message);
            }
        }
    }
}
