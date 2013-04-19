using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class AppingtonManager : MonoBehaviour
{
	// Fired whenever an event from Appington is received
	public static event Action<string,Dictionary<string,object>> onEvent;
	
	private float _initialVolume = 1;


	void Awake()
	{
		// Set the GameObject name to the class name for easy access from native code
		gameObject.name = this.GetType().ToString();
		DontDestroyOnLoad( this );
	}


	public void onEventOccurred( string param )
	{
		// extract the message which will have a name and a hashtable of values
		var dict = param.dictionaryFromJson();
		var eventName = dict.ContainsKey( "name" ) ? dict["name"].ToString() : "Unknown";
		
		if( onEvent != null )
			onEvent( eventName, dict["values"] as Dictionary<string,object> );
		
		handleEvent( eventName );
	}
	
	
	// Automatically handles ducking audio
	private void handleEvent( string eventName )
	{
		if( eventName == "audio_start" )
		{
			// duck audio
			_initialVolume = AudioListener.volume;
			AudioListener.volume = 0.17f;
		}
		else if( eventName == "audio_end" )
		{
			// restore audio volume
			AudioListener.volume = _initialVolume;
		}
	}


}

