using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MQTTClientMads : MonoBehaviour
{
    private MqttClient client;
    // private string broker = "test.mosquitto.org"; // Another free online broker
    private string broker = "mqtt.eclipseprojects.io";
    //private string broker = "10.126.128.222"; // Used to connect to a lokal private broker

    private string climateRoofMotorTopic = "climate_roof_motor"; // Topic for motor
    public string RoofMotorValue = ""; // Stores received motor value
    private string climateFanMotorTopic = "climate_fan_motor"; // Topic for fan
    public string fanMotorValue = ""; // Stores received fan value
    private string UVLightTopic = "climate_uv_light"; // Topic for fan
    public string UVLightValue = ""; // Stores received fan value

    // The gameobjects in the scene
    public GameObject MotorRod;
    public GameObject Roof;
    public GameObject Fan;
    public GameObject UVLightRed;
    public GameObject UVLightBlue;

    private int fanRotationSpeed = 1000; // Rotation speed in degrees per second
    private bool rotateFan = false; // Flag to control fan rotation

    // Animation
    public Animator RoofAnimator;
    public Animator MotorRodAnimator;

    void Start()
    {
        //RoofAnimator.SetBool("open", true);
        //MotorRodAnimator.SetBool("open", true);

        //StartCoroutine(Test());

        Application.runInBackground = true;  // Ensures the game runs even when out of focus, so you can click on the webpage and see the outpot in Unity 

        // Connect to MQTT broker
        client = new MqttClient(broker);
        client.MqttMsgPublishReceived += OnMessageReceived;

        string clientId = Guid.NewGuid().ToString();
        client.Connect(clientId);


        if (client.IsConnected)
        {
            Debug.Log("Connected to MQTT Broker!");
            // How to subscribe to the topics 
            client.Subscribe(new string[] { climateRoofMotorTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { climateFanMotorTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { UVLightTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        }
        else
        {
            Debug.LogError("Failed to connect to MQTT Broker!");
        }
    }

    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string message = Encoding.UTF8.GetString(e.Message);
        Debug.Log($"Received: {e.Topic} - {message}");

        if (e.Topic == climateRoofMotorTopic)
        {
            RoofMotorValue = message;

        }
        if (e.Topic == climateFanMotorTopic)
        {
            fanMotorValue = message;
        }
        if (e.Topic == UVLightTopic)
        {
            UVLightValue = message;
        }
    }

    void Update()
    {
        if (RoofMotorValue == "o")  // If ledValue is "o"
        {
            StartCoroutine(Extend());  // Start the coroutine to blink material
        }
        if (RoofMotorValue == "c")  // If ledValue is "c"
        {
            StartCoroutine(Retract());  // Start the coroutine to blink material
        }

        if (fanMotorValue == "f")  // If fanValue is "f"
        {
            Fan.transform.Rotate(new Vector3(0, fanRotationSpeed, 0) * Time.deltaTime);
        }

        if (fanMotorValue == "s")  // If fanValue is "s"
        {
            Fan.transform.Rotate(new Vector3(0, 0, 0) * Time.deltaTime);
        }

        if (UVLightValue == "u")  // If ledValue is "u"
        {
            UVLightRed.SetActive(true);
            UVLightBlue.SetActive(true);
        }
        if (UVLightValue == "v")  // If ledValue is "v"
        {
            UVLightRed.SetActive(false);
            UVLightBlue.SetActive(false);
        }

    }

    private IEnumerator Extend()
    {
        RoofAnimator.SetBool("open", true);
        MotorRodAnimator.SetBool("open", true);
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator Retract()
    {
        RoofAnimator.SetBool("open", false);
        MotorRodAnimator.SetBool("open", false);
        yield return new WaitForSeconds(0.5f);
    }

    void OnDestroy()
    {
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
    }
}
