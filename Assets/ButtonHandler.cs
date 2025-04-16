using UnityEngine;
using UnityEngine.UI;

public class ButtonHandler : MonoBehaviour
{
    /// <summary>
    /// A reference to the button that triggers the node options toggle.
    /// </summary>
    public Button buttonNodeOption;  // Reference to the button

    /// <summary>
    /// A reference to the GameObject representing the node options plane.
    /// </summary>
    public GameObject nodeOption;    // Reference to the plane

    /// <summary>
    /// Called when the script is first initialized.
    /// This method assigns the ToggleNodeOption method to the button's onClick event.
    /// It also ensures that the node option plane is initially disabled.
    /// </summary>
    /// <remarks>
    /// The <c>buttonNodeOption</c> should be assigned via the Unity Inspector to link the button.
    /// The <c>nodeOption</c> should be assigned to the GameObject that you want to show or hide when the button is clicked.
    /// </remarks>
    void Start()
    {
        // Assign the method to the button's onClick event
        buttonNodeOption.onClick.AddListener(ToggleNodeOption);
        
        // Ensure the plane is disabled at the start
        nodeOption.SetActive(false);
    }

    /// <summary>
    /// Toggles the visibility of the node options plane.
    /// This method is called when the button is clicked.
    /// </summary>
    /// <remarks>
    /// This method activates or deactivates the <c>nodeOption</c> GameObject based on its current state.
    /// If it is active, it will be deactivated, and vice versa.
    /// </remarks>
    void ToggleNodeOption()
    {
        // Toggle the plane's visibility
        nodeOption.SetActive(!nodeOption.activeSelf);
    }
}
