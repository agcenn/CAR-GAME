using OriginalKartRacer.Vehicle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace OriginalKartRacer.EditorTools
{
    public static class VehiclePhysicsSetupWizard
    {
        [MenuItem("Neon Harbor/Setup Selected Vehicle Physics")]
        private static void Setup()
        {
            var selected=Selection.activeGameObject;
            if(selected==null){Debug.LogWarning("Select a vehicle root first.");return;}
            if(!selected.scene.IsValid()){Debug.LogWarning("Select a vehicle instance in the scene.");return;}
            var body=selected.GetComponent<Rigidbody>();if(body==null)body=Undo.AddComponent<Rigidbody>(selected);
            Undo.RecordObject(body,"Configure vehicle mass");body.mass=180;body.constraints=RigidbodyConstraints.None;
            if(selected.GetComponent<Collider>()==null)Undo.AddComponent<BoxCollider>(selected);
            var kart=selected.GetComponent<KartController>();if(kart==null)kart=Undo.AddComponent<KartController>(selected);
            Undo.RecordObject(kart,"Create four-wheel layout");kart.EnsureWheelLayout();
            if(selected.GetComponent<DriftNitroSystem>()==null)Undo.AddComponent<DriftNitroSystem>(selected);
            EditorUtility.SetDirty(kart);EditorSceneManager.MarkSceneDirty(selected.scene);
            Debug.Log("Four-wheel physics ready. Edit Wheels/localMount and radius to match your model. RaceManager enables controls after countdown.",selected);
        }
    }
}
