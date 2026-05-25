using UnityEngine;

namespace MultiClaw
{

    public class MultiClaw_BranchTypeInitiative : MonoBehaviour
    {

        [Tooltip("The required branch type for this object to remain active.")]
        public BranchType requiredType = BranchType.Showcase;

        [Tooltip("If true, disables the GameObject instead of destroying it.")]
        public bool disableInsteadOfDestroy = false;

        void Awake()
        {
            if (!Branch.Is(requiredType))
            {
                if (disableInsteadOfDestroy)
                    gameObject.SetActive(false);
                else
                    Destroy(gameObject);
            }
        }

    }

}