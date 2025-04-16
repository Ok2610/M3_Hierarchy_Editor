using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections.Generic;
using TMPro;
using Newtonsoft.Json;
using System.Linq;
using UnityEngine.UI;
using System;
using System.Text;
using System.Text.RegularExpressions;
using Random = UnityEngine.Random;
using UnityEngine.InputSystem;
using UnityEngine.XR;

public class DisplayTags : MonoBehaviour
{
    public GameObject signPrefab; // Template of the nodes
    Vector3 scaleCorrector; // Scale of nodes's template
    private Camera mainCamera; // Used for mouse interactions
    private RaycastHit hit; // Used for mouse interactions
    private bool isDragging = false;
    private Vector3 offset; // Use for Dragging object
    private GameObject draggedObject; // Stores the object currently being moved

    // Dictionary to store parent-child relationships
    private Dictionary<GameObject, List<GameObject>> parentChildDictionary = new Dictionary<GameObject, List<GameObject>>();
    private Dictionary<(GameObject parent, GameObject child), LineRenderer> existingLines = new Dictionary<(GameObject, GameObject), LineRenderer>();
    private Dictionary<GameObject, NodeInfo> nodeToInfoDictionary = new Dictionary<GameObject, NodeInfo>();
    private Dictionary<int, int> nbNodeByDepth = new Dictionary<int, int>();
    private Dictionary<int, int> nbMaxNodeByDepth;
    // Dictionnaire pour stocker l'échelle locale originale de chaque enfant
    private Dictionary<Transform, Vector3> originalChildScales = new Dictionary<Transform, Vector3>();

    // For the panel option
    GameObject nodeOptionPanel;
    GameObject addNodePanel;
    GameObject DeleteNodePanel;
    GameObject DeleteBranchPanel;

    // semaphore
    bool semaphore = false;
    bool updateVisuel = false; 

    // For the scale of the screen and the nodes
    RectTransform screenRect;
    GameObject screenObject; // Screen
    Vector3 screenScale;
    float screenHeight;
    float nodeWidth;
    float xSpacing; // Define the space between the nodes of the same level
    GameObject touchedNode = null; // Gets the exact object that was touched
    TextMeshProUGUI tmpText; // Variable retrieving input from text fields
    int defaultNode = 0;
    int ScreenWidthInNodes; // Measurement of screen width in nodes
    float screenWidth;
    float screenInitialWidth;
    float scaleAdaptator;
    private UnityEngine.XR.InputDevice leftControllerDevice;
    private UnityEngine.XR.InputDevice rightControllerDevice;

    private XRNode leftControllerNode = XRNode.LeftHand;   // Contrôleur gauche
    private XRNode rightControllerNode = XRNode.RightHand; // Contrôleur droit

    private UnityEngine.XR.InputDevice currentControllerDevice;

    /// <summary>
    /// This method is called at the start of the object's lifecycle, during initialization.
    /// It initializes necessary variables related to the camera and the screen objects.
    /// </summary>
    /// <remarks>
    /// The method searches for the "Screen" object in the scene and initializes variables such as RectTransform and screen size.
    /// If the "Screen" object is not found, an error message is logged.
    /// </remarks>
    /// <exception cref="NullReferenceException">Thrown if the "Screen" object cannot be found in the scene and is critical for further operations.</exception>
    void Start()
    {
        mainCamera = Camera.main; // Allows the use of the mouse in Unity instead of controllers.

        screenObject = GameObject.Find("Screen");
        if (screenObject != null)
        {
            screenRect = screenObject.GetComponent<RectTransform>();
            screenScale = screenObject.transform.localScale;
            screenInitialWidth = screenRect.rect.width * screenScale.x;
        }
        else
        {
            Debug.LogError("The 'Screen' object was not found!");
        }
        leftControllerDevice = InputDevices.GetDeviceAtXRNode(leftControllerNode);
        rightControllerDevice = InputDevices.GetDeviceAtXRNode(rightControllerNode);
    }

    /// <summary>
    /// This method is called once per frame to update the game logic.
    /// It handles screen resizing via arrow keys, drag and drop functionality, and various node-related interactions (e.g., adding, deleting, showing nodes).
    /// </summary>
    /// <remarks>
    /// The method performs the following tasks:
    /// - Resizes the screen when the left or right arrow keys are pressed.
    /// - Handles drag-and-drop interactions, allowing the user to drag objects in the scene.
    /// - Manages interactions with nodes, such as displaying node options, adding nodes, and deleting nodes.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when a required component or object is not found (e.g., "InputField" or "Node").</exception>
    void Update()
    {  
        bool leftButtonAPressed = false;
        bool rightButtonAPressed = false;
        // Resizing the screen based on arrow key inputs
        screenScale = screenObject.transform.localScale;

        // Vérifie si le bouton A est pressé sur le contrôleur gauche
        if (leftControllerDevice.isValid)
        {
            leftControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out leftButtonAPressed);
        }

        // Vérifie si le bouton A est pressé sur le contrôleur droit
        if (rightControllerDevice.isValid)
        {
            rightControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.primaryButton, out rightButtonAPressed);
        }

        // Déterminer quel contrôleur est utilisé (gauche ou droit)
        if (leftButtonAPressed)
        {
            currentControllerDevice = leftControllerDevice;
        }
        else if (rightButtonAPressed)
        {
            currentControllerDevice = rightControllerDevice;
        }

        // DRAG & DROP + NODE DISPLAY OPTIONS
        if (InputManager.Instance != null && (Input.GetMouseButtonDown(0) || InputManager.Instance.LeftButtonAPressed || InputManager.Instance.RightButtonAPressed)) // Check if the mouse is clicked
        {
            Vector3 controllerPosition;
            Ray ray;
            if(Input.GetMouseButtonDown(0))
            {
                ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            }else{
                Quaternion controllerRotation;
                currentControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.devicePosition, out controllerPosition);
                currentControllerDevice.TryGetFeatureValue(UnityEngine.XR.CommonUsages.deviceRotation, out controllerRotation);

                Vector3 controllerForward = controllerRotation * Vector3.forward;

                ray = new Ray(controllerPosition, controllerForward);
            }

            if (Physics.Raycast(ray, out hit)) 
            {
                touchedNode = hit.collider.gameObject;
                
                // Check if the touched object is a valid "Node" or other interactive object
                if ((touchedNode.CompareTag("Node") && !touchedNode.CompareTag("DisplayNodeOption")) || (touchedNode.CompareTag("MoveScreen")))
                {
                    TryStartDragging();
                }
                // Show or hide node options based on the node touched
                else if (semaphore && !touchedNode.CompareTag("Node") && touchedNode.CompareTag("DisplayNodeOption"))
                {
                    nodeOptionPanel = touchedNode.transform.parent.Find("NodeOption")?.gameObject;
                    nodeOptionPanel.SetActive(!nodeOptionPanel.activeSelf);
                    semaphore = !semaphore;
                }
                // Handle add node button interaction
                else if (touchedNode.CompareTag("AddNodeButton"))
                {
                    if (DeleteNodePanel) DeleteNodePanel.SetActive(false);
                    if (DeleteBranchPanel) DeleteBranchPanel.SetActive(false);
                    addNodePanel = touchedNode.transform.Find("AddNode")?.gameObject;
                    addNodePanel.SetActive(!addNodePanel.activeSelf);
                }
                // Handle delete node button interaction
                else if (touchedNode.CompareTag("DeleteNodeButton"))
                {
                    if (addNodePanel) addNodePanel.SetActive(false);
                    if (DeleteBranchPanel) DeleteBranchPanel.SetActive(false);
                    DeleteNodePanel = touchedNode.transform.Find("DeleteNode")?.gameObject;
                    DeleteNodePanel.SetActive(!DeleteNodePanel.activeSelf);
                }
                // Handle delete branch button interaction
                else if (touchedNode.CompareTag("DeleteBranchButton"))
                {
                    if (addNodePanel) addNodePanel.SetActive(false);
                    if (DeleteNodePanel) DeleteNodePanel.SetActive(false);
                    DeleteBranchPanel = touchedNode.transform.Find("DeleteBranch")?.gameObject;
                    DeleteBranchPanel.SetActive(!DeleteBranchPanel.activeSelf);
                }
                // Show node based on input field content
                else if (touchedNode.CompareTag("ShowNodeButton"))
                {
                    GameObject inputField = touchedNode.transform.parent.Find("InputField")?.gameObject;
                    
                    if (inputField != null)
                    {
                        Transform textTransform = inputField.transform.Find("Text Area/Text");
                        TextMeshProUGUI tmpText = textTransform.GetComponent<TextMeshProUGUI>();
                        string textSaisi = tmpText.text.Replace("\u200B", "");
                        textSaisi = textSaisi.Trim();

                        if (int.TryParse(textSaisi, out int inputValueInt))
                        {
                            StartCoroutine(CallShowNodeAPI(inputValueInt));
                        }
                        else
                        {
                            Debug.Log($"The value '{textSaisi}' is not a valid number.");
                        }
                    }
                    else
                    {
                        Debug.LogError("No InputField component found on the object.");
                    }
                }
                // Show hierarchy based on input field content
                else if (touchedNode.CompareTag("ShowHierarchyButton"))
                {
                    GameObject inputField = touchedNode.transform.parent.Find("InputField")?.gameObject;
                    
                    if (inputField != null)
                    {
                        Transform textTransform = inputField.transform.Find("Text Area/Text");
                        TextMeshProUGUI tmpText = textTransform.GetComponent<TextMeshProUGUI>();
                        string textSaisi = tmpText.text.Replace("\u200B", "");
                        textSaisi = textSaisi.Trim();

                        if (int.TryParse(textSaisi, out int inputValueInt))
                        {
                            StartCoroutine(CallShowHierarchyAPI(inputValueInt));
                        }
                        else
                        {
                            Debug.Log($"The value '{textSaisi}' is not a valid number.");
                        }
                    }
                    else
                    {
                        Debug.LogError("No InputField component found on the object.");
                    }
                }
                // Add node logic
                else if (touchedNode.CompareTag("AddNode"))
                {
                    GameObject Node = touchedNode;

                    // Traverse up the hierarchy until the object with the "Node" tag is found
                    while (Node != null && !Node.CompareTag("Node")) 
                    {
                        Node = Node.transform.parent?.gameObject;
                    }

                    GameObject inputField = touchedNode.transform.parent.parent.Find("InputField")?.gameObject;
                    string nodeName = "";

                    if (inputField != null)
                    {
                        Transform textTransform = inputField.transform.Find("Text Area/Text");
                        TextMeshProUGUI tmpText = textTransform.GetComponent<TextMeshProUGUI>();
                        nodeName = tmpText.text.Replace("\u200B", "");
                        nodeName = nodeName.Trim();
                    }
                    else
                    {
                        Debug.LogError("No InputField component found on the object.");
                    }

                    if (!string.IsNullOrEmpty(nodeName))
                    {
                        StartCoroutine(AddNodeFromDatabase(nodeToInfoDictionary[Node].hierarchyId, nodeName, nodeToInfoDictionary[Node].tagTypeId, nodeToInfoDictionary[Node].tagSetId, nodeToInfoDictionary[Node].id));
                    }
                    else
                    {
                        Debug.LogWarning("Name empty");
                    }
                }
                // Delete node logic
                else if (touchedNode.CompareTag("DelNode"))
                {
                    GameObject Node = touchedNode;

                    // Traverse up the hierarchy until the object with the "Node" tag is found
                    while (Node != null && !Node.CompareTag("Node")) 
                    {
                        Node = Node.transform.parent?.gameObject;
                    }
                    StartCoroutine(DeleteNodeFromDatabase(nodeToInfoDictionary[Node].id));
                }
            }
        }
        // Handle drag-and-drop
        if (isDragging) // If dragging is in progress
        {
            DragObject();
        }

        // Stop dragging when mouse button is released
        if (Input.GetMouseButtonUp(0) || !InputManager.Instance.LeftButtonAPressed || !InputManager.Instance.RightButtonAPressed) // Stop dragging
        {
            StopDragging();
        }
    }

    /// <summary>
    /// Called when another collider enters the trigger zone of this object.
    /// Detects interactions with specific tagged objects to track the current node being touched.
    /// </summary>
    /// <param name=""other"">The collider that entered the trigger zone.</param>
    /// <remarks>
    /// - If the object has the tag "Node" (excluding "DisplayNodeOption") or "MoveScreen", it is considered as a touched node.
    /// - Updates the <c>touchedNode</c> reference to the currently collided object.
    /// </remarks>
    void OnTriggerEnter(Collider other)
    {
        if ((other.CompareTag("Node") && !other.CompareTag("DisplayNodeOption")) || other.CompareTag("MoveScreen"))
        {
            touchedNode = other.gameObject;
        }
    }

    /// <summary>
    /// Called when another collider exits the trigger zone of this object.
    /// Clears the reference to the previously touched node if it matches the exiting object.
    /// </summary>
    /// <param name=""other"">The collider that exited the trigger zone.</param>
    /// <remarks>
    /// - Ensures that <c>touchedNode</c> is only cleared if it matches the object that just exited the trigger.
    /// </remarks>
    void OnTriggerExit(Collider other)
    {
        if (touchedNode != null && other.gameObject == touchedNode)
        {
            touchedNode = null;
        }
    }

    /// <summary>
    /// Sends a request to the API to fetch data related to a specific node and its hierarchy.
    /// It clears existing data and destroys previously instantiated nodes before updating the visual with the new node data.
    /// </summary>
    /// <param name=""idNode"">The ID of the node for which data is being requested from the API.</param>
    /// <remarks>
    /// This method performs the following steps:
    /// - Clears all existing node-related data from dictionaries and destroys previously instantiated node GameObjects.
    /// - Sends a GET request to the API to fetch the hierarchy of the node with the specified ID.
    /// - If the request is successful, it parses the response and calls another coroutine to explore and process the nodes.
    /// - If there is an error in the API request, an error message is logged.
    /// </remarks>
    /// <exception cref=""UnityWebRequestException"">Thrown if there is an issue with the API request, such as connection issues or invalid response.</exception>
    IEnumerator CallShowNodeAPI(int idNode)
    {
        // Used to refresh the visual by adding or deleting nodes
        defaultNode = idNode;
        
        // RESET THE VISUAL
        // CLEAR ALL THE DICTIONARIES
        parentChildDictionary.Clear();
        existingLines.Clear();
        nodeToInfoDictionary.Clear();
        
        // DESTROY ALL PREVIOUSLY INSTALLED NODES
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Node");
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }

        // Construct the API URL with the specified node ID
        string url = "https://localhost:5001/api/node/" + idNode + "/tree";

        UnityWebRequest www = UnityWebRequest.Get(url);

        // Set the custom certificate handler before sending the request
        www.certificateHandler = new CustomCertificateHandler();

        // Wait for the request to complete
        yield return www.SendWebRequest();

        // Check if the request was successful
        if (www.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = www.downloadHandler.text;
            // If successful, start the process of exploring nodes
            StartCoroutine(ExploreNodes(jsonResponse));
        }
        else
        {
            // Log an error if the API request failed
            Debug.LogError("API error: " + www.error);
        }
    }

    /// <summary>
    /// Sends a request to the API to fetch a hierarchy of nodes based on the specified hierarchy ID.
    /// Clears the existing visual data, sends requests to retrieve node data, and processes the retrieved data to update the visual.
    /// </summary>
    /// <param name=""idHierarchy"">The ID of the hierarchy for which node data is being requested from the API.</param>
    /// <remarks>
    /// This method performs the following steps:
    /// - Clears all existing node-related data from dictionaries and destroys previously instantiated node GameObjects.
    /// - Sends a GET request to fetch the hierarchy associated with the specified ID.
    /// - For each node in the hierarchy, sends a second request to fetch the full node details and processes the response.
    /// - If the request is successful, it calls another coroutine to explore and process the nodes.
    /// - If any API request fails, an error message is logged for the specific request.
    /// </remarks>
    /// <exception cref=""UnityWebRequestException"">Thrown if there is an issue with the API request, such as connection issues or invalid response.</exception>
    IEnumerator CallShowHierarchyAPI(int idHierarchy)
    {
        // RESET THE VISUAL
        // CLEAR ALL THE DICTIONARIES
        parentChildDictionary.Clear();
        existingLines.Clear();
        nodeToInfoDictionary.Clear();

        // DESTROY ALL PREVIOUSLY INSTALLED NODES
        GameObject[] objects = GameObject.FindGameObjectsWithTag("Node");
        foreach (GameObject obj in objects)
        {
            Destroy(obj);
        }

        // Construct the API URL for the hierarchy data
        string url = "https://localhost:5001/api/node/" + idHierarchy + "/hierarchy";
        UnityWebRequest www = UnityWebRequest.Get(url);
        www.certificateHandler = new CustomCertificateHandler();

        // Send the request and wait for the result
        yield return www.SendWebRequest();

        // Check if the request was successful
        if (www.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = www.downloadHandler.text;

            // Deserialize the JSON response into a list of dictionaries
            List<Dictionary<string, int>> jsonList = JsonConvert.DeserializeObject<List<Dictionary<string, int>>>(jsonResponse);

            // Extract the "id" values from the JSON response into a list
            List<int> idList = jsonList.Select(node => node["id"]).ToList();

            // Iterate through the list of node IDs
            foreach (int id in idList)
            {
                // Build the URL for each node's tree data
                url = "https://localhost:5001/api/node/" + id + "/tree";
                UnityWebRequest request = UnityWebRequest.Get(url);
                
                // Send the request and wait for the result
                yield return request.SendWebRequest();

                // Check if the request for the node was successful
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonResponseHierarchy = request.downloadHandler.text;
                    // Call the function to explore and process the node data
                    StartCoroutine(ExploreNodes(jsonResponseHierarchy));
                }
                else
                {
                    // Log an error if the API request for the node fails
                    Debug.LogError($"API error for the ID {id}: {request.error}");
                }
            }
        }
        else
        {
            // Log an error if the initial API request fails
            Debug.LogError("API error: " + www.error);
        }
    }

    /// <summary>
    /// Explores the nodes hierarchy by creating and displaying nodes recursively based on the JSON data received.
    /// It positions the nodes on the screen and adjusts their scale and layout to fit within the visual bounds of the screen.
    /// </summary>
    /// <param name="jsonFile">A JSON string containing the node hierarchy to be displayed.</param>
    /// <remarks>
    /// This method performs the following steps:
    /// - Initializes the screen and node scale variables.
    /// - Creates the root node and attaches it to the screen at the appropriate position.
    /// - Recursively displays the child nodes of the root node by calling the `DisplayNodeChildren` function.
    /// - Calculates the width of the screen based on the number of nodes and adjusts the scaling of the screen accordingly.
    /// - Adjusts the position and scale of the nodes to ensure they fit within the screen, scaling them proportionally.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if the JSON file is null or empty.</exception>
    /// <exception cref="NullReferenceException">Thrown if any required game objects or components (like 'Screen') are not found in the scene.</exception>
    IEnumerator ExploreNodes(string jsonFile)
    {
        // Parse the JSON file into a Nodes object
        Nodes currentNode = JsonUtility.FromJson<Nodes>(jsonFile);

        // Initialize the screen and node scale variables
        ScreenWidthInNodes = 0;
        scaleAdaptator = 1.0f; 

        // Create the root node and position it at the center of the screen
        scaleCorrector = signPrefab.transform.localScale;

        float x = 0f;
        float y = 0f;
        float z = 0f;
        GameObject screenObject = GameObject.Find("Screen");
        if (screenObject != null)
        {
            Vector3 screenPosition = screenObject.transform.position;
            z = screenPosition.z;
        }
        else
        {
            Debug.LogError("The 'Screen' object was not found!");
        }

        Vector3 rootPosition = new Vector3(x, y, (screenHeight / 5));
        GameObject rootSign = CreateSign(currentNode, rootPosition, null);

        // Attach the root node to the screen
        rootSign.transform.SetParent(screenRect, false);

        // Reset local rotation to align with the screen
        rootSign.transform.localRotation = Quaternion.identity;

        // Initialize dictionaries for node counts by depth
        nbNodeByDepth.Clear();
        if (nbMaxNodeByDepth != null)
        {
            nbMaxNodeByDepth.Clear();
        }

        // Count the number of nodes by depth for positioning
        countNodeByDepth(currentNode, 0);
        nbMaxNodeByDepth = new Dictionary<int, int>(nbNodeByDepth);
        originalChildScales.Clear();
        parentChildDictionary[rootSign] = new List<GameObject>();
        originalChildScales[rootSign.transform] = rootSign.transform.localScale;

        // Recursively display the children of the root node
        DisplayNodeChildren(currentNode.children, 1, rootSign.transform, nodeToInfoDictionary[rootSign].id);

        // Calculate screen width based on node count and layout
        screenWidth = (ScreenWidthInNodes * nodeWidth * rootSign.GetComponent<RectTransform>().localScale.x / 1.5f) +
                    (ScreenWidthInNodes + 1) * xSpacing * rootSign.GetComponent<RectTransform>().localScale.x / 1.5f;
        
        //Debug.Log("ScreenWidthInNodes: " + ScreenWidthInNodes + " | nodeWidth: " + nodeWidth + " | rootSign.localScale.x: " + rootSign.GetComponent<RectTransform>().localScale.x);
        //Debug.LogWarning("part 1 : " + ScreenWidthInNodes * nodeWidth * rootSign.GetComponent<RectTransform>().localScale.x + 
        //                " part 2 : " + (ScreenWidthInNodes + 1) * xSpacing * rootSign.GetComponent<RectTransform>().localScale.x);

        // Adjust the screen scaling based on calculated width
        scaleAdaptator = screenWidth / screenInitialWidth;
        //Debug.Log("screenWidth : " + screenWidth + " | screenInitialWidth : " + screenInitialWidth);

        // Scale the screen object if necessary
        RectTransform screenRectTransform = screenObject.GetComponent<RectTransform>();
        if (screenRectTransform.localScale.x != 0.5f || screenRectTransform.localScale.y != 0.5f || screenRectTransform.localScale.z != 0.5f)
            screenRectTransform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        
        // Adjust the screen's scale
        screenRectTransform.localScale = new Vector3(screenRectTransform.localScale.x * scaleAdaptator, 
                                                    screenRectTransform.localScale.y, 
                                                    screenRectTransform.localScale.z);

        // Adjust the nodes' scale and position based on the screen scaling
        foreach (Transform child in screenObject.transform)
        {
            if (child.CompareTag("MoveScreen")) continue;

            child.localScale = new Vector3(
                child.localScale.x / scaleAdaptator,
                child.localScale.y,
                child.localScale.z
            );
            child.localPosition = new Vector3(
                child.localPosition.x / scaleAdaptator,
                child.localPosition.y,
                child.localPosition.z
            );
        }

        foreach (var entry in nodeToInfoDictionary)
        {
            // entry.Key est le GameObject (le nœud), entry.Value est le NodeInfo associé
            UpdateNodeConnections(entry.Key); 
        }
        yield break;
    }

    /// <summary>
    /// Displays the child nodes of a given parent node by creating new "sign" game objects for each child.
    /// This method is recursive and will display all descendants of the initial node, positioning them on the screen
    /// according to their depth in the hierarchy and adjusting the spacing between nodes based on the screen scale.
    /// </summary>
    /// <param name=""children"">The list of child nodes to display.</param>
    /// <param name=""depth"">The current depth level in the node hierarchy. Used for calculating node positioning.</param>
    /// <param name=""parentTransform"">The parent transform to attach the child nodes to. This determines their relative positioning.</param>
    /// <param name=""parentnodeId"">The ID of the parent node. This is used to store node information in dictionaries for later reference.</param>
    /// <remarks>
    /// This method performs the following steps:
    /// - Calculates the spacing between nodes based on the node's dimensions and the screen scale factor.
    /// - Creates a new game object for each child node and attaches it to the screen at the calculated position.
    /// - Creates a line between the parent and child nodes for visual connection.
    /// - Recursively calls itself to display the children of the current child nodes, thus building the entire node tree.
    /// </remarks>
    /// <exception cref=""ArgumentNullException"">Thrown if the `children` list is null.</exception>
    /// <exception cref=""KeyNotFoundException"">Thrown if the `parentTransform.gameObject` is not found in the `parentChildDictionary`.</exception>
    void DisplayNodeChildren(List<Nodes> children, int depth, Transform parentTransform, int? parentnodeId)
    {
        if (children == null || children.Count == 0)
            return;

        // Get node width and height from prefab to determine spacing
        nodeWidth = signPrefab.GetComponent<Renderer>().bounds.size.x;
        float nodeHeight = signPrefab.GetComponent<Renderer>().bounds.size.y;
        
        // Calculate the spacing between nodes
        xSpacing = (nodeWidth * 0.1f) / 2;
        float ySpacing = ((nodeHeight * 0.1f) + (nodeWidth * 0.1f) / 2);

        // Adjust the spacing based on the screen scale
        xSpacing = xSpacing * scaleAdaptator;
        ySpacing = ySpacing * scaleAdaptator;

        //Debug.Log("xSpacing: " + xSpacing + " | ySpacing: " + ySpacing);

        // Generate a random color for the connection line
        Color randomColor = new Color(Random.value, Random.value, Random.value);

        // Iterate through the child nodes and create signs for each
        foreach (var child in children)
        {
            Vector3 localPosition = new Vector3(0, 0.2f, 0);

            // Calculate the X position based on the depth and number of nodes at that depth
            localPosition.x = 1.2f * ((nbMaxNodeByDepth[depth] - nbNodeByDepth[depth])) - ((nbMaxNodeByDepth[depth]) / 2);
            localPosition.z = parentTransform.localPosition.z + ySpacing;

            nbNodeByDepth[depth] -= 1; // Update the number of nodes at this depth

            // Create the sign for the child node
            GameObject childSign = CreateSign(child, localPosition, parentnodeId);

            // Store the original scale of the child node's transform
            originalChildScales[childSign.transform] = childSign.transform.localScale;

            // Attach the child node to the screen and set its rotation
            childSign.transform.SetParent(screenRect, false);
            childSign.transform.localRotation = Quaternion.identity;

            // Add the child node to the parent-child dictionary
            if (!parentChildDictionary.ContainsKey(parentTransform.gameObject))
            {
                parentChildDictionary[parentTransform.gameObject] = new List<GameObject>();
            }
            parentChildDictionary[parentTransform.gameObject].Add(childSign);

            // Create a line between the parent and child node
            CreateLine(parentTransform, childSign.transform, randomColor);
            
            // If the child node has its own children, recursively display them
            if (child.children != null && child.children.Count > 0)
            {
                DisplayNodeChildren(child.children, depth + 1, childSign.transform, nodeToInfoDictionary[childSign].id);
            }
        }
    }

    /// <summary>
    /// Creates a sign object for a given node at the specified position.
    /// The sign object represents the node visually in the scene, with its properties like ID, tag, and text.
    /// The sign is also added to a dictionary for later reference and linked to the parent node if applicable.
    /// </summary>
    /// <param name=""currentNode"">The node data containing information like ID, tag, and hierarchy details.</param>
    /// <param name=""position"">The position in the scene where the sign should be placed.</param>
    /// <param name=""parentnodeId"">The ID of the parent node, if applicable. This helps in establishing parent-child relationships between nodes.</param>
    /// <returns>A GameObject representing the visual sign of the node.</returns>
    /// <remarks>
    /// This method:
    /// - Instantiates a sign prefab at the specified position and applies the necessary rotation.
    /// - Updates the scale of the sign based on predefined values.
    /// - Stores information about the node in a dictionary for later reference.
    /// - Updates the sign's text content with the node's details (e.g., ID, tag name).
    /// - The method also handles the case where the node is the root by adjusting its position.
    /// </remarks>
    /// <exception cref=""ArgumentNullException"">Thrown if the `signPrefab` or other critical components are not properly assigned.</exception>
    GameObject CreateSign(Nodes currentNode, Vector3 position, int? parentnodeId)
    {
        // Adjust position if parentnodeId is null, indicating it's a root node
        if (parentnodeId == null)
        {
            position.y = 0.2f;
            position.z = -3f; // Position root node in a specific place in the scene
        }
        
        // Instantiate the sign prefab at the specified position and apply rotation
        GameObject node = Instantiate(signPrefab, position, Quaternion.Euler(-90, 0, 0));

        // Set the scale of the node
        RectTransform nodeRect = node.GetComponent<RectTransform>();
        if (nodeRect != null)
        {
            node.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f); // Adjust scale of the node for visibility
        }

        // Add the node information to a dictionary for later use
        nodeToInfoDictionary[node] = new NodeInfo(
            currentNode.id, 
            currentNode.tag.name, 
            currentNode.hierarchyId, 
            currentNode.tag.tagTypeId, 
            currentNode.tag.tagsetId, 
            parentnodeId
        );

        // Find the Canvas component to update the text inside the node
        Canvas signCanvas = node.transform.Find("Canvas")?.GetComponent<Canvas>();
        UpdateText updateTextScript = signCanvas?.GetComponentInChildren<UpdateText>();

        if (updateTextScript != null)
        {
            // Update the content of the text inside the sign
            updateTextScript.UpdateTextContent($"Name: {currentNode.tag.name}\nID: {currentNode.id}\nTag ID: {currentNode.tagId}");
        }
        else
        {
            Debug.LogError("The UpdateText script was not found on the Canvas.");
        }

        return node;
    }

    /// <summary>
    /// Creates a visual line between a parent and child transform, representing their relationship.
    /// If a line already exists between the same parent and child, it is replaced with a new one.
    /// The line is rendered using a `LineRenderer` component and customized with a specified color.
    /// </summary>
    /// <param name=""parent"">The parent transform in the hierarchy, used as the starting point of the line.</param>
    /// <param name=""child"">The child transform in the hierarchy, used as the ending point of the line.</param>
    /// <param name=""lineColor"">The color of the line to be drawn between the parent and child.</param>
    /// <remarks>
    /// - This method checks if a line already exists between the parent and child, and if so, it destroys the existing line before creating a new one.
    /// - A new GameObject is created with a `LineRenderer` component that renders a line between the parent and child positions.
    /// - The line width is adjusted based on the scale of the scene (controlled by `scaleCorrector`).
    /// - A new material is created for the line and the specified color is applied.
    /// </remarks>
    void CreateLine(Transform parent, Transform child, Color lineColor)
    {
        // Check if a line already exists between the parent and child
        if (existingLines.ContainsKey((parent.gameObject, child.gameObject)))
        {
            // If the line exists, remove the existing one
            LineRenderer existingLine = existingLines[(parent.gameObject, child.gameObject)];
            Destroy(existingLine.gameObject);
            existingLines.Remove((parent.gameObject, child.gameObject));
        }

        // Create a new GameObject for the line
        GameObject lineObj = new GameObject("Relation");
        lineObj.transform.SetParent(parent);

        // Add a LineRenderer component to the line object
        LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();
        
        // Set the width of the line
        lineRenderer.startWidth = 0.2f * scaleCorrector.x * 0.1f;
        lineRenderer.endWidth = 0.2f * scaleCorrector.x * 0.1f;

        // Set the number of positions (start and end)
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, parent.position); // Set the start position (parent)
        lineRenderer.SetPosition(1, child.position);  // Set the end position (child)

        // Create a new material for the line and set its color
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = lineColor;
        lineRenderer.endColor = lineColor;

        // Store the line in the dictionary to track it
        existingLines[(parent.gameObject, child.gameObject)] = lineRenderer;
    }

    /// <summary>
    /// Updates the positions of the line between the parent and child transforms.
    /// This method is called when the parent or child moves, to ensure the line is updated accordingly.
    /// </summary>
    /// <param name=""parent"">The parent transform, which is the start point of the line.</param>
    /// <param name=""child"">The child transform, which is the end point of the line.</param>
    /// <param name=""lineRenderer"">The `LineRenderer` component that is responsible for drawing the line between the parent and child.</param>
    /// <remarks>
    /// - This method ensures the visual line always connects the correct parent and child positions in the world space.
    /// - The `LineRenderer` component's positions are updated by setting the start (0) and end (1) points to the parent and child positions, respectively.
    /// - It is useful to call this method when either the parent or child transform moves, so that the line follows the new positions.
    /// </remarks>
    void UpdateLinePositions(Transform parent, Transform child, LineRenderer lineRenderer)
    {
        // Check if the lineRenderer is valid
        if (lineRenderer != null)
        {
            // Update the positions of the line based on the parent's and child's current positions
            lineRenderer.SetPosition(0, parent.position);  // Start position of the line (parent)
            lineRenderer.SetPosition(1, child.position);   // End position of the line (child)
        }
    }

    /// <summary>
    /// Sets the transparency (alpha value) of the given object and all of its children that have a Renderer component.
    /// This method is useful when you need to adjust the visibility of an object and its hierarchy in the scene.
    /// </summary>
    /// <param name=""obj"">The main `GameObject` whose transparency will be set, as well as all of its child objects.</param>
    /// <param name=""alpha"">The alpha value representing the transparency. A value between 0 (fully transparent) and 1 (fully opaque).</param>
    /// <remarks>
    /// - The method first checks if the provided object has a `Renderer` component and applies the transparency to it.
    /// - Then, the transparency is applied recursively to all child objects that have a `Renderer` component.
    /// - This can be useful for making an entire object and its children fade in or out, or to change their visibility dynamically during gameplay.
    /// </remarks>
    void SetTransparency(GameObject obj, float alpha)
    {
        if (obj == null) return;

        // Apply transparency to the main object if it has a Renderer component
        Renderer mainRenderer = obj.GetComponent<Renderer>();
        if (mainRenderer != null)
        {
            ApplyTransparency(mainRenderer, alpha);
        }

        // Apply transparency to all child objects that have a Renderer component
        foreach (Renderer childRenderer in obj.GetComponentsInChildren<Renderer>())
        {
            ApplyTransparency(childRenderer, alpha);
        }
    }

    /// <summary>
    /// Applies transparency (alpha value) to a `Renderer` component. This method modifies the color of the material 
    /// and ensures the material supports transparency by adjusting its blend mode and other render settings.
    /// </summary>
    /// <param name=""renderer"">The `Renderer` component whose material transparency will be adjusted.</param>
    /// <param name=""alpha"">The alpha value representing the transparency. A value between 0 (fully transparent) and 1 (fully opaque).</param>
    /// <remarks>
    /// - This method modifies the `Renderer`'s material color by adjusting the alpha channel.
    /// - It ensures that the material supports transparency by setting the appropriate blend mode and other relevant properties.
    /// - It disables alpha testing, enables alpha blending, and sets the render queue to ensure proper rendering of transparent objects.
    /// </remarks>
    void ApplyTransparency(Renderer renderer, float alpha)
    {
        if (renderer == null || renderer.material == null) return;

        // Get the current color and modify the alpha channel
        Color color = renderer.material.color;
        color.a = alpha;
        renderer.material.color = color;

        // Ensure that the material supports transparency
        renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        renderer.material.SetInt("_ZWrite", 0); // Disable depth writing for transparent objects
        renderer.material.DisableKeyword("_ALPHATEST_ON"); // Disable alpha testing (if enabled)
        renderer.material.EnableKeyword("_ALPHABLEND_ON"); // Enable alpha blending
        renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON"); // Disable premultiplied alpha blending
        renderer.material.renderQueue = 3000; // Set the render queue to ensure transparency is rendered correctly
    }

    /// <summary>
    /// This method attempts to start dragging an object. It checks whether the touched object is a node or a screen mover, 
    /// and initializes necessary variables and visual effects to start the drag process.
    /// </summary>
    void TryStartDragging()
    {
        // Set the dragging flag to true indicating that dragging has started
        isDragging = true;

        // Check if the touched object is a Node
        if (touchedNode.CompareTag("Node"))
        {
            draggedObject = touchedNode; // Set the dragged object to the touched node
            Transform canvasTransform = touchedNode.transform.Find("Canvas"); // Find the "Canvas" inside the touched node
            Transform textTransform = canvasTransform.Find("text"); // Find the "text" object inside the Canvas
            tmpText = textTransform.GetComponent<TextMeshProUGUI>(); // Get the TextMeshProUGUI component of the text object
        }
        // Check if the touched object is a screen-moving object
        else if (touchedNode.CompareTag("MoveScreen"))
        {
            draggedObject = touchedNode.transform.parent.gameObject; // Set the dragged object to the parent of the "MoveScreen" object
            foreach (var entry in existingLines) // Iterate over all the existing lines (connections between nodes)
            {
                GameObject parent = entry.Key.parent; // Get the parent object of the line
                GameObject child = entry.Key.child; // Get the child object of the line
                LineRenderer line = entry.Value; // Get the LineRenderer component that renders the line

                // If the line exists, disable it (hide the line)
                if (line != null)
                {
                    line.gameObject.SetActive(false); // Hide the line
                }
            }
        }

        // Set the offset for dragging (distance between the object and the point of touch)
        offset = new Vector3(draggedObject.transform.position.x - hit.point.x, 
                            draggedObject.transform.position.y - hit.point.y, 
                            0); // Set the offset based on the position of the object and the touch point

        // Change the transparency of the dragged object to indicate it's being dragged
        SetTransparency(draggedObject, 0.5f); // Set the transparency of the dragged object to 50% (0.5f)
    }

    /// <summary>
    /// This method handles the dragging of an object. It updates the object's position based on the mouse position in the scene.
    /// It also ensures that the object is moved along a specific plane (the screen plane) and updates the connections between nodes if necessary.
    /// </summary>
    void DragObject()
    {
        // If no object is being dragged, exit the method
        if (draggedObject == null) return;

        // Create a ray from the mouse position in world space
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // Create a horizontal plane based on the orientation and position of the screen
        Plane screenPlane = new Plane(screenRect.up, screenRect.position);

        // Check if the ray intersects the screen plane
        if (screenPlane.Raycast(ray, out float distance))
        {
            // Calculate the position on the screen plane where the ray hits
            Vector3 targetPosition = ray.GetPoint(distance);

            // Apply the offset to the target position to maintain the initial relative distance
            targetPosition += offset;

            // Update the position of the dragged object
            draggedObject.transform.position = targetPosition;

            // If the dragged object is a node, update the connections between nodes
            if (draggedObject.CompareTag("Node"))
            {
                UpdateNodeConnections(draggedObject);
            }
        }
        else
        {
            Debug.LogWarning("The ray from the mouse doesn't intersect the screen plane!");
        }
    }

    /// <summary>
    /// Updates the connection lines between parent and child nodes.
    /// This method redraws the relationship lines between nodes when one of them is moved.
    /// It handles parent-child relationships by checking the `parentChildDictionary`.
    /// </summary>
    /// <param name="node">The node whose connections need to be updated.</param>
    void UpdateNodeConnections(GameObject node)
    {
        // If the node is a parent, update the lines to its children
        if (parentChildDictionary.ContainsKey(node))
        {
            // Generate a random color for the line
            Color randomColor = new Color(Random.value, Random.value, Random.value); 

            // Update the lines to each child
            foreach (var child in parentChildDictionary[node])
            {
                // Create or update the line between the parent and the child
                CreateLine(node.transform, child.transform, randomColor);
            }
        }

        // If the node is a child, find its parent and update the line
        foreach (var kvp in parentChildDictionary)
        {
            // Generate a random color for the line
            Color randomColor = new Color(Random.value, Random.value, Random.value); 

            // Check if this node is one of the children of the current parent
            if (kvp.Value.Contains(node))
            {
                // Create or update the line between the parent and this node
                CreateLine(kvp.Key.transform, node.transform, randomColor);
                break;  // Once the parent is found, no need to continue searching
            }
        }
    }

    /// <summary>
    /// Stops the dragging operation of an object.
    /// This function is called when the user releases the object they were dragging.
    /// It resets the transparency properties of the object, checks for collisions with other nodes if necessary, 
    /// and updates the connection lines between nodes if the dragged object is a screen.
    /// </summary>
    void StopDragging()
    {
        isDragging = false;  // Ends the dragging state
        if (draggedObject != null)
        {
            // Resets the transparency of the object to its original value
            SetTransparency(draggedObject, 1f);

            // If the object is a node
            if (draggedObject.CompareTag("Node"))
            {
                // Checks for collision with other nodes
                CheckCollisionWithOtherNodes(draggedObject);
            }
            else
            {
                // If the object is not a node, it means it's a screen (MoveScreen)
                foreach (var entry in existingLines)
                {
                    // Updates the position of the existing lines between nodes
                    UpdateLinePositions(entry.Key.Item1.transform, entry.Key.Item2.transform, entry.Value);

                    LineRenderer line = entry.Value;

                    // Reactivates the connection line between nodes
                    if (line != null)
                    {
                        line.gameObject.SetActive(true);
                    }
                }
            }
            // Resets the currently dragged object
            draggedObject = null;
        }
    }

    /// <summary>
    /// Checks for collisions between the dragged node and other nodes in the scene.
    /// This function uses the <see cref="Physics.OverlapBox"/> method to detect collisions 
    /// between the BoxCollider of the currently dragged node and other colliders in the scene.
    /// If a collision with another node is detected, a PUT request is made to update the API.
    /// </summary>
    /// <param name="draggedNode">
    /// The GameObject representing the node currently being dragged.
    /// </param>
    void CheckCollisionWithOtherNodes(GameObject draggedNode)
    {
        // Retrieves the BoxCollider of the dragged node
        BoxCollider draggedCollider = draggedNode.GetComponent<BoxCollider>();

        // Checks if the dragged node's collider is colliding with any other BoxColliders in the scene
        Collider[] colliders = Physics.OverlapBox(draggedCollider.bounds.center, draggedCollider.bounds.extents, Quaternion.identity);

        // Iterates through all detected collisions
        foreach (Collider col in colliders)
        {
            // If the collision is not with the same node, proceed with the request
            if (col.gameObject != draggedNode && col.CompareTag("Node"))
            {
                // Displays a message in the console for the detected collision
                Debug.Log("Collision detected with node:" + col.gameObject.name);
                
                // Makes a PUT request to update the parent of the dragged node
                StartCoroutine(UpdateParentAPI(draggedNode, col.gameObject));
                
                // If a visual update is required, call the function to refresh the display
                if(updateVisuel)
                {
                    StartCoroutine(CallShowNodeAPI(defaultNode));
                    updateVisuel = false;
                }
            }
        }
    }

    /// <summary>
    /// Sends a PUT request to update the parent of a moved node in the API.
    /// This function extracts the ID of the moved node and its previous parent from the scene information,
    /// then sends a PUT request containing the new parent ID and old parent ID to a REST API.
    /// </summary>
    /// <param name="draggedNode">The GameObject representing the moved node whose parent needs to be updated.</param>
    /// <param name="newParentNode">The GameObject representing the new parent to which the moved node should be attached.</param>
    /// <returns>A coroutine that sends the PUT request and waits for the server response.</returns>
    IEnumerator UpdateParentAPI(GameObject draggedNode, GameObject newParentNode)
    {
        // Extract the ID of the dragged node from the text displayed on the Canvas
        int extractedId = -1;
        Transform textTransform = draggedNode.transform.Find("Canvas/text");
        if (textTransform != null)
        {
            TMP_Text tmpTextComponent = textTransform.GetComponent<TMP_Text>();
            if (tmpTextComponent != null)
            {
                string textValue = tmpTextComponent.text;
                extractedId = ExtractId(textValue);
            }
            else
            {
                Debug.LogError("No Text or TMP_Text component found in 'text'");
            }
        }
        else
        {
            Debug.LogError("The object 'text' was not found!");
        }

        // Retrieve the ID of the dragged node from the extracted ID
        int draggedNodeId = extractedId;

        // Retrieve the ID of the old parent (before the update)
        int oldParentId = -1;  // Default value, could be a value indicating no parent

        // Check if the node has a parent in the dictionary
        foreach (var node in nodeToInfoDictionary.Keys)
        {
            if (nodeToInfoDictionary[node].id == draggedNodeId)
            {
                oldParentId = nodeToInfoDictionary[node].parentnodeId.Value;
                break;  // Stop once found
            }
        }

        
        // Extract the ID of the new parent node from the text displayed on the Canvas
        int extractedIdNewParent = -1;
        Transform textTransformNewParent = newParentNode.transform.Find("Canvas/text");
        if (textTransform != null)
        {
            TMP_Text tmpTextComponent = textTransformNewParent.GetComponent<TMP_Text>();
            if (tmpTextComponent != null)
            {
                string textValue = tmpTextComponent.text;
                extractedIdNewParent = ExtractId(textValue);
            }
            else
            {
                Debug.LogError("No Text or TMP_Text component found in 'text'");
            }
        }
        else
        {
            Debug.LogError("The object 'text' was not found!");
        }
        
        // Retrieve the ID of the new parent
        //int newParentId = nodeToInfoDictionary[newParentNode].id;

        // Prepare the request body in JSON format
        var requestData = new
        {
            ParentNodeId = extractedIdNewParent,
            OldParentNodeId = oldParentId
        };

        string jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);

        // Perform the PUT request
        using (UnityWebRequest www = UnityWebRequest.Put($"https://localhost:5001/api/node/{draggedNodeId}/updateParent", jsonData))
        {
            www.method = UnityWebRequest.kHttpVerbPUT;
            www.SetRequestHeader("Content-Type", "application/json");

            // Log the sent JSON for debugging
            Debug.Log("Data sent: " + jsonData);

            // Assign the custom certificate handler
            www.certificateHandler = new CustomCertificateHandler();

            updateVisuel = true;

            // Wait for the response
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Parent successfully updated!");
            }
            else
            {
                Debug.LogError("Error while updating parent:" + www.error);
                Debug.LogError("Server response:" + www.downloadHandler.text);
            }
        }
    }


    /// <summary>
    /// Extracts the node ID from a string using a regular expression.
    /// The function searches for a specific pattern in the text (specifically after "ID:") and returns the ID as an integer.
    /// </summary>
    /// <param name="textValue">
    /// The text containing the ID to extract. The expected format is "ID: [id]" where [id] is an integer number.
    /// </param>
    /// <returns>
    /// An integer representing the ID extracted from the text. Returns -1 if extraction fails.
    /// </returns>
    int ExtractId(string textValue)
    {
        // Regular expression to capture a number after "ID:"
        Match match = Regex.Match(textValue, @"ID:\s*(\d+)");

        if (match.Success)
        {
            return int.Parse(match.Groups[1].Value);
        }

        Debug.LogError("Unable to extract text ID!");
        return -1; // Default value if extraction fails
    }
    
    /// <summary>
    /// Adds a new node to the database by sending an HTTP POST request to the API.
    /// The function serializes the node information into JSON and sends the request to the API to add the node.
    /// If the addition is successful, another API is called to display the updated node hierarchy.
    /// </summary>
    /// <param name="hierarchyId">The ID of the hierarchy to which the node belongs.</param>
    /// <param name="name">The name of the node to be added.</param>
    /// <param name="tagTypeId">The ID of the tag type associated with the node.</param>
    /// <param name="tagSetId">The ID of the tag set to which the node belongs.</param>
    /// <param name="parentNodeId">The ID of the parent node. Can be null if the node is a root node.</param>
    /// <returns>A coroutine that sends the POST request asynchronously and waits for the server's response.</returns>
    public IEnumerator AddNodeFromDatabase(int hierarchyId, string name, int tagTypeId, int tagSetId, int? parentNodeId)
    {
        var requestData = new
        {
            hierarchy_id = hierarchyId,
            name = name,
            tagtype_id = tagTypeId,
            tagset_id = tagSetId,
            parentnode_id = parentNodeId ?? null
        };

        // Serialize to JSON
        string jsonData = JsonConvert.SerializeObject(requestData, Formatting.Indented);
        byte[] jsonToSend = Encoding.UTF8.GetBytes(jsonData);

        // Create POST request
        using (UnityWebRequest www = new UnityWebRequest("https://localhost:5001/api/node/add", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(jsonToSend);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Assign custom certificate handler if needed
            www.certificateHandler = new CustomCertificateHandler();

            // Send the request and wait for response
            yield return www.SendWebRequest();
        
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Node successfully added!");
                StartCoroutine(CallShowHierarchyAPI(hierarchyId)); // Call another API to show the updated hierarchy
            }
            else
            {
                Debug.LogError("Error while adding node:" + www.error);
                Debug.LogError("Server response:" + www.downloadHandler.text);
            }
        }
    }


    /// <summary>
    /// Deletes a node from the database by sending an HTTP DELETE request to the API.
    /// The function locates the ID of the node to delete, constructs the request URL, and performs the deletion.
    /// After a successful deletion, the node hierarchy is updated.
    /// </summary>
    /// <param name="nodeId">The ID of the node to delete.</param>
    /// <returns>A coroutine that sends the DELETE request asynchronously and waits for the server's response.</returns>
    public IEnumerator DeleteNodeFromDatabase(int nodeId)
    {
        // API URL with the ID of the node to delete
        string url = $"https://localhost:5001/api/node/{nodeId}/delete";

        int hierarchyId = 0;

        // Search for the hierarchy ID related to the node to delete
        foreach (var node in nodeToInfoDictionary.Keys)
        {
            if (nodeToInfoDictionary[node].id == nodeId)
            {
                hierarchyId = nodeToInfoDictionary[node].hierarchyId;
            }
        }

        // Create DELETE request
        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            www.SetRequestHeader("Content-Type", "application/json");

            // Assign custom certificate handler if needed
            www.certificateHandler = new CustomCertificateHandler();

            // Debug: print the URL and node being deleted
            Debug.Log($"URL: {url}");
            Debug.Log($"Deleting node with ID: {nodeId}");

            // Send the request and wait for response
            yield return www.SendWebRequest();

            // Check if the request was successful
            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Node {nodeId} successfully deleted!");
                StartCoroutine(CallShowHierarchyAPI(hierarchyId)); // Refresh hierarchy after deletion
            }
            else
            {
                // Handle errors if deletion fails
                Debug.LogError("Error while deleting node:" + www.error);
                Debug.LogError("Server response:" + www.downloadHandler.text);
            }
        }
    }

    /// <summary>
    /// Calculates and updates the number of nodes present at each depth level in a node hierarchy.
    /// This function is recursive and traverses all children of a node while keeping a counter for each depth level.
    /// </summary>
    /// <param name="node">The current node to process.</param>
    /// <param name="depth">The current depth in the hierarchy.</param>
    /// <remarks>
    /// The function updates the <see cref="nbNodeByDepth"/> dictionary, which tracks the number of nodes at each depth.
    /// It also updates the <see cref="ScreenWidthInNodes"/> variable to reflect the maximum number of nodes found at any given depth.
    /// </remarks>
    void countNodeByDepth(Nodes node, int depth)
    {
        // If the depth level doesn't exist in the dictionary, initialize it to 0
        if (!nbNodeByDepth.ContainsKey(depth))
        {
            nbNodeByDepth[depth] = 0;
        }

        // Increment the number of nodes at the given depth
        nbNodeByDepth[depth] += 1;

        // If the number of nodes at this depth exceeds the current screen width, update it
        if (nbNodeByDepth[depth] > ScreenWidthInNodes)
        {
            ScreenWidthInNodes = nbNodeByDepth[depth];
        }

        // Recursively call this function for each child of the current node
        foreach (Nodes child in node.children)
        {
            countNodeByDepth(child, depth + 1);
        }
    }
}