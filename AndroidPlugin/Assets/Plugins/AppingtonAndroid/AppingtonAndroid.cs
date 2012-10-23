using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class AppingtonAndroid : MonoBehaviour
{
#if UNITY_ANDROID
	private static AndroidJavaObject _plugin;
	
	
	private static AndroidJavaObject instance()
	{
		if( _plugin == null )
		{		
			// find the plugin instance
			using( var pluginClass = new AndroidJavaClass( "com.appington.AppingtonPlugin" ) )
				_plugin = pluginClass.CallStatic<AndroidJavaObject>( "instance" );
		}
		
		return _plugin;
	}
	
	
	#region Public API
	
	// Initializes the Appington SDK and prepares it for use
	public static void init()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		instance().Call( "init" );
	}


	// Sends a control message to the Appington SDK
	public static void control( string name, Dictionary<string,object> parameters )
	{
		if( Application.platform != RuntimePlatform.Android )
			return;
		
		instance().Call( "control", name, parameters != null ? parameters.toJson() : "{}" );
	}
	
	#endregion
	
	
	#region Private methods

	// Called automatically when Unity is paused
	private static void onPause()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;

		instance().Call( "onPause");
	}


	// Called automatically when Unity is unpaused
	private static void onResume()
	{
		if( Application.platform != RuntimePlatform.Android )
			return;

		instance().Call( "onResume");
	}
	
	#endregion
	
	
	#region Unity Lifecycle
	
	void OnApplicationPause( bool didPause )
	{
		if( didPause )
			onPause();
		else
			onResume();
	}
	
	#endregion
	
#endif
}
