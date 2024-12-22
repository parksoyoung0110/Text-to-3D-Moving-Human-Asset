using UnityEngine;
using UnityEditor;
using System.IO;
using System.Diagnostics;

public class BoneImporter : EditorWindow
{
    // Blender 실행 파일 경로
    public const string BLENDER_EXEC = @"C:\Program Files\Blender Foundation\Blender 4.3\blender.exe";

    // Python 스크립트 경로 (txt 파일과 obj 파일을 Blender에서 처리하는 스크립트)
    public const string PYTHON_SCRIPT_PATH = "Assets/Editor/bone.py";

    private static string currentPath;
    private static string objPath;
    private static string destination;

    [MenuItem("Tools/Bone Loading...")]
    static void ImportBoneData()
    {
        // txt 파일 선택
        string path = EditorUtility.OpenFilePanel("Select rig.txt file...", "", "txt");
        if (path.Length != 0)
        {
            currentPath = path;

            // obj 파일 선택 (여기서 obj 파일도 같이 선택하도록 변경)
            objPath = EditorUtility.OpenFilePanel("Select OBJ file...", "", "obj");
            if (objPath.Length != 0)
            {
                // destination 경로 설정
                string dest = EditorUtility.SaveFilePanel("Select destination...", Application.dataPath, Path.GetFileNameWithoutExtension(path), "fbx");
                if (dest.Length != 0)
                {
                    // destination이 Unity 프로젝트 안에 있는지 확인
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

                // Blender 커맨드 구성 (Python 스크립트를 실행하는 명령어)
                string command = string.Format("-b --python \"{0}\" -- \"{1}\" \"{2}\" \"{3}\"", PYTHON_SCRIPT_PATH, path, objPath, dest);

                // Blender 프로세스 실행
                var processInfo = new ProcessStartInfo(BLENDER_EXEC, command)
                {
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                };

                var process = Process.Start(processInfo);
                process.EnableRaisingEvents = true;

                // Blender의 출력과 에러 로그 처리
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

                // Blender 프로세스 종료 대기
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

    // Blender 프로세스 종료 후 처리
    private static void Process_Exited(object sender, System.EventArgs e)
    {
        string newFile = string.Format(@"{0}\{1}.fbx", Path.GetDirectoryName(currentPath), Path.GetFileName(currentPath));
        if (File.Exists(newFile))
        {
            // FBX 파일을 Unity의 destination으로 복사
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
