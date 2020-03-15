using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

// An attribute to help with setting local path names of a prefab located in the resources folder.
// This only needs to be used for any prefab spawned by Photon.Instantiate since it requires a string
public class PhotonPrefab : PropertyAttribute
{
    public System.Type m_type;

    public PhotonPrefab(System.Type component = null)
    {
        m_type = component;
    }
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(PhotonPrefab))]
public class PhotonPrefabDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        PhotonPrefab photonPrefab = attribute as PhotonPrefab;

        if (property.propertyType == SerializedPropertyType.String)
        {
            EditorGUI.BeginChangeCheck();

            Object curAsset = Resources.Load(property.stringValue);
            Object newAsset = EditorGUI.ObjectField(position, label, curAsset, typeof(GameObject), false);

            if (newAsset != curAsset)
            {
                string assetPath = AssetDatabase.GetAssetPath(newAsset);

                // Photon only supports instantiating prefabs under the 'Resources' folder.
                // The name of the folder is hardset to 'Resources' (https://docs.unity3d.com/ScriptReference/Resources.html)
                int folderLoc = assetPath.IndexOf("Resources/");
                if (folderLoc >= 0)
                {
                    assetPath = assetPath.Substring(folderLoc + "Resources/".Length);

                    // Remove the .prefab suffix at end of selected object
                    assetPath = assetPath.Substring(0, assetPath.Length - (".prefab").Length);
                }
                else if (newAsset)
                {
                    Debug.LogWarning(string.Format("Unable to set Object {0} as it is not located in the resources folder", newAsset.name));
                }

                // Actually check if we could load this asset before setting it
                GameObject gameObject = Resources.Load(assetPath) as GameObject;
                if (gameObject)
                {
                    bool bCanSet = true;

                    if (photonPrefab.m_type != null)
                    {
                        bCanSet = gameObject.GetComponent(photonPrefab.m_type) != null;
                        if (!bCanSet)
                            Debug.LogWarning(string.Format("Unable to set Object {0} as it does not contain component of type {1}", newAsset.name, photonPrefab.m_type));
                    }

                    if (bCanSet)
                        if (EditorGUI.EndChangeCheck())
                            property.stringValue = assetPath;
                }
            }
        }
        else
        {
            base.OnGUI(position, property, label);
        }
    }
}
#endif
