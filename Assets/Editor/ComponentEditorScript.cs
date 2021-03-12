using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;

public class ComponentEditorScript : EditorWindow
{
    static ComponentEditorScript instance;

    public GameObject componentRetrieveSource;
    private GameObject lastSource;

    private static bool[] componentIncludes;
    private static bool[] copyValues;

    // Create a new editor window and create a singleton of our window instance
    [MenuItem("Proposterous/Prop Network Components")]
    public static void ShowWindow()
    {
        instance = GetWindow<ComponentEditorScript>(false, "Network Components Tool", true);
    }

    // Window GUI internally called on repaint
    void OnGUI()
    {
        // Blank window if we've lost our singleton reference for some reason (avoids a wall of errors)
        if (instance)
        {
            // Base vertical layout
            EditorGUILayout.BeginVertical();

            // Source Object
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Reference Object: ");
            componentRetrieveSource = EditorGUILayout.ObjectField(componentRetrieveSource, typeof(GameObject), true) as GameObject;
            EditorGUILayout.EndHorizontal();

            // I have a reference object
            if (componentRetrieveSource)
            {
                // Component Selection

                Component[] components = componentRetrieveSource.GetComponents<Component>();

                // Re-init toggle array (checkboxes)
                if (lastSource != componentRetrieveSource || componentIncludes.Length != components.Length)
                {
                    componentIncludes = new bool[components.Length];
                    copyValues = new bool[components.Length];

                    for (int j = 0; j < components.Length; j++)
                    {
                        componentIncludes[j] = false;
                        copyValues[j] = false;
                    }
                }

                int i = 0;

                // Layout select check boxes
                foreach (Component original in components)
                {
                    Transform t = original as Transform;

                    // Not the transform component
                    if (t == null)
                    {
                        EditorGUILayout.BeginHorizontal();
                        componentIncludes[i] = GUILayout.Toggle(componentIncludes[i], original.GetType().ToString());
                        copyValues[i] = GUILayout.Toggle(copyValues[i], "Copy Values");
                        EditorGUILayout.EndHorizontal();

                    }

                    i++;
                }

                // Update the last check
                lastSource = componentRetrieveSource;

                // Commit button
                if (GUILayout.Button("Add Components"))
                {
                    // The target to apply our components to
                    GameObject target;
                    // The path for the base prefab
                    string prefabPath = ResolvePrefabPath(out target);


                    if (target)
                    {
                        //Make sure we add our prop to the PropInteraction layer. (11)
                        target.layer = 11;

                        // -------------------------< COPYING COMPONENTS >-------------------------


                        i = 0;
                        foreach (Component original in components)
                        {
                            // Was this component flagged for copy?
                            if (componentIncludes[i])
                            {
                                // Get the component type we are working with
                                System.Type type = original.GetType();

                                // Check for existing type on prefab
                                Component copy = target.GetComponent(type);

                                // If it doesn't exist, add it
                                if (copy == null)
                                {
                                    copy = target.AddComponent(type);
                                }

                                // Use internal utilities to compy the component values (be aware this copies based on source)
                                // This might not be what we want always (i.e local component refs are not relative)
                                if (copyValues[i])
                                {
                                    if (UnityEditorInternal.ComponentUtility.CopyComponent(original))
                                    {
                                        if (UnityEditorInternal.ComponentUtility.PasteComponentValues(copy))
                                        {
                                            Debug.Log("Successfully copied" + original.GetType().ToString() + " to target.");
                                        }
                                    }
                                }
                            }

                            i++;
                        }


                        // -------------------------</ COPYING COMPONENTS >-------------------------

                    }
                }
            }
            EditorGUILayout.EndVertical();
        }
    }

    // Based on the current unity project conditions, resolve a prefab path to update
    private string ResolvePrefabPath(out GameObject targetAdd)
    {
        string prefabPath = "";
        GameObject outAdd = null;

        // Be sure we have an object selected
        if (Selection.activeObject is UnityEngine.Object)
        {
            // We have selected an object in the project window, we may assume it's the root
            if (Selection.assetGUIDs.Length > 0)
            {

                UnityEngine.GameObject test = Selection.activeObject as UnityEngine.GameObject;

                // Make sure this object is a prefab and not a random asset
                if (test != null)
                {
                    // Pull the path directly from the selection
                    prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(test);
                    outAdd = test;
                }
            }
            else
            {
                // We have selected a scene object, check if main scene or prefab stage
                var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
                if (prefabStage != null)
                {
                    GameObject rootAsGO = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;

                    // Pull the path from the stage
                    prefabPath = prefabStage.assetPath;
                    outAdd = Selection.activeObject as UnityEngine.GameObject;
                }
                else
                {
                    // We have selected an object in the scene view, we must find the root
                    if (Selection.activeObject is UnityEngine.GameObject)
                    {
                        GameObject asGO = Selection.activeObject as GameObject;

                        // Pull the path from the scene root in the world
                        prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(asGO);
                        outAdd = asGO;
                    }
                }
            }
        }
        else
        {
            // We have no current selection accross any unity windows, check if prefab stage is open
            var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
            if (prefabStage != null)
            {
                GameObject rootAsGO = PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot;

                // Pull the path from the stage
                prefabPath = prefabStage.assetPath;
                outAdd = rootAsGO;
            }
        }

        targetAdd = outAdd;
        return prefabPath;
    }
}
