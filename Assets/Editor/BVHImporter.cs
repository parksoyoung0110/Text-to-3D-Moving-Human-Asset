using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class BVHImporter : EditorWindow
{

    public const string BLENDER_EXEC = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";
    public const string PYTHON_SCRIPT_PATH = "Assets/Editor/bhv2fbx.py";



    private static string currentPath;
    private static string destination;

    [MenuItem("Tools/Import BVH...")]
    static void ImportBVH()
    {
        string path = EditorUtility.OpenFilePanel("Select BVH file...", "", "bvh");
        if (path.Length != 0)
        {
            currentPath = path;
            string dest = EditorUtility.SaveFilePanel("Select destination...", Application.dataPath, Path.GetFileNameWithoutExtension(path), "fbx");
            if (dest.Length != 0)
            {
                if (dest.Contains(Application.dataPath))
                {
                    destination = "Assets" + dest.Split(new string[] { Application.dataPath }, System.StringSplitOptions.None)[1];
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "You must select a folder inside the Unity project.", "Close");
                    currentPath = string.Empty;
                    destination = string.Empty;
                    return;
                }
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "You must select a valid destination folder inside the current project.", "Close");
                currentPath = string.Empty;
                destination = string.Empty;
                return;
            }
            string command = string.Format("-b --python \"{0}\" -- \"{1}\" \"{2}\"", PYTHON_SCRIPT_PATH, path, dest);

            var processInfo = new ProcessStartInfo(BLENDER_EXEC, command)
            {
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            var process = Process.Start(processInfo);
            process.EnableRaisingEvents = true;

            process.OutputDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.Log("[Blender Output]: " + e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrEmpty(e.Data))
                    UnityEngine.Debug.LogError("[Blender Error]: " + e.Data);
            };

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            process.WaitForExit();
            process.Close();
        }
        else
        {
            currentPath = string.Empty;
            destination = string.Empty;
        }
    }

    private static void Process_Exited(object sender, System.EventArgs e)
    {
        string newFile = string.Format(@"{0}\{1}.fbx", Path.GetDirectoryName(currentPath), Path.GetFileName(currentPath));
        if (File.Exists(newFile))
        {
            File.Copy(newFile, destination);
        }
        else
        {
            UnityEngine.Debug.LogError("Unable to create FBX file. Please check the source BVH and try again.");
            currentPath = string.Empty;
            destination = string.Empty;
        }


    }
}
