using UnityEngine;
using UnityEngine.UI;

public class GameplayRestriction : MonoBehaviour
{
    public ScenarioSceneStarter scenarioSceneStarter;
    public GameObject MotorcycleAdvancedSet;
    public GameObject BicycleAdvancedSet;

    public void CheckTutorialRequirement(string vehicle)
    {
        // Check if the player has completed the tutorial
        if (PlayerPrefs.GetInt($"{vehicle}_TutorialCompleted", 0) == 0)
        {
            if (vehicle == "Motorcycle")
            {
                // Start the tutorial scene for motorcycles
                scenarioSceneStarter.StartScene("Tutorial_Motorcycle");
            }
            else
            {
                // Start the tutorial scene for other vehicles
                scenarioSceneStarter.StartScene("Tutorial_Bicycle");
            }
        }

        if (PlayerPrefs.GetInt($"{vehicle}_BasicCompleted", 0) < 4)
        {
            Debug.Log("Basic tutorial not completed");
            if (vehicle == "Motorcycle")
            {
                MotorcycleAdvancedSet.GetComponent<Button>().interactable = false;
                // TextMeshProUGUI advancedText = MotorcycleAdvancedSet.GetComponentInChildren<TextMeshProUGUI>();
                // advancedText.text = "Complete Basic Scenarios First";
            }
            else
            {
                BicycleAdvancedSet.GetComponent<Button>().interactable = false;
                // TextMeshProUGUI advancedText = BicycleAdvancedSet.GetComponentInChildren<TextMeshProUGUI>();
                // advancedText.text = "Complete Basic Scenarios First";
            }
        }
    }
}
