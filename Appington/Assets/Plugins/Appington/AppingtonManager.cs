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


	void OnApplicationPause( bool didPause )
	{
		Appington.onApplicationPause( didPause );
	}


	public void onEventOccurred( string param )
	{
		// extract the message which will have a name and a hashtable of values
		var dict = param.dictionaryFromJson();
		var eventName = dict.ContainsKey( "name" ) ? dict["name"].ToString() : "Unknown";
		var values = dict["values"] as Dictionary<string,object>;

		if( onEvent != null )
			onEvent( eventName, values );

		handleEvent( eventName, values );
	}


	// Automatically handles ducking audio
	private void handleEvent( string eventName, Dictionary<string,object> values )
	{
		if( eventName == "audio_start" )
		{
			// duck audio
			if( values.ContainsKey( "lowered_volume" ) )
			{
				_initialVolume = AudioListener.volume;
				AudioListener.volume = float.Parse( values["lowered_volume"].ToString() ) / 100.0f;
			}
		}
		else if( eventName == "audio_end" )
		{
			// restore audio volume
			AudioListener.volume = _initialVolume;
		}
	}


}
