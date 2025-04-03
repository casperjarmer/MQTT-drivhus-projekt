using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

public class MQTTClient : MonoBehaviour
{
    private MqttClient client;
    // private string broker = "test.mosquitto.org"; // Another free online broker
    //private string broker = "mqtt.eclipseprojects.io";
    private string broker = "10.126.128.221"; // Used to connect to a lokal private broker

    private string potTopic = "arduino/potentiometerC"; // Topic for potentiometer
    private string butTopic = "arduino/buttonC";
    private string climateRoofMotorTopic = "climate_roof_motor"; // Topic for led

    private int potValue = 0; // Stores received potentiometer value
    private int butValue = 0;
    public string LEDValue = ""; // Stores received led value

    private float potRotation = 0.0f; // The 3D potentiometers rotation

    // The gameobjects in the scene
    public GameObject potentiometer;
    public GameObject buttonhead;

    public GameObject greenLED;
    public GameObject blueLED;


    private float blinkTime = -1f;  
  

    void Start()
    {
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
            client.Subscribe(new string[] { potTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { butTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.Subscribe(new string[] { climateRoofMotorTopic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
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
        // If it is data from the potentiometer, parse the value out, so it can be used in another function/ in update
        if (e.Topic == potTopic)
        {
            int.TryParse(message, out potValue);
        }
        if (e.Topic == butTopic)
        {
            int.TryParse(message, out butValue);
        }

        if (e.Topic == climateRoofMotorTopic)
        {
            LEDValue = message;
            
        }
    }

 
    void Update()
    {
        Debug.Log($"Potentiometer Value: {potValue}");        

        //Rotate potentiometer
        potRotation = 50 + potValue / 4;
        potentiometer.transform.rotation = Quaternion.Euler(0.0f, potRotation, 0.0f);


        Debug.Log($"Led Value: {LEDValue}");

        if (butValue == 1)
        {
            buttonhead.transform.localPosition = new Vector3(0, 0.241f, 0);
        }
        else
        {
            buttonhead.transform.localPosition = new Vector3(0, 0.328f, 0);
        }

        if (LEDValue == "b" && blinkTime == -1f)  // If ledValue is "g" and no blinking started yet
        {
            StartCoroutine(BlinkBlueLED());  // Start the coroutine to blink material
            blinkTime = Time.time;  // Record the time when blinking started
        }
        if (LEDValue == "g" && blinkTime == -1f)  // If ledValue is "g" and no blinking started yet
        {
            StartCoroutine(BlinkGreenLED());  // Start the coroutine to blink material
            blinkTime = Time.time;  // Record the time when blinking started
        }


        // Reset the material after 0.5 seconds
        if (blinkTime >= 0 && Time.time - blinkTime >= 0.5f)
        {
            LEDValue = "";  // Reset ledValue to empty
            blinkTime = -1f;  // Reset the blink timer
        }

    }

    private IEnumerator BlinkBlueLED()
    {
        yield return new WaitForSeconds(0.5f); // Trying to make a little delay to match the real led 
         // Change to the blink material for 0.5 seconds
        blueLED.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        // After 0.5 seconds, reset to the default material
        blueLED.SetActive(false);
    }
    private IEnumerator BlinkGreenLED()
    {
        yield return new WaitForSeconds(0.5f); // Trying to make a little delay to match the real led 
                                               // Change to the blink material for 0.5 seconds
        greenLED.SetActive(true);
        yield return new WaitForSeconds(0.5f);
        // After 0.5 seconds, reset to the default material
        greenLED.SetActive(false);
    }

    void OnDestroy()
    {
        if (client != null && client.IsConnected)
        {
            client.Disconnect();
        }
    }
}
