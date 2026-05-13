using UnityEngine;

namespace MultiClaw.Core
{

    public class MultiClaw_VersionTypeInitiative : MonoBehaviour
    {

        [Tooltip("The required version type for this object to remain active.")]
        public VersionType requiredType = VersionType.Showcase;

        [Tooltip("If true, disables the GameObject instead of destroying it.")]
        public bool disableInsteadOfDestroy = false;

        void Awake()
        {
            if (!Version.IsType(requiredType))
            {
                if (disableInsteadOfDestroy)
                    gameObject.SetActive(false);
                else
                    Destroy(gameObject);
            }
        }

    }

}