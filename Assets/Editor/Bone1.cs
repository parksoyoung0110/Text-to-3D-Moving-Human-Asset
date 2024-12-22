using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class BoneImporter : EditorWindow
{
    // Blender ���� ���� ���
    public const string BLENDER_EXEC = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

    // Python ��ũ��Ʈ ��� (txt ���ϰ� obj ������ Blender���� ó���ϴ� ��ũ��Ʈ)
    public const string PYTHON_SCRIPT_PATH = "Assets/Editor/bone.py";

    private static string currentPath;
    private static string objPath;
    private static string destination;

    [MenuItem("Tools/Bone Loading...")]
    static void ImportBoneData()
    {
        // txt ���� ����
        string path = EditorUtility.OpenFilePanel("Select rig.txt file...", "", "txt");
        if (path.Length != 0)
        {
            currentPath = path;

            // obj ���� ���� (���⼭ obj ���ϵ� ���� �����ϵ��� ����)
            objPath = EditorUtility.OpenFilePanel("Select OBJ file...", "", "obj");
            if (objPath.Length != 0)
            {
                // destination ��� ����
                string dest = EditorUtility.SaveFilePanel("Select destination...", Application.dataPath, Path.GetFileNameWithoutExtension(path), "fbx");
                if (dest.Length != 0)
                {
                    // destination�� Unity ������Ʈ �ȿ� �ִ��� Ȯ��
                    if (dest.Contains(Application.dataPath))
                    {
                        destination = "Assets" + dest.Split(new string[] { Application.dataPath }, System.StringSplitOptions.None)[1];
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "You must select a folder inside the Unity project.", "Close");
                        currentPath = string.Empty;
                        objPath = string.Empty;
                        destination = string.Empty;
                        return;
                    }
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "You must select a valid destination folder inside the current project.", "Close");
                    currentPath = string.Empty;
                    objPath = string.Empty;
                    destination = string.Empty;
                    return;
                }

                // Blender Ŀ�ǵ� ���� (Python ��ũ��Ʈ�� �����ϴ� ��ɾ�)
                string command = string.Format("-b --python \"{0}\" -- \"{1}\" \"{2}\" \"{3}\"", PYTHON_SCRIPT_PATH, path, objPath, dest);

                // Blender ���μ��� ����
                var processInfo = new ProcessStartInfo(BLENDER_EXEC, command)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                var process = Process.Start(processInfo);
                process.EnableRaisingEvents = true;

                // Blender�� ��°� ���� �α� ó��
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

                // Blender ���μ��� ���� ���
                process.WaitForExit();
                process.Close();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "You must select a valid OBJ file.", "Close");
                currentPath = string.Empty;
                objPath = string.Empty;
                destination = string.Empty;
            }
        }
        else
        {
            currentPath = string.Empty;
            objPath = string.Empty;
            destination = string.Empty;
        }
    }

    // Blender ���μ��� ���� �� ó��
    private static void Process_Exited(object sender, System.EventArgs e)
    {
        string newFile = string.Format(@"{0}\{1}.fbx", Path.GetDirectoryName(currentPath), Path.GetFileName(currentPath));
        if (File.Exists(newFile))
        {
            // FBX ������ Unity�� destination���� ����
            File.Copy(newFile, destination);
        }
        else
        {
            UnityEngine.Debug.LogError("Unable to create FBX file. Please check the source data and try again.");
            currentPath = string.Empty;
            objPath = string.Empty;
            destination = string.Empty;
        }
    }
}
