using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.UI;
using TMPro;

public class MQTTClientMads : MonoBehaviour
{
    private MqttClient client;
    // private string broker = "test.mosquitto.org"; // Another free online broker
    // private string broker = "mqtt.eclipseprojects.io";
    //private string broker = "10.126.128.222"; // Used to connect to a lokal private broker
    private string broker = "10.126.128.209"; // Used to connect to a private broker


    private string climateRoofMotorTopic = "climate_arduino/climate_roof_motor"; // Topic for motor
    public string RoofMotorValue = ""; // Stores received motor value
    private string climateFanMotorTopic = "climate_arduino/climate_fan_motor"; // Topic for fan
    public string fanMotorValue = ""; // Stores received fan value
    private string UVLightTopic = "climate_arduino/climate_uv_light"; // Topic for fan
    public string UVLightValue = ""; // Stores received fan value
    private string climateTemperatureTopic = "climate_arduino/climate_temperature_sensor"; // Topic for temperature
    public string temperatureValue = ""; // Stores received temperature value
    private string climateLightTopic = "climate_arduino/climate_light_sensor"; // Topic for light
    public string lightValue = ""; // Stores received light value
    private string climateHumidityTopic = "climate_arduino/climate_humidity_sensor"; // Topic for humidity
    public string humidityValue = ""; // Stores received humidity value

    // The gameobjects in the scene
    public GameObject OutdoorLight;
    public GameObject MotorRod;
    public GameObject Roof;
    public GameObject Fan;
    public GameObject UVLightRed;
    public GameObject UVLightBlue;
    public GameObject TempText;
    public GameObject LightText;
    public GameObject HumidityText;

    public TMP_Text TempTextTMP; // Reference to the TMP text component
    public TMP_Text LightTextTMP; // Reference to the TMP text component
    public TMP_Text HumidityTextTMP; // Reference to the TMP text component

    private int fanRotationSpeed = 1000; // Rotation speed in degrees per second
    private bool rotateFan = false; // Flag to control fan rotation
    private int OutsideLightValueClamped = 0;

    // Animation
    public Animator RoofAnimator;
    public Animator MotorRodAnimator;

    void Start()
    {
        TempTextTMP = TempText.GetComponent<TMP_Text>(); // Get the TMP text component
        LightTextTMP = LightText.GetComponent<TMP_Text>(); // Get the TMP text component
        HumidityTextTMP = HumidityText.GetComponent<TMP_Text>(); // Get the TMP text component

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
            client.Subscribe(new string[] { climateTemperatureTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { climateHumidityTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { climateLightTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
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

        if (e.Topic == climateHumidityTopic)
        {
            humidityValue = message;
        }

        if (e.Topic == climateLightTopic)
        {
            lightValue = message;
        }

        if (e.Topic == climateTemperatureTopic)
        {
            temperatureValue = message;
        }

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
        if (temperatureValue != "")
        {
            TempTextTMP.text = "Temperature: " + temperatureValue + " °C"; // Update the text with the received temperature value
        }

        if (humidityValue != "")
        {
            HumidityTextTMP.text = "Humidity: " + humidityValue + " %"; // Update the text with the received humidity value
        }

        if (lightValue != "")
        {
            LightTextTMP.text = "Light value: " + lightValue; // Update the text with the received light value
        }

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
