using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class DebugValues
{
    // Start is called before the first frame update
    static DebugValues()
    {
        Debug.Log("JVM info log");
        Debug.Log("HasKey? - " + EditorPrefs.HasKey("AndroidJVMMaxHeapSize"));
        Debug.Log("JVM Heap Size: " + EditorPrefs.GetInt("AndroidJVMMaxHeapSize"));
    }
}
