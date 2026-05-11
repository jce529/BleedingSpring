using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays 3 water tier orbs above the player's head.
/// Synchronizes with PlayerWaterStats.OnWaterTierChanged.
/// </summary>
public class PlayerTierDisplay : MonoBehaviour
{
    [Header("Tier Orbs")]
    [SerializeField] private Image[] orbImages = new Image[3];

    [Header("Colors")]
    [SerializeField] private Color litColor = new Color(0.4f, 0.85f, 1f, 1f);
    [SerializeField] private Color dimColor = new Color(0.2f, 0.2f, 0.25f, 0.5f);

    private PlayerWaterStats _stats;

    private void Awake()
    {
        _stats = GetComponentInParent<PlayerWaterStats>();
    }

    private void Start()
    {
        if (_stats == null)
        {
            Debug.LogWarning("[PlayerTierDisplay] PlayerWaterStats not found in parent hierarchy.");
            return;
        }

        _stats.OnWaterTierChanged += HandleWaterTierChanged;
        
        // Initialize with current value
        HandleWaterTierChanged(_stats.WaterTier);
    }

    private void OnDestroy()
    {
        if (_stats != null)
            _stats.OnWaterTierChanged -= HandleWaterTierChanged;
    }

    private void HandleWaterTierChanged(int tier)
    {
        for (int i = 0; i < orbImages.Length; i++)
        {
            if (orbImages[i] != null)
                orbImages[i].color = (i < tier) ? litColor : dimColor;
        }
    }
}
