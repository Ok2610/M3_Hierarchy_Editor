using UnityEngine;
using TMPro;

public class UpdateText : MonoBehaviour
{
    /// <summary>
    /// The TextMeshProUGUI component used to display the text.
    /// </summary>
    [SerializeField] private TextMeshProUGUI textComponent;  // Make this variable public

    /// <summary>
    /// Called when the script is first initialized.
    /// This method checks all child objects of the current GameObject to find one with the name "text" 
    /// and attempts to assign the TextMeshProUGUI component from it to <c>textComponent</c>.
    /// If no matching child is found, an error is logged.
    /// </summary>
    /// <remarks>
    /// The method assumes that there is a child object named "text" that has a TextMeshProUGUI component attached.
    /// If no such child is found, it will output an error message in the console.
    /// </remarks>
    void Start()
    {
        // Check all children
        foreach (Transform child in transform)
        {
            if (child.name == "text")
            {
                textComponent = child.GetComponent<TextMeshProUGUI>();
                if (textComponent == null)
                {
                    Debug.LogError("The child 'text' does not have a TextMeshProUGUI component!");
                }
                break;
            }
        }
        if (textComponent == null)
        {
            Debug.LogError("The 'text' child was not found!");
        }
    }

    /// <summary>
    /// Updates the text displayed by the TextMeshProUGUI component.
    /// This method assigns the provided <paramref name=""newText""/> value to the textComponent's text.
    /// If the TextMeshProUGUI component is not found, an error message is logged.
    /// </summary>
    /// <param name=""newText"">The new text that will replace the current text.</param>
    /// <remarks>
    /// This method will only update the text if <c>textComponent</c> is valid (not null).
    /// If <c>textComponent</c> is null, an error is logged to inform the developer.
    /// </remarks>
    public void UpdateTextContent(string newText)
    {
        if (textComponent != null)
        {
            textComponent.text = newText;
        }
        else
        {
            Debug.LogError("The TextMeshProUGUI component was not found!");
        }
    }
}