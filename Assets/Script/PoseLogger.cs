using System;
using System.IO;
using UnityEngine;

// Attach this to a standalone GameObject to log its position and orientation
// every 0.5 seconds. Logs are written under the "script/log" folder relative to
// the project root.
public class PoseLogger : MonoBehaviour
{
    private StreamWriter writer;

    private void Start()
    {
        // Determine the path "script/log" relative to the project root
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string logDir = Path.Combine(projectRoot, "script", "log");
        Directory.CreateDirectory(logDir);

        string fileName = DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
        string filePath = Path.Combine(logDir, fileName);

        writer = new StreamWriter(filePath);
        InvokeRepeating(nameof(LogPose), 0f, 0.5f);
    }

    private void LogPose()
    {
        Vector3 pos = transform.position;
        Quaternion rot = transform.rotation;
        writer.WriteLine($"{Time.time:F2}, {pos.x:F3}, {pos.y:F3}, {pos.z:F3}, {rot.eulerAngles.x:F2}, {rot.eulerAngles.y:F2}, {rot.eulerAngles.z:F2}");
        writer.Flush();
    }

    private void OnDestroy()
    {
        if (writer != null)
        {
            writer.Flush();
            writer.Close();
        }
    }
}
