using System.Runtime.InteropServices;
using UnityEngine;

public class MinimizeGame : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    private const int SW_MINIMIZE = 6;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(
        System.IntPtr windowHandle,
        int command);

    [DllImport("user32.dll")]
    private static extern System.IntPtr GetActiveWindow();
#endif

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10))
            Minimize();
    }

    private static void Minimize()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        ShowWindow(GetActiveWindow(), SW_MINIMIZE);
#else
        Debug.Log("Minimize works in a Windows build.");
#endif
    }
}