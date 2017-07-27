﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VR;
using ROSBridgeLib.sensor_msgs;
using ROSBridgeLib;
using ROSBridgeLib.custom_msgs;

/// <summary>
/// BeRoboymanager has different tasks to do:
///
///     -# Keep track of user movement and translate roboy when in specific view modes
///     -# Convert received images into textures which can then be rendered on screen
///     -# FUTURE: Send tracking messages over the rosbridge to gazebo/ real roboy
/// </summary>
public class BeRoboyManager : Singleton<BeRoboyManager> {


    #region PUBLIC_MEMBER_VARIABLES

    /// <summary>
    /// Set whether head movement should be tracked or not.
    /// </summary>
    public bool TrackingEnabled = false;

    /// <summary>
    /// Reference to the render texture in which the Zed feed gets pushed into.
    /// </summary>
    public RenderTexture RT_Zed;

    /// <summary>
    /// Reference to the render texture in which the Simulation feed gets pushed into.
    /// </summary>
    public RenderTexture RT_Simulation;

    #endregion PUBLIC_MEMBER_VARIABLES

    #region PRIVATE_MEMBER_VARIABLES

    /// <summary>
    /// The HMD main camera.
    /// </summary>
    [SerializeField]
    private GameObject m_Cam;

    /// <summary>
    /// Texture in which the received simulation images get drawn.
    /// </summary>
    private Texture2D m_TexSim;

    /// <summary>
    /// Texture in which the received zed images get drawn.
    /// </summary>
    private Texture2D m_TexZed;

    /// <summary>
    /// Is the main camera initialized or not.
    /// </summary>
    private bool m_CamInitialized = false;

    /// <summary>
    /// Variable to determine if headset was rotated.
    /// </summary>
    private float m_CurrentAngleY = 0.0f;

    private float m_CurrentAngle = 0.0f;

    /// <summary>
    /// Variable to determine if headset was rotated.
    /// </summary>
    private float m_CurrentAngleX = 0.0f;

    /// <summary>
    /// Color array for the simulation image conversion.
    /// </summary>
    private Color[] m_ColorArraySim = new Color[640 * 480];

    /// <summary>
    /// Color array for the zed image conversion.
    /// </summary>
    private Color[] m_ColorArrayZed = new Color[1280 * 720];

    #endregion PRIVATE_MEMBER_VARIABLES

    #region MONOBEHAVIOR_METHODS

    /// <summary>
    /// Initialize textures.
    /// </summary>
    void Awake()
    {
        m_TexSim = new Texture2D(640, 480);
        m_TexZed = new Texture2D(1280, 720);
    }


    void Start () {

        //Looking for the HMD camera in scene
        if (!m_CamInitialized)
        {
            tryInitializeCamera();
        }
        else
        {
            Debug.Log("No Camera found!");
        }
       
    }
	
	void Update () {

        // Looking for the HMD camera in scene.
        if (!m_CamInitialized)
        {
            tryInitializeCamera();
        }
        // If the camera is found, move and rotate Roboy accordingly.
        else
        {
            if(TrackingEnabled)
                translateRoboy();
        }

    }
    #endregion //MONOBEHAVIOR_METHODS


    #region PUBLIC_METHODS

    /// <summary>
    /// Primary function to receive image (zed) messages from ROSBridge. Renders the received images.
    /// </summary>
    /// <param name="msg">JSON msg containing roboy pose.</param>
    public void ReceiveZedMessage(ImageMsg image)
    {
        RefreshZedImage(image);
    }

    /// <summary>
    /// Primary function to receive image (simulation) messages from ROSBridge. Renders the received images.
    /// </summary>
    /// <param name="msg">JSON msg containing roboy pose.</param>
    public void ReceiveSimMessage(ImageMsg image)
    {
        RefreshSimImage(image);
    }

    public void ReceiveExternalJoint(List<string> jointNames, List<float> angles)
    {
        ROSBridgeLib.custom_msgs.ExternalJointMsg msg =
            new ROSBridgeLib.custom_msgs.ExternalJointMsg(jointNames, angles);

        ROSBridge.Instance.Publish(RoboyHeadPublisher.GetMessageTopic(), msg);
    }

    #endregion PUBLIC_METHODS


    #region PRIVATE_METHODS

    /// <summary>
    /// Renders the received images from the zed camera
    /// </summary>
    /// <param name="msg">JSON msg containing the roboy pose.</param>
    private void RefreshZedImage(ImageMsg image)
    {
        //Get the image as an array from the message.
        byte[] image_temp = image.GetImage();

        int j = 0;
        for (int i = 0; i < image_temp.Length; i += 3)
        {
            m_ColorArrayZed[j].b = image_temp[i] / (float)255;
            m_ColorArrayZed[j].g = image_temp[i + 1] / (float)255;
            m_ColorArrayZed[j].r = image_temp[i + 2] / (float)255;

            m_ColorArrayZed[j].a = 1f;
            j++;
        }

        // Load data into the texture.
        m_TexZed.SetPixels(m_ColorArrayZed);
        m_TexZed.Apply();

        Graphics.Blit(m_TexZed, RT_Zed);
    }

    /// <summary>
    /// Renders the received images from the simulation.
    /// </summary>
    /// <param name="msg">JSON msg containing the roboy pose.</param>
    private void RefreshSimImage(ImageMsg image)
    {
        
        // Get the image as an array from the message.
        byte[] image_temp = image.GetImage();
        

        int j = 0;
        for (int i = 0; i < image_temp.Length; i += 3)
        {
            m_ColorArraySim[j].r = image_temp[i] / (float)255;
            m_ColorArraySim[j].g = image_temp[i + 1] / (float)255;
            m_ColorArraySim[j].b = image_temp[i + 2] / (float)255;

            m_ColorArraySim[j].a = 1f;
            j++;
        }

        // Load data into the texture.
        m_TexSim.SetPixels(m_ColorArraySim);
        m_TexSim.Apply();

        Graphics.Blit(m_TexSim, RT_Simulation);
    }

    /// <summary>
    /// Turn Roboy with the movement of the HMD.
    /// </summary>
    private void translateRoboy()
    {
        Transform head_parent = transform.GetChild(0).Find("head");
        Transform head_pivot = head_parent.GetChild(0);
        Transform torso_parent = transform.GetChild(0).Find("torso");
        Transform torso_pivot = torso_parent.GetChild(0);

        // Check whether the user has rotated the headset or not
        if (m_CurrentAngleY != m_Cam.transform.eulerAngles.y)
        {
            // If the headset was rotated, rotate roboy
            head_parent.RotateAround(head_pivot.position, Vector3.up, m_Cam.transform.eulerAngles.y - m_CurrentAngleY);
            //transform.RotateAround(m_Cam.transform.localPosition, Vector3.up, m_Cam.transform.eulerAngles.y - m_CurrentAngle);

        }
        m_CurrentAngleY = m_Cam.transform.eulerAngles.y;

        // Check whether the user has rotated the headset or not
        if (m_CurrentAngleX != m_Cam.transform.eulerAngles.x)
        {
            // If the headset was rotated, rotate roboy
            head_parent.RotateAround(head_pivot.position, Vector3.right, m_Cam.transform.eulerAngles.x - m_CurrentAngleX);
            //transform.RotateAround(m_Cam.transform.localPosition, Vector3.up, m_Cam.transform.eulerAngles.y - m_CurrentAngle);

        }
        m_CurrentAngleX = m_Cam.transform.eulerAngles.x;



        // Move roboy accordingly to headset movement
        Quaternion headRotation = InputTracking.GetLocalRotation(VRNode.Head);
        //transform.position = m_Cam.transform.position + (headRotation * Vector3.forward) * (-0.3f);

        
        Vector3 dir = InputTracking.GetLocalPosition(VRNode.RightHand) - torso_parent.position;
        float angle = Vector3.Angle(torso_parent.forward, dir);
        if (m_CurrentAngle != angle)
        {
            Debug.Log("Moved controller! "+ (m_CurrentAngle - angle ));
            torso_parent.RotateAround(torso_pivot.position, Vector3.up, (m_CurrentAngle - angle)*10);
        }

        m_CurrentAngle = angle;

        //torso_parent.RotateAround(torso_pivot.position, Vector3.up, InputTracking.GetLocalRotation(VRNode.RightHand).eulerAngles.z);

        //Convert the headset rotation from unity coordinate spaze to gazebo coordinates
        Quaternion rot = GazeboUtility.UnityRotationToGazebo(InputTracking.GetLocalRotation(VRNode.Head));
        float x_angle = 0.0f;
        float y_angle = 0.0f;

        if (rot.eulerAngles.x > 180)
        {
            y_angle = (rot.eulerAngles.x - 360) * Mathf.Deg2Rad;
        }
        else
        {
            y_angle = rot.eulerAngles.x * Mathf.Deg2Rad;
        }

        x_angle = rot.eulerAngles.z * Mathf.Deg2Rad;
        
        //Determine which joints should me modified
        List<string> joints = new List<string>();
        joints.Add("neck_3");
        joints.Add("neck_4");

        //Determine the angle for the joints
        List<float> angles = new List<float>();
        angles.Add(x_angle);
        angles.Add(y_angle);
        
        //Start sending the actual message
        ReceiveExternalJoint(joints, angles);
    }

    /// <summary>
    /// Looking for the main camera in the scene, which can be attached to Roboy.
    /// </summary>
    private void tryInitializeCamera()
    {
        // Look for a camera and initialize it.
        m_Cam = GameObject.FindGameObjectWithTag("MainCamera");
        if (m_Cam != null)
            m_CamInitialized = true;
    }

    #endregion PRIVATE_METHODS
}