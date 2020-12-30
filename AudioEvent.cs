using System;
using UnityEngine;
using UnityEngine.Audio;

    //And audio event, mapping a audio settings to a string event
    [Serializable]
    public class AudioEvent
    {
        public string EventName;
        public float RandomWeight = 1.0f;
        public float Volume = 1.0f;
        public AudioClip AudioClip = null;
        public int Priority = 128;
        public int GroupID = 0;
        public bool Loop = false;
        public bool StopWhenSourceDies = false;
        public bool KeepLoopingWhenSourceDies = false;
        public bool DoNotTrackSourceMovement = false;

        public bool BypassEffects = false;
        public bool BypassListenerEffects = false;
        public bool BypassReverbZones = false;
        public float DopplerLevel = 1.0f;
        public float MaxDistance = 500.0f;
        public float MinDistance = 1.0f;
        public bool Mute = false;
        public AudioMixerGroup OutputAudioMixerGroup = null;
        public float PanStereo = 0.0f;
        public float Pitch = 1.0f;
        public bool Spatialize = false;
        public AudioRolloffMode RolloffMode = AudioRolloffMode.Logarithmic;
        public AnimationCurve CustomRolloffCurve;
        public float SpatialBlend = 0.0f;
        public float ReverbZoneMix = 1.0f;

        [System.NonSerialized]
        public string BankName;

        //Used by the AudioEventEditor 'Clone'
        //Duplicates an AudioEvent.
        //TODO - Do this via reflection.
        public AudioEvent Clone()
        {
            var ev = new AudioEvent();
            ev.RandomWeight = RandomWeight;
            ev.Volume = Volume;
            ev.AudioClip = AudioClip;
            ev.EventName = EventName;
            ev.Loop = Loop;
            ev.StopWhenSourceDies = StopWhenSourceDies;
            ev.Priority = Priority;
            ev.GroupID = GroupID;
            ev.KeepLoopingWhenSourceDies = KeepLoopingWhenSourceDies;
            ev.DoNotTrackSourceMovement = DoNotTrackSourceMovement;

            ev.BypassEffects = BypassEffects;
            ev.BypassListenerEffects = BypassListenerEffects;
            ev.BypassReverbZones = BypassReverbZones;
            ev.DopplerLevel = DopplerLevel;
            ev.MaxDistance = MaxDistance;
            ev.MinDistance = MinDistance;
            ev.Mute = Mute;
            ev.OutputAudioMixerGroup = OutputAudioMixerGroup;
            ev.PanStereo = PanStereo;
            ev.Pitch = Pitch;
            ev.RolloffMode = RolloffMode;
            ev.CustomRolloffCurve = CustomRolloffCurve;
            ev.SpatialBlend = SpatialBlend;
            ev.ReverbZoneMix = ReverbZoneMix;
            ev.Spatialize = Spatialize;

            return ev;
        }

        //Transfers audio settings to an audio source
        public void TransferToAudioSource(AudioSource source)
        {
            source.loop = Loop;
            source.volume = Volume;
            source.clip = AudioClip;
            source.bypassEffects = BypassEffects;
            source.bypassListenerEffects = BypassListenerEffects;
            source.bypassReverbZones = BypassReverbZones;
            source.dopplerLevel = DopplerLevel;
            source.maxDistance = MaxDistance;
            source.minDistance = MinDistance;
            source.mute = Mute;
            source.outputAudioMixerGroup = OutputAudioMixerGroup;
            source.panStereo = PanStereo;
            source.pitch = Pitch;
            source.priority = Priority;
            source.rolloffMode = RolloffMode;
            source.spatialBlend = SpatialBlend;
            source.reverbZoneMix = ReverbZoneMix;
            source.spatialize = Spatialize;
            if(RolloffMode == AudioRolloffMode.Custom)
            {
                source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, CustomRolloffCurve);
            }
        }

        //Transfers audio settings from an AudioSource. Only used in the editor
        public void TransferFromAudioSource(AudioSource source)
        {
            Loop = source.loop;
            Volume = source.volume;
            AudioClip = source.clip;
            BypassEffects = source.bypassEffects;
            BypassListenerEffects = source.bypassListenerEffects;
            BypassReverbZones = source.bypassReverbZones;
            DopplerLevel = source.dopplerLevel;
            MaxDistance = source.maxDistance;
            MinDistance = source.minDistance;
            Mute = source.mute;
            OutputAudioMixerGroup = source.outputAudioMixerGroup;
            PanStereo = source.panStereo;
            Pitch = source.pitch;
            Priority = source.priority;
            RolloffMode = source.rolloffMode;
            SpatialBlend = source.spatialBlend;
            ReverbZoneMix = source.reverbZoneMix;
            Spatialize = source.spatialize;

            if(RolloffMode == AudioRolloffMode.Custom)
            {
                CustomRolloffCurve = source.GetCustomCurve(AudioSourceCurveType.CustomRolloff);
            }
            else
            {
                CustomRolloffCurve = null;
            }
        }

    }
