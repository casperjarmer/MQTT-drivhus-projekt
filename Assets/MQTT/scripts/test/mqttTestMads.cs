using UnityEngine;
using System;
using System.Collections;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.UIElements;

public class MQTTClientMads : MonoBehaviour
{
    private MqttClient client;
    private string broker = "10.126.128.221"; // Local private broker

    // Topic for the rod's position
    private string rodPositionTopic = "rod/position";
    private float rodPosition = 0.0f; // Stores received rod position value

    // The GameObjects in the scene
    public GameObject windowObject; // Window digital twin
    public GameObject rodObject;    // Rod digital twin

    // Calibration parameters for the rod's real-world position.
    // Adjust these based on your actual measurements.
    public float minRodPos = 0.0f;    // Rod's fully closed position (real-world value)
    public float maxRodPos = 100.0f;  // Rod's fully open position (real-world value)

    // Calibration parameters for the window's rotation in Unity.
    public float minAngle = 0.0f;     // Window closed angle in Unity
    public float maxAngle = 90.0f;    // Window fully open angle in Unity

    // Positions for the rod object in Unity.
    // Set these in the Inspector to match the closed/open states of the rod.
    public Vector3 rodClosedPosition; // Rod's position when fully closed
    public Vector3 rodOpenPosition;   // Rod's position when fully open

    // Debugging slider to simulate rod position without MQTT
    public bool useDebugSlider = true;  // Toggle to use slider input for debugging
    public GameObject uiDocumentObject; // Reference to the UI Document GameObject
    private Slider debugSlider;  // Reference to the UI slider in the scene

    void Start()
    {
        Application.runInBackground = true;

        // Load the UIDocument and reference the Slider
        if (uiDocumentObject != null)
        {
            var uiDocument = uiDocumentObject.GetComponent<UIDocument>();
            if (uiDocument != null)
            {
                var root = uiDocument.rootVisualElement;
                debugSlider = root.Q<Slider>("debugSlider");

                if (debugSlider != null)
                {
                    debugSlider.lowValue = minRodPos;
                    debugSlider.highValue = maxRodPos;
                    debugSlider.value = minRodPos;
                    Debug.Log($"Debug slider initialized: lowValue={debugSlider.lowValue}, highValue={debugSlider.highValue}, value={debugSlider.value}");
                }
                else
                {
                    Debug.LogError("Debug slider reference is not set!");
                }
            }
            else
            {
                Debug.LogError("UIDocument component is not found!");
            }
        }
        else
        {
            Debug.LogError("UI Document GameObject reference is not set!");
        }

        if (!useDebugSlider)
        {
            // Connect to the MQTT broker if not in debug mode
            client = new MqttClient(broker);
            client.MqttMsgPublishReceived += OnMessageReceived;

            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);

            if (client.IsConnected)
            {
                Debug.Log("Connected to MQTT Broker!");
                // Subscribe to the rod position topic
                client.Subscribe(new string[] { rodPositionTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            }
            else
            {
                Debug.LogError("Failed to connect to MQTT Broker!");
            }
        }
        else
        {
            Debug.Log("Debug mode enabled. Using slider input.");
        }
    }

    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Message);
        Debug.Log($"Received: {e.Topic} - {message}");

        // Process only the rod position data
        if (e.Topic == rodPositionTopic)
        {
            float.TryParse(message, out rodPosition);
        }
    }

    void Update()
    {
        // If in debug mode, override rodPosition with the slider value
        if (useDebugSlider && debugSlider != null)
        {
            rodPosition = debugSlider.value;
        }

        // Normalize the rod's position between minRodPos and maxRodPos
        float normalizedValue = Mathf.InverseLerp(minRodPos, maxRodPos, rodPosition);

        // Map the normalized value to the window's rotation angle
        float windowAngle = Mathf.Lerp(minAngle, maxAngle, normalizedValue);
        // Apply the rotation to the window object (assuming rotation around the Y-axis)
        windowObject.transform.rotation = Quaternion.Euler(0, 0, windowAngle);

        // Interpolate the rod object's position between the closed and open positions
        rodObject.transform.position = Vector3.Lerp(rodClosedPosition, rodOpenPosition, normalizedValue);

        // Debug log for verification
        Debug.Log($"Rod Position: {rodPosition} (normalized: {normalizedValue}) => Window Angle: {windowAngle}");
    }

    void OnDestroy()
    {
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
    }
}