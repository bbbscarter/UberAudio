using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine.Audio;


namespace UberAudio
{

    //The core UberAudio manager singleton
    public class AudioManager : MonoBehaviour
    {
        static public AudioManager Instance
        {
            get; private set;
        }

        // Use to build an event name from segments
        // See 'Play' for details
        public static string MakeEvent(string s1, params object[] vars)
        {
            var outputString = s1.ToUpper();
        
            foreach(var obj in vars)
            {
                string t = obj as string;
                if(t!=null && t!="")
                {
                    outputString += AudioEventSeparator;
                    outputString += t.ToUpper();
                }
            }
        
            return outputString;
        }

        public bool LogMissingEvents = false;
        public bool LogAllEvents = false;
        public bool LogLookups = false;
        public bool LogBankLoads = true;
        public bool LogBankRefCounts = false;

        //Set up our singleton.
        public void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
            }
            else
            {
                Instance = this;
                name = "uberaudio_singleton";
                DontDestroyOnLoad(gameObject);
            }
        }
    
        // Support for music. Not finished
        public void PlayMusic(string eventName)
        {
            StopMusic();
            Instance.CurrentMusicEvent = eventName;
        }

        // Support for music. Not finished
        public void QueueMusic(string eventName)
        {
            Instance.MusicQueue.Enqueue(eventName);
        }

        // Support for music. Not finished
        public void StopMusic()
        {
            MusicQueue.Clear();

            if (MusicEmitter)
            {
                MusicEmitter.Stop();
                MusicEmitter = null;
            }        
        }
    
        public void Update()
        {
            // Update the music queue
            if(MusicQueue.Count>0)
            {
                if (string.IsNullOrEmpty(CurrentMusicEvent))
                {
                    PlayMusic(MusicQueue.Dequeue());
                }
            }
            
            //Delete any dead emitters
            LinkedListNode<AudioEmitter> currentNode = ActiveEmitters.First;
        
            while (currentNode != null)
            {
                if (currentNode.Value.Finished)
                {
                    var node = currentNode;
                    currentNode = currentNode.Next; 
                    GameObject.Destroy(node.Value.gameObject);
                    ActiveEmitters.Remove(node);
                }
                else
                {
                    currentNode = currentNode.Next;
                }
            }

#if UNITY_EDITOR
            if (LogBankRefCounts)
            {
                foreach(KeyValuePair<string, int> kvp in BankReferenceCounts)
                {
                    Debug.Log(kvp.Key.ToString() + ":" + kvp.Value.ToString());
                }
                LogBankRefCounts = false;
            }
#endif
        }

        // Play an audio event.
        //
        // audioEventName is an audio event name from an audio bank
        // Event names are made of segments, ordered right to left in increasing order of specialisation
        // Eg. 'FOOTSTEP', 'GRAVEL, FOOTSTEP', 'WOLF, GRAVEL, FOOTSTEP'
        // The engine will strip the specialisations off, left to right, until it finds a match
        // Use MakeEvent to construct these.
        //
        // source is a GameObject to attach to - if null, uses a global emitter, useful for 2D sounds
        //
        // If successful, returns an AudioEmitter attached to a new GameObject
        public AudioEmitter Play(string audioEventName, GameObject source = null)
        {
            if(string.IsNullOrEmpty(audioEventName))
            {
                return null;
            }

            //If we're not attaching audio to another game object, assume this is a 2D sound and attach it to this manager
            if(source==null)
            {
                source = this.gameObject;
            }

            if (LogAllEvents)
            {
                Debug.Log("AudioManager: Trying event '" + audioEventName + "'" + (source != null ? (" (source = '" + source.name + "')") : ""));
            }
        
            // Look up an event from this trigger
            AudioEvent audioEvent = GetSoundForEvent(audioEventName);
            if (audioEvent != null && audioEvent.AudioClip!=null)
            {
                var emitter = AudioEmitter.Create(source.transform, audioEvent);
                emitter.Play();
                ActiveEmitters.AddLast(emitter);
                return emitter;
            }
            else if (LogMissingEvents)
            {
                Debug.Log("$$$$Missing sound event '" + audioEventName + "' (source = '" + source.name + "')");
            }
            return null;
        }

        public void Stop(AudioEmitter emitter)
        {
            if(emitter!=null)
            {
                emitter.Stop();
            }
        }

        /// Recursively look up an event in the loaded banks, chopping bits off the audio event name until a match is found.
        /// If there are multiple AudioEvents with the same trigger, does weighted randomisation.
        AudioEvent GetSoundForEvent(string audioEventName)
        {
            if (LogLookups)
            {
                Debug.Log("$$$Looking for sound event " + audioEventName);
            }
        
            List<AudioEvent> events;
            if (AudioEvents.TryGetValue(audioEventName, out events))
            {
                AudioEvent audioEvent;

                //If we have more than one event, get a weighted, random one.
                if (events.Count > 1)
                {
                    audioEvent = GetRandomEvent(events);
                }
                else
                {
                    audioEvent = events[0];
                }

                if(LogLookups)
                {
                    Debug.Log("$$$Found sound event " + audioEventName);
                }

                return audioEvent;
            }
            else
            {
                var splitLocation = audioEventName.IndexOf(AudioEventSeparator);
                if (splitLocation >= 0 && splitLocation + 1 < audioEventName.Length)
                {
                    var newEv = audioEventName.Substring(splitLocation + 1);
                    return GetSoundForEvent(newEv);
                }
            }
            return null;
        }

        // Uses the event weightings to get a random event from a group
        public static AudioEvent GetRandomEvent(List<AudioEvent> events)
        {
            var totalWeight = 0.0f;
            events.ForEach(e => totalWeight += e.RandomWeight);

            var dieRoll = UnityEngine.Random.Range(0, totalWeight);
            var currentWeight = 0.0;

            foreach (var ev in events)
            {
                currentWeight += ev.RandomWeight;
                if (dieRoll < currentWeight)
                {
                    return ev;
                }
            }
            return null;
        }

        // Internal - Used by AudioBankMounter
        // If bankName is not loaded, loads it and sets the ref count to one
        // Otherwise it just increments the ref count
        public void LoadEventBank(string bankName)
        {
            if (BankReferenceCounts.ContainsKey(bankName))
            {
                BankReferenceCounts[bankName] += 1;
                return;
            }

            BankReferenceCounts[bankName] = 1;

            var bank = Resources.Load(bankName, typeof(AudioEventBank)) as AudioEventBank;
            if (bank == null)
            {
                Debug.Log("Failed to load audio bank " + bankName);
                return;
            }

            if (LogBankLoads)
            {
                Debug.Log("Loading bank " + bankName);
            }

        
            foreach (var se in bank.AudioEvents)
            {
                if (!AudioEvents.ContainsKey(se.EventName))
                {
                    AudioEvents[se.EventName] = new List<AudioEvent>();
                }
                se.BankName = bankName;
                AudioEvents[se.EventName].Add(se);
            }
        }

        // Internal - Used by AudioBankMounter
        // Decrements the reference count on the bank named bankName
        // If it hits zero, unloads it
        public void UnloadEventBank(string bankName)
        {
            if (BankReferenceCounts.ContainsKey(bankName))
            {
                BankReferenceCounts[bankName] -= 1;
                if (BankReferenceCounts[bankName] <= 0)
                {
                    BankReferenceCounts.Remove(bankName);

                    Debug.Log("AudioManager: Unload bank " + bankName);

                    var keysToDelete = new List<string>();
                    foreach (var kvp in AudioEvents)
                    {
                        kvp.Value.RemoveAll(e => e.BankName == bankName);
                        if (kvp.Value.Count == 0)
                        {
                            keysToDelete.Add(kvp.Key);
                        }
                    }

                    foreach (var k in keysToDelete)
                    {
                        AudioEvents.Remove(k);
                    }
                }
            }
        }

        Dictionary<string, List<AudioEvent>> AudioEvents = new Dictionary<string, List<AudioEvent>>();
        LinkedList<AudioEmitter> ActiveEmitters = new LinkedList<AudioEmitter>();
        Dictionary<string, int> BankReferenceCounts = new Dictionary<string, int>();

        AudioEmitter MusicEmitter;
        string CurrentMusicEvent;
        Queue<string> MusicQueue = new Queue<string>();

        //The seperator between removable parts of an event trigger
        //If you change this, you'll need to change your audio banks!
        static string AudioEventSeparator = ":";

    }
}
