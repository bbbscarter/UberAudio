using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.IO;
 
namespace UberAudio
{
    //Menu option for creating a new audio manager
    public class AudioManagerCreator
    {
        [MenuItem("Assets/Uber Audio/Create Audio Manager")]
        public static void CreateAudioManager()
        {
            var audioGO = new GameObject();
            audioGO.AddComponent<AudioManager>();
            audioGO.name = "UberAudioManager";
        }

        [MenuItem("Assets/Uber Audio/Create Audio Bank Mounter")]
        public static void CreateAudioBankMounter()
        {
            var audioGO = new GameObject();
            audioGO.AddComponent<AudioBankMounter>();
            audioGO.name = "UberAudio BankMounter";
        }

    }

    //Wizard for creating a new audio bank
    public class AudioEventWizard :ScriptableWizard
    {
        public string BankName = "AudioBank";
        [MenuItem("Assets/Uber Audio/Create Audio Bank")]
        public static void CreateLevelAsset()
        {
            ScriptableWizard.DisplayWizard<AudioEventWizard>("Enter name new Audio event database", "Create");
		
        }
	
        void OnWizardCreate()
        {
            string path = "Assets";

            foreach (UnityEngine.Object obj in Selection.GetFiltered(typeof(UnityEngine.Object), SelectionMode.Assets))
            {
                path = AssetDatabase.GetAssetPath(obj);
                if (File.Exists(path))
                {
                    path = Path.GetDirectoryName(path);
                }
                break;
            }
            AudioEventBank asset = ScriptableObject.CreateInstance<AudioEventBank>();
            asset.AudioEvents = new List<AudioEvent>();
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, BankName+".asset")));
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;		
        }
    }

}

