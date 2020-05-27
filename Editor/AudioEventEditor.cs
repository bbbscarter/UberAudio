using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Collections;

namespace UberAudio
{
    [CustomEditor(typeof(AudioEventBank))]
    public class AudioEventEditor : Editor {
	 
        const int PlayButtonWidth = 20;
        const int FieldButtonWidth = 30;
        const int TopButtonHeight = 30;

        GameObject AudioPlayer;

        AudioEventBank Bank;
        int IndexToClone;
        int IndexToDelete;
        string LastEventName = "";
        Vector2 ScrollPos;
        bool CheckEventAdjacency = false;

        //Keep track of what's been folded out
        Dictionary<int, bool> FoldedOut = new Dictionary<int, bool>();

        //Helper class for dealing with drag/drop
        class DropArea
        {
            public Rect Area;
            public int Index;

            public DropArea(int index, Rect area)
            {
                Area = area;
                Index = index;
            }
        }

        //List of active drag/drop areas
        List<DropArea> DropAreas = new List<DropArea>();

        //For showing details on audio events, we create proxy audio sources - they live here.
        GameObject AudioSourceProxyGO;
        Dictionary<AudioEvent, AudioSource> EventToSourceMap = new Dictionary<AudioEvent, AudioSource>();

        
        void Awake()
        {
            Bank=(AudioEventBank)target;
            if(Bank.AudioEvents==null)
            {
                Bank.AudioEvents = new List<AudioEvent>();
            }
        }

        void OnDisable()
        {
            Cleanup();
        }

        //Cleanup our temporary game objects
        void Cleanup()
        {
            if(AudioPlayer)
            {
                AudioPlayer.GetComponent<AudioSource>().Stop();
                GameObject.DestroyImmediate(AudioPlayer);
            }
            if(AudioSourceProxyGO!=null)
            {
                GameObject.DestroyImmediate(AudioSourceProxyGO);
            }
            EventToSourceMap.Clear();
            FoldedOut.Clear();
        }

        //Create a temporary game object for handling our in-editor audio playback
        void CreateAudioPlayer()
        {
            if(AudioPlayer==null)
            {
                AudioPlayer = new GameObject();
                AudioPlayer.hideFlags = HideFlags.HideAndDontSave;
                AudioPlayer.AddComponent<AudioSource>();
                AudioPlayer.transform.position = Vector3.zero;
                AudioPlayer.AddComponent<AudioListener>();
            }
        }

        //Move any event data into their audio source proxies
        void TransferSettingsToProxies()
        {
            if(AudioSourceProxyGO==null)
            {
                AudioSourceProxyGO = new GameObject();
                AudioSourceProxyGO.hideFlags = HideFlags.HideAndDontSave;
            }

            foreach(var ev in Bank.AudioEvents)
            {
                AudioSource audioSource = GetOrCreateAudioSourceProxy(ev);
                ev.TransferToAudioSource( audioSource);
            }
        }

        //If an audio source proxy doesn't exist for this event, create one
        //Regardless, return the proxy
        AudioSource GetOrCreateAudioSourceProxy(AudioEvent ev)
        {
            AudioSource audioSource = null;
            EventToSourceMap.TryGetValue(ev, out audioSource);
                
            if(audioSource==null)
            {
                audioSource = AudioSourceProxyGO.AddComponent<AudioSource>();
                EventToSourceMap[ev] = audioSource;
            }
            return audioSource;
        }

        //Root of all gui stuff
        public override void OnInspectorGUI()
        {
            IndexToClone = -1;
            IndexToDelete = -1;

            DropAreas.Clear();

		
            //Top buttons
            EditorGUILayout.BeginVertical();

            //Add a new audio event
            if(GUILayout.Button("Add entry...", GUILayout.Height(TopButtonHeight)))
            {
                var	newAudioEvent = new AudioEvent();
                newAudioEvent.EventName = "NEWKEY_" + Bank.AudioEvents.Count;
                Bank.AudioEvents.Add(newAudioEvent);
                var source = GetOrCreateAudioSourceProxy(newAudioEvent);
                newAudioEvent.TransferFromAudioSource(source);
            }
            
            //Stop all sounds from playing
            if(GUILayout.Button("Stop Playback", GUILayout.Height(TopButtonHeight)))
            {
                AudioPlayer.GetComponent<AudioSource>().Stop();
            }

            //Temporarily make our audio proxy sources visible, otherwise they are greyed out in the inspector
            if(AudioSourceProxyGO!=null)
            {
                AudioSourceProxyGO.hideFlags = HideFlags.DontSaveInEditor;
            }


            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            LastEventName = "";
            // ScrollPos = EditorGUILayout.BeginScrollView(ScrollPos);
            EditorGUILayout.BeginVertical();
        
            //Now display the individual events
            if(Bank != null)
            {
                TransferSettingsToProxies();
                if(Bank.AudioEvents!=null)
                {
                    for(int c1=0; c1<Bank.AudioEvents.Count; c1++)
                    {
                        DisplayLine(c1);
                    }
                }
            }
		
            //Clean up various bits of state we've had to keep going
            //If we've been asked to delete something, honour that
            if(IndexToDelete>=0)
            {
                Bank.AudioEvents.RemoveAt(IndexToDelete);
            }

            //If we've been asked to clone something, honour that
            if(IndexToClone>=0)
            {	
                var clone = Bank.AudioEvents[IndexToClone].Clone();
                Bank.AudioEvents.Insert(IndexToClone, clone);
            }

            //Handle drag drop logic
            DoDragDrop();
            
            // EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            EditorUtility.SetDirty(target);
            
            //Set the audio source proxies back to being invisible
            if(AudioSourceProxyGO!=null)
            {
                AudioSourceProxyGO.hideFlags = HideFlags.HideAndDontSave;
            }

            //Make sure same named events are adjacent
            if(CheckEventAdjacency)
            {
                CheckEventAdjacency = false;
                CleanupNonAdjacentEvents();
                GUI.FocusControl("");
            }

        }

        //Draw an audio source inline with our event editor
        //Used for showing/editing audio source proxies
        void DrawProxy(AudioEvent ev)
        {
            var source = EventToSourceMap[ev];
            var editor = Editor.CreateEditor(source);
            editor.OnInspectorGUI();
            Object.DestroyImmediate(editor);
        }


	
        //Draw a single audio event 
        public void DisplayLine(int audioEventIndex)
        {
            AudioEvent audioEvent = Bank.AudioEvents[audioEventIndex];

            //Visually seperate items from different groups
            if(audioEvent.EventName!=LastEventName)
            {
                LastEventName = audioEvent.EventName;
                EditorGUILayout.Space();
            }

            bool topLevelEvent = IsEventIndexTopOfGroup(audioEventIndex);

            //Draw all these controls on a single line
            EditorGUILayout.BeginHorizontal();

            //Drag handle
            Rect dropArea = GUILayoutUtility.GetRect(40, 20);
            dropArea.width = 30;
            if(topLevelEvent)
            {
                GUI.Box(dropArea, "");
                DropAreas.Add(new DropArea(audioEventIndex, dropArea));
            }

            //Details foldout
            bool foldedOut = false;
            if(FoldedOut.ContainsKey(audioEvent.GetHashCode()))
            {
                foldedOut = FoldedOut[audioEvent.GetHashCode()];
            }

            EditorGUIUtility.labelWidth = 20;
            EditorGUIUtility.fieldWidth = 1;

            foldedOut = EditorGUILayout.Foldout(foldedOut, "");
            FoldedOut[audioEvent.GetHashCode()] = foldedOut;

            //Show all the summary details and buttons
            EditorGUI.BeginChangeCheck();

            //Playback controls
            if (GUILayout.Button(new GUIContent("T", "Play this audio event"), GUILayout.MaxWidth(PlayButtonWidth)))
            // if (GUILayout.Button(new GUIContent("T", "Play this audio event"), GUILayout.ExpandWidth(false)))
            {
                PlayAudio(audioEventIndex, true);
            }

            if (GUILayout.Button(new GUIContent("R", "Play random event in this group"), GUILayout.MaxWidth(PlayButtonWidth)))
            {
                PlayAudio(audioEventIndex, false);
            }

            //Event name
            EditorGUIUtility.labelWidth = 20;
            EditorGUIUtility.fieldWidth = 100;
            GUI.SetNextControlName("TextField");
            audioEvent.EventName = EditorGUILayout.TextField(audioEvent.EventName);

            //If we've changed the event name, make sure we update any adjacency issues.
            if (Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "TextField")
            {
                CheckEventAdjacency = true;
            }
            
            //The audio clip
            EditorGUIUtility.labelWidth = 30;
            EditorGUIUtility.fieldWidth = 100;
            audioEvent.AudioClip = (AudioClip) EditorGUILayout.ObjectField("", audioEvent.AudioClip, typeof(AudioClip), false, GUILayout.MinWidth(60));

            //The random weight control
            EditorGUIUtility.labelWidth = 20;
            EditorGUIUtility.fieldWidth = 20;
            audioEvent.RandomWeight = EditorGUILayout.FloatField("Wgt", audioEvent.RandomWeight, GUILayout.MaxWidth (60));

            //Show the loop/lifetime mask
            DrawAudioEventOptionsMask(audioEvent);

            //Delete an item?
            if (GUILayout.Button("Dl", GUILayout.MaxWidth(25)))
            {
                IndexToDelete = audioEventIndex;
            }

            //Clone an item?
            if (GUILayout.Button("Cl", GUILayout.MaxWidth(FieldButtonWidth)))
            {
                IndexToClone = audioEventIndex;
            }

            // GUILayout.FlexibleSpace();

            //If any data was changed, transfer them to the proxy audio source
            if(EditorGUI.EndChangeCheck())
            {
                audioEvent.TransferToAudioSource(GetOrCreateAudioSourceProxy(audioEvent));
            }

            EditorGUILayout.EndHorizontal();	

            //If we're folded out, show the details from the proxy audio source
            if(foldedOut)
            {
                var source = EventToSourceMap[audioEvent];
                if(source!=null)
                {
                    EditorGUIUtility.labelWidth = 0;
                    EditorGUIUtility.fieldWidth = 0;
                    EditorGUI.BeginChangeCheck();
                    DrawProxy(audioEvent);
                    if(EditorGUI.EndChangeCheck())
                    {
                        audioEvent.TransferFromAudioSource(source);
                    }
                }
            }
        }

        class CustomDragData
        {
            public int Index;
        }

        //Handle drag drop of event groups.
        void DoDragDrop()
        {
            var currentEvent = Event.current;

            if(currentEvent.type==EventType.DragExited)
            {
                DragAndDrop.PrepareStartDrag();
            }

            var dragArea = DropAreas.Find(a=>a.Area.Contains(currentEvent.mousePosition));
            var dragData = DragAndDrop.GetGenericData("DragItem") as CustomDragData;

            switch(currentEvent.type)
            {
                case EventType.MouseDown:
                    if(dragArea!=null)
                    {
                        DragAndDrop.PrepareStartDrag();
                        var customDragData = new CustomDragData();
                        customDragData.Index = dragArea.Index;
                        DragAndDrop.SetGenericData("DragItem", customDragData);
                        Object[] objectRefs = new Object[0];
                        DragAndDrop.objectReferences = objectRefs;
                    }
                    currentEvent.Use();
                    break;
                    
                case EventType.MouseDrag:
                    if(dragData!=null)
                    {
                        FoldedOut.Clear();
                        DragAndDrop.StartDrag(Bank.AudioEvents[dragData.Index].EventName);
                    }
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                    currentEvent.Use();
                    break;
                case EventType.DragUpdated:
                    if(dragData!=null && dragArea!=null)
                    {
                        if(dragArea.Index!=dragData.Index)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                        }
                        else
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
                        }
                    }
                    currentEvent.Use();
                    break;
                case EventType.Repaint:
                    if(dragData!=null && dragArea!=null)
                    {
                        if(DragAndDrop.visualMode==DragAndDropVisualMode.Move)
                        {
                            EditorGUI.DrawRect(dragArea.Area, Color.white);
                        }
                    }
                    // currentEvent.Use();
                    break;
                case EventType.DragPerform:
                    if(dragData!=null && dragArea!=null)
                    {
                        if(dragArea.Index!=dragData.Index)
                        {
                            InsertGroupBefore(dragData.Index, dragArea.Index);
                            DragAndDrop.AcceptDrag();
                        }
                    }

                    currentEvent.Use();
                    break;

                case EventType.DragExited:
                    currentEvent.Use();
                    break;
            }
        }

        //Convert some bools into flags, and back again
        void DrawAudioEventOptionsMask(AudioEvent audioEvent)
        {
            var names = new string[] {"Loop", "Stop playing immediately when source dies", "Keep looping when source dies", "Do not Track source movement"};
            int mask = 0;
            if(audioEvent.Loop) mask |= 1<<0;
            if(audioEvent.StopWhenSourceDies) mask |= 1<<1;
            if(audioEvent.KeepLoopingWhenSourceDies) mask |= 1<<2;
            if(audioEvent.DoNotTrackSourceMovement) mask |= 1<<3;

            mask = EditorGUILayout.MaskField("", mask, names, GUILayout.MinWidth(100));

            audioEvent.Loop = (mask & (1<<0))>0;
            audioEvent.StopWhenSourceDies = (mask & 1<<1)>0;
            audioEvent.KeepLoopingWhenSourceDies = (mask & 1<<2)>0;
            audioEvent.DoNotTrackSourceMovement = (mask & 1<<3)>0;
        }

        //Is this event at the top of its group?
        bool IsEventIndexTopOfGroup(int audioEventIndex)
        {
            var thisEv = Bank.AudioEvents[audioEventIndex];

            return (audioEventIndex-1<0 || Bank.AudioEvents[audioEventIndex-1].EventName!=thisEv.EventName);
        }

        //Moves an event group before another event group
        void InsertGroupBefore(int groupStartIndex, int destinationStartIndex)
        {
            var groupName = Bank.AudioEvents[groupStartIndex].EventName;
            var objectsToMove = Bank.AudioEvents.FindAll(o=>o.EventName==groupName);
            var destinationStartObject = Bank.AudioEvents[destinationStartIndex];

            Bank.AudioEvents.RemoveAll(o=>o.EventName==groupName);
            var newDestinationStartIndex = Bank.AudioEvents.IndexOf(destinationStartObject);
            Bank.AudioEvents.InsertRange(newDestinationStartIndex, objectsToMove);
        }

        //If there are events that are by themselves, which should be with other members of their group, move them.
        void CleanupNonAdjacentEvents()
        {
            var eventsToRehome = new List<AudioEvent>();
            for(int c1=Bank.AudioEvents.Count-1; c1>=0; c1--)
            {
                var thisEv = Bank.AudioEvents[c1];
                bool adjacentToSameName = ((c1+1<Bank.AudioEvents.Count && Bank.AudioEvents[c1+1].EventName==Bank.AudioEvents[c1].EventName) ||
                                           (c1-1>=0 && Bank.AudioEvents[c1-1].EventName==Bank.AudioEvents[c1].EventName));
                if(!adjacentToSameName)
                {
                    if(Bank.AudioEvents.Find(ev=>ev!=thisEv && thisEv.EventName == ev.EventName)!=null)
                    {
                        eventsToRehome.Add(thisEv);
                    }
                }
            }
            foreach(var ev in eventsToRehome)
            {
                Bank.AudioEvents.Remove(ev);
                Bank.AudioEvents.Insert(Bank.AudioEvents.FindLastIndex(e=>e.EventName==ev.EventName)+1, ev);
            }
        }


        //Play an audio event.
        //If onlythisOne is false, it chooses a random weighted event from the group
        void PlayAudio(int index, bool onlyThisOne)
        {
            if(Application.isPlaying)
            {
                return;
            }

            if(AudioPlayer)
            {
                AudioPlayer.GetComponent<AudioSource>().Stop();
            }
            else
            {
                CreateAudioPlayer();
            }

            AudioEvent audioEvent = null;

            if(onlyThisOne)
            {
                audioEvent = Bank.AudioEvents[index];
            }
            else
            {
                var audioEventName = Bank.AudioEvents[index].EventName;
                List<AudioEvent> audioEvents = new List<AudioEvent>();

                foreach(var evt in Bank.AudioEvents)
                {
                    if(evt.EventName==audioEventName)
                    {
                        audioEvents.Add(evt);
                    }
                }

                audioEvent = AudioManager.GetRandomEvent(audioEvents);
            }
        
            if(audioEvent!=null)
            {
                var source = AudioPlayer.GetComponent<AudioSource>();

                audioEvent.TransferToAudioSource(source);
                source.Play();
            }
        }
    }
}
