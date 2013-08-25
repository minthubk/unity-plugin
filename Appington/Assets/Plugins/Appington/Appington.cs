using UnityEngine;
using System.Runtime.InteropServices;
using System.Collections.Generic;


public class Appington
{
	#pragma warning disable 0649
	private static IAppington _instance;
	#pragma warning restore 0649

	private static IAppington instance()
	{
		if( _instance == null )
		{
#if UNITY_IPHONE
			_instance = new iOSAppington();
#elif UNITY_ANDROID
			_instance = new AndroidAppington();
#endif
		}
		return _instance;
	}



	#region Public API

	// Initializes the Appington SDK and prepares it for use
	public static void init(string api_token)
	{
		instance().init(api_token);
	}


	// Sends a control message to the Appington SDK
	public static void control( string name, Dictionary<string,object> parameters )
	{
		instance().control( name, parameters != null ? parameters.toJson() : "{}" );
	}

	#endregion


	#region Private methods

	// Called automatically when Unity is paused
	private static void onPause()
	{
		instance().onPause();
	}


	// Called automatically when Unity is unpaused
	private static void onResume()
	{
		instance().onResume();
	}

	#endregion


	#region Unity Lifecycle

	public static void onApplicationPause( bool didPause )
	{
		if( didPause )
			onPause();
		else
			onResume();
	}

	#endregion



	#region Platform Specific Private Implementations

#if UNITY_IPHONE

	private class iOSAppington : IAppington
	{
		[DllImport("__Internal")]
		private static extern void _appingtonInit(string api_token);

		public void init(string api_token)
		{
			if( Application.platform != RuntimePlatform.IPhonePlayer )
				return;

			_appingtonInit(api_token);
		}


		[DllImport("__Internal")]
		private static extern void _appingtonControl( string name, string values );

		public void control( string name, string parameters )
		{
			if( Application.platform != RuntimePlatform.IPhonePlayer )
				return;

			_appingtonControl( name, parameters );
		}


		public void onResume()
		{
			if( Application.platform != RuntimePlatform.IPhonePlayer )
				return;
		}


		public void onPause()
		{
			if( Application.platform != RuntimePlatform.IPhonePlayer )
				return;
		}
	}

#elif UNITY_ANDROID

	private class AndroidAppington : IAppington
	{
		private static AndroidJavaObject _plugin;

		public AndroidAppington()
		{
			if( Application.platform != RuntimePlatform.Android )
				return;

			using( var pluginClass = new AndroidJavaClass( "com.appington.AppingtonPlugin" ) )
				_plugin = pluginClass.CallStatic<AndroidJavaObject>( "instance" );
		}


		public void init(string api_token)
		{
			if( Application.platform != RuntimePlatform.Android )
				return;

			_plugin.Call( "init", api_token );
		}


		public void control( string name, string parameters )
		{
			if( Application.platform != RuntimePlatform.Android )
				return;

			_plugin.Call( "control", name, parameters );
		}


		public void onResume()
		{
			if( Application.platform != RuntimePlatform.Android )
				return;

			_plugin.Call( "onResume" );
		}


		public void onPause()
		{
			if( Application.platform != RuntimePlatform.Android )
				return;

			_plugin.Call( "onPause" );
		}
	}

#endif

	#endregion

}
