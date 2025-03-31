using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This script sits in the select mode menu screen, particularly when selecting the specific modes (Intersection, EDSA, etc.)
/// It saves the player's condition choices into the specialConditionsInitializer, which will transcend/survive the scene switching via DontDestroyOnLoad.
/// </summary>
public class SpecialConditionsSelector : MonoBehaviour
{
    private SpecialConditionsInitializer specialConditionsInitializer;
    public string NextSceneSelected { get; set; }

    private void Start()
    {
        specialConditionsInitializer = SpecialConditionsInitializer.Instance;
    }

    public void StartSelectedScene()
    {
        nextScene.Instance.LoadScene(NextSceneSelected);
    }

    public void ActivateNightCondition(Button button)
    {
        specialConditionsInitializer.specialConditionsInvolved["Night"] = !specialConditionsInitializer.specialConditionsInvolved["Night"];
        ActivateButton(button);
        Debug.Log("Night = " + specialConditionsInitializer.specialConditionsInvolved["Night"]);
    }

    public void ActivateRainCondition(Button button)
    {
        specialConditionsInitializer.specialConditionsInvolved["Rain"] = !specialConditionsInitializer.specialConditionsInvolved["Rain"];
        ActivateButton(button);
        Debug.Log("Rain = " + specialConditionsInitializer.specialConditionsInvolved["Rain"]);
    }

    public void ActivateFogCondition(Button button)
    {
        specialConditionsInitializer.specialConditionsInvolved["Fog"] = !specialConditionsInitializer.specialConditionsInvolved["Fog"];
        ActivateButton(button);
        Debug.Log("Fog = " + specialConditionsInitializer.specialConditionsInvolved["Fog"]);
    }

    public void ActivateButton(Button button)
    {
        Image img = button.GetComponent<Image>();
        img.color = img.color == Color.white ? Color.gray : Color.white; // darken (white is false, gray is true)
    }
}
