using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Class responsible for managing user input for VR devices, 
/// such as buttons on the left and right hands.
/// Implements the Singleton pattern to ensure only one instance exists.
/// </summary>
public class InputManager : MonoBehaviour
{
    /// <summary>
    /// Singleton instance of the InputManager class.
    /// </summary>
    public static InputManager Instance; 

    /// <summary>
    /// Reference to the left hand input device.
    /// </summary>
    private InputDevice leftHandDevice;

    /// <summary>
    /// Reference to the right hand input device.
    /// </summary>
    private InputDevice rightHandDevice;

    private bool _leftButtonAPressed;

    /// <summary>
    /// Gets a value indicating whether the primary button (A) on the left hand is pressed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the primary button (A) on the left hand is pressed; otherwise, <c>false</c>.
    /// </value>
    public bool LeftButtonAPressed
    {
        get { return _leftButtonAPressed; }
        private set { _leftButtonAPressed = value; }
    }

    private bool _rightButtonAPressed;

    /// <summary>
    /// Gets a value indicating whether the A button on the right controller is pressed.
    /// </summary>
    /// <value>
    /// <c>true</c> if the A button on the right controller is pressed; otherwise, <c>false</c>.
    /// </value>
    public bool RightButtonAPressed
    {
        get { return _rightButtonAPressed; }
        private set { _rightButtonAPressed = value; }
    }

    /// <summary>
    /// Called when the object is initialized. Sets up the Singleton instance and input devices.
    /// </summary>
    void Awake()
    {
        // Ensure there is only one instance of the Singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Optional: persists from one scene to another
        }
        else
        {
            Destroy(gameObject); // Destroys the object if another instance exists
        }
    }

    /// <summary>
    /// Called at the start. Initializes the input devices.
    /// </summary>
    void Start()
    {
        InitializeDevices();
    }

    /// <summary>
    /// Initializes the input devices for the left and right hands.
    /// </summary>
    void InitializeDevices()
    {
        List<InputDevice> devices = new List<InputDevice>();

        // Initialization for the left hand
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, devices);
        if (devices.Count > 0)
        {
            leftHandDevice = devices[0];
            Debug.LogWarning("Left hand device found: " + leftHandDevice.name);
        }

        devices.Clear();

        // Initialization for the right hand
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
        {
            rightHandDevice = devices[0];
            Debug.LogWarning("Right hand device found: " + rightHandDevice.name);
        }
    }

    /// <summary>
    /// Called every frame. Checks the button states and re-initializes devices if necessary.
    /// </summary>
    void Update()
    {
        // Retry initialization if necessary
        if (!leftHandDevice.isValid || !rightHandDevice.isValid)
        {
            InitializeDevices();
        }

        // Read the button state for the left hand
        if (leftHandDevice.isValid)
        {
            bool leftPressed = false;
            if (leftHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out leftPressed))
            {
                LeftButtonAPressed = leftPressed;
            }
        }

        // Read the button state for the right hand
        if (rightHandDevice.isValid)
        {
            bool rightPressed = false;
            if (rightHandDevice.TryGetFeatureValue(CommonUsages.primaryButton, out rightPressed))
            {
                RightButtonAPressed = rightPressed;
            }
        }
    }
}
