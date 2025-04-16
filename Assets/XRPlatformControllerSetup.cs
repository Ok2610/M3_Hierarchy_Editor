using UnityEngine;
using UnityEngine.XR.Management;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
#endif

/// <summary>
/// Manages the setup of the VR controller models based on the active XR platform.
/// This script enables or disables specific controller GameObjects depending on the XR loader being used.
/// </summary>
///
/// <remarks>
/// This class is designed to work with Unity's XR system and specifically handles the Oculus VR platform.
/// When the Oculus loader is active, it disables the default controller models and enables the Oculus-specific ones.
/// </remarks>
internal class XRPlatformControllerSetup : MonoBehaviour
{
    [SerializeField]
    GameObject m_LeftController;

    [SerializeField]
    GameObject m_RightController;
    
    [SerializeField]
    GameObject m_LeftControllerOculusPackage;

    [SerializeField]
    GameObject m_RightControllerOculusPackage;

    /// <summary>
    /// Initializes the controller setup based on the active XR platform.
    /// If the Oculus XR loader is active, this function disables the default controllers and enables the Oculus-specific controllers.
    /// </summary>
    /// <remarks>
    /// The function checks the active XR loaders, and if the "Oculus Loader" is found, it disables the default
    /// left and right controllers and activates the corresponding Oculus controllers.
    /// </remarks>
    void Start()
    {
#if UNITY_EDITOR
        var loaders = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone).Manager.activeLoaders;
#else
        var loaders = XRGeneralSettings.Instance.Manager.activeLoaders;
#endif
        
        // Iterate through the active loaders to check if the Oculus loader is active
        foreach (var loader in loaders)
        {
            if (loader.name.Equals("Oculus Loader"))
            {
                // Disable the default controllers and enable the Oculus controllers
                m_RightController.SetActive(false);
                m_LeftController.SetActive(false);
                m_RightControllerOculusPackage.SetActive(true);
                m_LeftControllerOculusPackage.SetActive(true);
            }
        }
    }
}