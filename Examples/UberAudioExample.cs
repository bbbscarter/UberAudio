using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UberAudio;

public class UberAudioExample : MonoBehaviour
{
	class SoundGO
	{
		public float StartTime;
		public float LifeTime;
		public GameObject GO;
	}

	List<SoundGO> ActiveObjects = new List<SoundGO>();
	public GUIText InstructionText;
	public Transform StartPos;
	public Transform EndPos;
	public GameObject SoundObjectPrefab;
	public float TimeToTravel = 2;

	
	// Use this for initialization
	void Start ()
	{
		InstructionText.text =
			@"1 Play LaserShot
2 Play UnknownShot
3 Play Immediate Stop Loop
4 Play Release Loop
5 Play static Shot
0 Kill all";
	}
	
	// Update is called once per frame
	void Update ()
	{
		if(Input.GetKeyDown("1"))
		{
			var soundGO = CreateSoundGO();
			AudioManager.Instance.Play(AudioManager.MakeEvent("LASER", "SHOT"), soundGO.GO);
		}
		else if(Input.GetKeyDown("2"))
		{
			var soundGO = CreateSoundGO();
			AudioManager.Instance.Play(AudioManager.MakeEvent("UNKNOWN", "SHOT"), soundGO.GO);
		}
		else if(Input.GetKeyDown("3"))
		{
			var soundGO = CreateSoundGO();
			AudioManager.Instance.Play("LOOP", soundGO.GO);
		}
		else if(Input.GetKeyDown("4"))
		{
			var soundGO = CreateSoundGO();
			AudioManager.Instance.Play("DIE_RELEASE_LOOP", soundGO.GO);
		}
		else if(Input.GetKeyDown("5"))
		{
			var soundGO = CreateSoundGO();
			AudioManager.Instance.Play("STATIC_SHOT", soundGO.GO);
		}
		else if(Input.GetKeyDown("0"))
		{
			foreach(var go in ActiveObjects)
			{
				GameObject.Destroy(go.GO);
			}
			ActiveObjects.Clear();
		}
		UpdateActiveGOs();
	}

	SoundGO CreateSoundGO()
	{
		var go = GameObject.Instantiate(SoundObjectPrefab);
		// var go = new GameObject();
		var soundGO = new SoundGO();
		soundGO.StartTime = Time.time;
		soundGO.LifeTime = TimeToTravel;
		soundGO.GO = go;
		go.transform.position = StartPos.position;
		ActiveObjects.Add(soundGO);
		return soundGO;
	}

	void UpdateActiveGOs()
	{
		var currentTime = Time.time;
		foreach(var go in ActiveObjects)
		{
			if(go.StartTime+go.LifeTime<currentTime)
			{
				GameObject.Destroy(go.GO);
				go.GO = null;
			}
			else
			{
				go.GO.transform.position = Vector3.Lerp(StartPos.position, EndPos.position, (currentTime-go.StartTime)/(go.LifeTime));
			}
		}
		ActiveObjects.RemoveAll(go=>go.GO==null);
	}
}
