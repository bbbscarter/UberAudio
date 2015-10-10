using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

namespace UberAudio
{
    /// <summary>
    /// An audio emitter is UberAudio's version of an AudioSource.
    /// AudioManager.Play() returns an AudioEmitter GameObject
    /// AudioEmitters automatically track the life, position, etc of the GameObject they're attached to.
    /// </summary>
    public class AudioEmitter : MonoBehaviour
    {
        Transform AttachedTo;
        AudioSource _Source;
        AudioEvent AudioSoundEvent;


        /// <summary>
        /// Update the emitter
        /// </summary>
        public void LateUpdate()
        {
            if(_Source==null)
            {
                return;
            }
        
            //Unless the emitter is flagged to stay still, track the gameobject we're attached to
            if (!AudioSoundEvent.DoNotTrackSourceMovement)
            {
                if (AttachedTo != null && AttachedTo.hasChanged)
                {
                    transform.position = AttachedTo.position;
                }
            }

            //If we're a looping sound and our parent has died, release the loop.
            if(_Source.loop)
            {
                if(AttachedTo==null && !AudioSoundEvent.KeepLoopingWhenSourceDies)
                {
                    _Source.loop = false;
                }
            }

            //If attached object is dead and the emitter is flagged to die if so, stop playing
            if (AudioSoundEvent.StopWhenSourceDies && AttachedTo == null)
            {
                _Source.Stop();
            }
        }

        public void Play()
        {
            _Source.Play();
        }

        public AudioSource Source
        {
            get{ return _Source; }
        }


        public bool Finished
        {
            get
            {
                return _Source==null || !_Source.isPlaying;
            }
        }

        public static AudioEmitter Create(Transform attachedTo, AudioEvent ev)
        {
            GameObject go = new GameObject("AudioEmitter");
            AudioEmitter emitter = go.AddComponent<AudioEmitter>();
            emitter._Source = go.AddComponent<AudioSource>();

            go.transform.parent = AudioManager.Instance.gameObject.transform;
            go.transform.position = attachedTo.position;
            emitter.AttachedTo = attachedTo;
            emitter.AudioSoundEvent = ev;
            ev.TransferToAudioSource(emitter._Source);
            return emitter;
        }

        public void Stop()
        {
            if(_Source)
            {
                _Source.Stop();
                _Source = null;
            }
        }
    }
}
