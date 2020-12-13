using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UberAudio
{
    [AddComponentMenu("Assets/Uber Audio/Bank Mounter")]
    public class AudioBankMounter : MonoBehaviour
    {
        public List<string> BanksToMount = new List<string>();

        private bool loaded = false;

        void Start()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogWarning("There is no UberAudio Manager found");
                return;
            }
            foreach (var bankName in BanksToMount)
            {
                AudioManager.Instance.LoadEventBank(bankName);
                loaded = true;
            }
        }

        void OnDestroy()
        {
            if (AudioManager.Instance == null)
            {
                return;
            }

            foreach (var bankName in BanksToMount)
            {
                if (AudioManager.Instance != null && loaded)
                {
                    AudioManager.Instance.UnloadEventBank(bankName);
                }
            }
        }
    }
}
