package com.appington;

import org.json.JSONException;
import org.json.JSONObject;

import android.app.Activity;
import android.util.Log;

import com.unity3d.player.UnityPlayer;


public class AppingtonPlugin implements com.appington.agar.EventListenerJSON
{
	private static AppingtonPlugin _instance;
	private static final String TAG = "AppingtonPlugin";

	public Activity _activity = null;



	public static AppingtonPlugin instance()
	{
		if( _instance == null )
			_instance = new AppingtonPlugin();
		return _instance;
	}


	private Activity getActivity()
	{
		// this allows testing directly in Eclipse
		if( _activity != null )
			return _activity;

		return UnityPlayer.currentActivity;
	}


	// com.appington.agar.EventListenerJSON
	@Override
	public Object onEvent( String name, JSONObject values )
	{
		Log.i( TAG, "got an event name: " + name );

		try
		{
			// create a hash like so: { name: name, values: values }
			JSONObject rootJsonObject = new JSONObject();
			rootJsonObject.put( "name", name );
			rootJsonObject.put( "values", values );

			Log.i( TAG, "json: " + rootJsonObject.toString() );
			UnityPlayer.UnitySendMessage( "AppingtonManager", "onEventOccurred", rootJsonObject.toString() );
		}
		catch( JSONException e )
		{
			Log.i( TAG, "Error adding data to JSONObject: " + e.getMessage() );
		}

		return null;
	}


	// Public API exposed to Unity
	public void init(String api_token)
	{
		com.appington.agar.Agar.init( getActivity(), api_token );
		com.appington.agar.Agar.registerListener( this );
		onResume();
	}


	public void onPause()
	{
		com.appington.agar.Agar.onPauseActivity( getActivity() );
	}


	public void onResume()
	{
		com.appington.agar.Agar.onResumeActivity( getActivity() );
	}


	public void control( String name, String json )
	{
		com.appington.agar.Agar.control( name, json );
	}



}
