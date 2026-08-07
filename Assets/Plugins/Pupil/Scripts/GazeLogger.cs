using System.IO;
//using System.Collections.Generic;
using UnityEngine;
//using Valve.VR; // Essential for SteamVR 2.0

public class GazeLogger : MonoBehaviour
{
    [Header("References")]
    private StreamWriter writer;

    [Tooltip("Assign the specific VR Camera (Head) object here, not the Rig")]
    public Transform headsetTransform;
    
    [Header("Settings")]
    public string fileName = "gaze_hits.csv";
    private string filePath;
    
    void Start()
    {
        // Auto-assign if not set: looks for the main camera or SteamVR camera
        if (headsetTransform == null)
        {
            headsetTransform = Camera.main?.transform;
            if (headsetTransform == null)
                Debug.LogError("[RecordHeadOrientation] No headset transform assigned and no Main Camera found!");
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

	if (headsetTransform == null)
	   return;
        Vector3 pos = headsetTransform.position;
        Vector3 rot = headsetTransform.eulerAngles;

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