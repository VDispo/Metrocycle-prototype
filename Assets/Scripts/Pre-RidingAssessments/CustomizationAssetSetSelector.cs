using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// <em>
/// This is part of the UI prefab that is auto generated by <see cref="CharacterCustomizationHandler"/>'s array of <see cref="CustomizationAssetSetSO"/>.
/// </em>
/// <br/><br/>
/// This is a selector of which asset of the set to use (set defined by the referenced <see cref="CustomizationAssetSetSO"/>).
/// <br/><br/>
/// The selection is saved into <see cref="CustomizationAssetsSelected"/> to survive the scene change.
/// NOTE that the assetsSelected instantiated here are not the intances that survive through and make it to gameplay. 
/// Instead, <see cref="CustomizationAssetsSelected"/> saves the indeces of the assetsSelected selected and recreates them during scene change.
/// </summary>
public class CustomizationAssetSetSelector : MonoBehaviour
{
    [HideInInspector] public CustomizationAssetSetSO customizationAssetSetSO;

    [Header("Buttons")]
    [SerializeField] private Button leftSelectButton;
    [SerializeField] private Button rightSelectButton;

    [Header("Selection")]
    [SerializeField] private TextMeshProUGUI selectedText;
    [HideInInspector] public int selectedIdx = 0;
    [HideInInspector] public bool selectedValid = false;

    /// <summary> Left or Right / Back or Next </summary>
    public enum SelectDir  { Back = -1, Next = 1 }

    public void Initialize(CustomizationAssetSetSO customizationAssetSetSO)
    {
        this.customizationAssetSetSO = customizationAssetSetSO;
        leftSelectButton.onClick.AddListener(() => ScrollThroughSelector(SelectDir.Back));
        rightSelectButton.onClick.AddListener(() => ScrollThroughSelector(SelectDir.Next));
        UpdateSelection();
    }

    public void ScrollThroughSelector(SelectDir dir)
    {
        static int modulo(int a, int b) 
        { 
            int c = a % b; // remainder function
            return c < 0 ? c + b : c; // offset the remainder if negative
        };

        /// move next or back (wrapping)
        selectedIdx = modulo(selectedIdx + (int)dir, customizationAssetSetSO.choicesPrefabsWithPassing.Count);

        /// switch selection
        UpdateSelection();
    }

    // [possible TODO: destroy and instantiate are performance heavy BUT this soln is simple
    // and it only happens in this customization screen so not much problem,
    // but could be optimized by instantiating everything at the start]
    private void UpdateSelection()
    {
        // get asset
        GameObject newAsset = customizationAssetSetSO.choicesPrefabsWithPassing.Keys.ToArray()[selectedIdx];
        
        // check if valid
        selectedValid = customizationAssetSetSO.choicesPrefabsWithPassing[newAsset];

        // display name
        selectedText.text = newAsset.name;
        
        // destroy prev asset
        Destroy(CustomizationAssetsTransformParents.Instance.assetsSelected[customizationAssetSetSO.type]);
        
        // instantiate new asset
        CustomizationAssetsTransformParents.Instance.assetsSelected[customizationAssetSetSO.type] =
            Instantiate(newAsset, parent: CustomizationAssetsTransformParents.Instance.parentTransformOfAssets[customizationAssetSetSO.type]);
    }
}
