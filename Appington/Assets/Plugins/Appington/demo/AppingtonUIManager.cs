using UnityEngine;
using System.Collections.Generic;


public class AppingtonUIManager : MonoBehaviour
{
#if UNITY_ANDROID || UNITY_IPHONE
	void OnGUI()
	{
		float yPos = 5.0f;
		float xPos = 5.0f;
		float width = ( Screen.width >= 800 || Screen.height >= 800 ) ? 320 : 160;
		float height = ( Screen.width >= 800 || Screen.height >= 800 ) ? 80 : 40;
		float heightPlus = height + 10.0f;
	
	
		if( GUI.Button( new Rect( xPos, yPos, width, height ), "Init" ) )
		{
			Appington.init();
		}


		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Welcome" ) )
		{
			var dict = new Dictionary<string,object>();
			Appington.control( "welcome", dict );
		}

		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Health 20" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "life", 20 );
			dict.Add( "event", "life_threshold" );
			Appington.control( "trigger", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Health 50" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "life", 50 );
			dict.Add( "event", "life_threshold" );
			Appington.control( "trigger", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Health 90" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "life", 90 );
			dict.Add( "event", "life_threshold" );
			Appington.control( "trigger", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Level Start" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "event", "level_start" );
			dict.Add( "level", 3 );
			Appington.control( "trigger", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Level End" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "event", "level_end" );
			dict.Add( "level", 5 );
			Appington.control( "trigger", dict );
		}

	}
#endif
}
