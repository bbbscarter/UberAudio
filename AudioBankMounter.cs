using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace UberAudio
{
    [AddComponentMenu("Assets/Uber Audio/Bank Mounter")]
    public class AudioBankMounter : MonoBehaviour
    {
        public List<string> BanksToMount = new List<string>();

        void Start()
        {
            if(AudioManager.Instance==null)
            {
                return;
            }
            foreach (var bankName in BanksToMount)
            {
                AudioManager.Instance.LoadEventBank(bankName);
            }
        }

        void OnDestroy()
        {
            if(AudioManager.Instance==null)
            {
                return;
            }

            foreach (var bankName in BanksToMount)
            {
                if(AudioManager.Instance)
                {
                    AudioManager.Instance.UnloadEventBank(bankName);
                }
            }
        }
    }
}
