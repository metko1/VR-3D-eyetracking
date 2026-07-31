using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Valve.VR; // Essential for SteamVR 2.0

public class GazeLogger : MonoBehaviour
{
    [Header("References")]
    private StreamWriter writer;
    private SteamVR_Action_Pose poseAction;

    [Header("Settings")]
    public string fileName = "gaze_hits.csv";
    private string filePath;
    
    void Start()
    {
        // Initialize the Pose action from the default set
        // This replaces the need for Unity Input System bindings
        poseAction = SteamVR_Actions.default_Pose;
        
        if (poseAction == null)
        {
            Debug.LogError("SteamVR Pose action not found. Ensure you clicked 'Save and Generate' in Window > SteamVR Input.");
        }

        // Define file path in persistent data path
        filePath = Path.Combine(Application.persistentDataPath, fileName);
        Debug.Log($"Recording to: {filePath}");

        writer = new StreamWriter("gaze_hits.csv");
        writer.WriteLine("Time,Object,ObjX,ObjY,ObjZ,HeadPosX,HeadPosY,HeadPosZ,HeadRotX,HeadRotY,HeadRotZ");
    }

    public void LogHit(RaycastHit hit)
    {
	if (writer == null)
	{
    	  Debug.LogError("Writer ist NULL!");
    	  return;
	}

        if (poseAction == null || !poseAction.active)
        {
          Debug.LogError("No poseAction or not active");
          return;
        }
         
        // Get the HMD (Head) position and rotation
        // SteamVR_Input_Sources.Head specifically targets the headset
        Vector3 pos = poseAction.GetLocalPosition(SteamVR_Input_Sources.Head);
        Quaternion rot = poseAction.GetLocalRotation(SteamVR_Input_Sources.Head);

        writer.WriteLine(
            Time.time.ToString("F4") + ","
            + hit.collider.name + "," + hit.point.x + "," + hit.point.y + "," + hit.point.z + ","
            + pos.x + "," + pos.y + "," + pos.z + "," 
            + rot.x + "," + rot.y + "," + rot.z);
    }

    void OnApplicationQuit()
    {
        writer.Close();
    }
}