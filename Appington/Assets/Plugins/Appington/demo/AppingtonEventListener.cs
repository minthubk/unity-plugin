using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class AppingtonEventListener : MonoBehaviour
{
	void OnEnable()
	{
		// Listen to all events for illustration purposes
		AppingtonManager.onEvent += onEvent;
	}


	void OnDisable()
	{
		// Remove all event handlers
		AppingtonManager.onEvent -= onEvent;
	}


	void onEvent( string name, Dictionary<string,object> values )
	{
		Debug.Log( "onEvent: " + name );
		
		if( values != null )
		{
			foreach( var de in values )
				Debug.Log( de.Key + ": " + de.Value );
		}
	}
}


