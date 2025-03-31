using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script sits in the mode select screen, along with SpecialConditionSelector.
/// This saves all the selected special conditions, and will be alive in the loading of the new scene.
/// </summary>
public class SpecialConditionsInitializer : MonoBehaviour
{
    public static SpecialConditionsInitializer Instance;

    public Dictionary<string, bool> specialConditionsInvolved = new();

    private void Awake()
    {
        if (Instance) Destroy(Instance.gameObject);
        Instance = this;
    }

    void Start()
    {
        DontDestroyOnLoad(gameObject);

        // Initialize dictionary
        specialConditionsInvolved.Add("Night", false);
        specialConditionsInvolved.Add("Rain", false);
        specialConditionsInvolved.Add("Fog", false);
    }
}
