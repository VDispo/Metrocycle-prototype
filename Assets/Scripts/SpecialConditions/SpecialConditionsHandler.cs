using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// This script sits inside a specific game mode scene. 
/// It relies on specialConditionsInitializer existing (on DontDestroyOnLoad screen). 
/// Upon getting this object, it takes its data and activates the respective condition handlers/initiators (e.g. RainConditionHandler starts all the rain start upon activation);.
/// </summary>
public class SpecialConditionsHandler : MonoBehaviour
{
    [Header("Specific Handlers")]
    [SerializeField] private NightConditionHandler nightConditionHandler;
    [SerializeField] private RainConditionHandler rainConditionHandler;
    [SerializeField] private FogConditionHandler fogConditionHandler;
    public Dictionary<ConditionHandler, bool> specialConditionsInvolved = new();

    [Header("Vehicle")]
    [SerializeField] private GameObject motorcycle;
    [SerializeField] private GameObject bicycle;
    private bool isMotorcycle;

    [Header("Skybox")]
    [SerializeField][Tooltip("in the same (hardcoded) order as specialConditionsInvolved Dictionary")] private Material[] skyboxMats;
    [SerializeField] private Skybox[] sideMirrorSkyboxes; // for motorcycle

    private void Start()
    {
        // Initialize each handler
        isMotorcycle = GameManager.Instance.getBikeType() == Metrocycle.BikeType.Motorcycle;
        rainConditionHandler.isMotorcycle = isMotorcycle;
        rainConditionHandler.activeVehicle = isMotorcycle ? motorcycle.transform : bicycle.transform;

        // Initialize dictionary containing active states of each condition
        specialConditionsInvolved.Add(nightConditionHandler, false);
        specialConditionsInvolved.Add(rainConditionHandler, false);
        specialConditionsInvolved.Add(fogConditionHandler, false);

        // Copy active states data from specialConditionsInitializer (if not found, continue game as normal aka Day Condition)
        SpecialConditionsInitializer starter = SpecialConditionsInitializer.Instance;
        if (starter)
        {
            ConditionHandler[] keys = new ConditionHandler[specialConditionsInvolved.Count]; 
            specialConditionsInvolved.Keys.CopyTo(keys, 0);
            foreach (ConditionHandler cond in keys)
            {
                foreach (string condName in starter.specialConditionsInvolved.Keys)
                {
                    if (cond.ConditionName == condName)
                        specialConditionsInvolved[cond] = starter.specialConditionsInvolved[condName];
                }
            }

            InitializeConditions();
            ChangeSkybox();
        }
        else Debug.Log("SpecialConditionsInitializer not found. Be sure you're playing from the title screen");
    }

    /// <summary>
    /// The first occuring element in the Dictionary is prioritized. 
    /// For example, the setup right now is: nightConditionHandler was added to the dictionary, then rain, then fog;
    /// if multiple are selected, the skybox to set will be that of the first appearing element in the order above. 
    /// </summary>
    private void ChangeSkybox()
    {
        bool[] values = specialConditionsInvolved.Values.ToArray();
        Material chosenSkyboxMat = null;
        for (int i = 0; i < specialConditionsInvolved.Count; i++)
        {
            if (values[i])
            {
                chosenSkyboxMat = skyboxMats[i];
                break;
            }
        }

        if (chosenSkyboxMat)
        {
            if (isMotorcycle)
                foreach (Skybox comp in sideMirrorSkyboxes)
                {
                    comp.material = chosenSkyboxMat;
                }

            GameObject mainCam = GameObject.FindGameObjectWithTag("MainCamera");
            mainCam.GetComponent<Skybox>().material = chosenSkyboxMat;
        }
    }

    //public List<string> GetConditionNames()
    //{
    //    List<string> conditionNamesOrdered = new(specialConditionsInvolved.Count);

    //    foreach(ConditionHandler handler in specialConditionsInvolved.Keys)
    //    {
    //        conditionNamesOrdered.Add(handler.ConditionName);
    //    }

    //    return conditionNamesOrdered;
    //}

    public void InitializeConditions()
    {
        foreach (var entry in specialConditionsInvolved)
        {
            entry.Key.gameObject.SetActive(entry.Value);
        }
    }
}