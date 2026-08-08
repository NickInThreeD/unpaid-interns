using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MP_FPS
{
    /// <summary>
    /// In-game HUD shell. The health/ammo/reticle wiring this used to drive read straight from
    /// <c>PredictedPlayerGhost</c>, an ECS ghost component deleted with Netcode for Entities. No
    /// NetworkVariable-backed health/weapon state exists yet to replace it with, so only HUD
    /// visibility is wired up here; the rest is future work once that state exists.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InGameHUD : MonoBehaviour
    {
        private VisualElement m_RootElement;

        void OnEnable()
        {
            m_RootElement = GetComponent<UIDocument>().rootVisualElement;
        }

        void LateUpdate()
        {
            bool isInGame = GameSettings.Instance.GameState == GlobalGameState.InGame;
            if (m_RootElement.style.display != (isInGame ? DisplayStyle.Flex : DisplayStyle.None))
            {
                m_RootElement.style.display = isInGame ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }
    }
}
