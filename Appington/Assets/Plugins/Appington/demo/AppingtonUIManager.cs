using UnityEngine;
using System.Collections.Generic;


public class AppingtonUIManager : MonoBehaviour
{
#if UNITY_ANDROID
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

		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Voice Tip 1" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "which", 1 );
			Appington.control( "voice_tip", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Voice Tip 2" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "which", 2 );
			Appington.control( "voice_tip", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Voice Tip 3" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "which", 3 );
			Appington.control( "voice_tip", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Level Start" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "level", 3 );
			Appington.control( "level_start", dict );
		}
		
		
		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Level End" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "level", 3 );
			Appington.control( "level_end", dict );
		}

	}
#endif
}
