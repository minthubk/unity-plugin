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
            // See https://dashboard.appington.com/integrate/#init-apppington
            // (step 3) to get the correct api token here
			Appington.init(@api_token@);
		}


		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Placement 1" ) )
		{
			var dict = new Dictionary<string,object>();
            dict.Add("id", "1");
			Appington.control( "placement", dict );
		}


		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Conversion 1" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "id", "1" );
			Appington.control( "conversion", dict );
		}


		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Placement 2" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "id", "2" );
			Appington.control( "placement", dict );
		}


		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Placement 3" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "id", "3" );
			Appington.control( "placement", dict );
		}


		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Placement 4" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "id", "4" );
			Appington.control( "placement", dict );
		}


		if( GUI.Button( new Rect( xPos, yPos += heightPlus, width, height ), "Placement 5" ) )
		{
			var dict = new Dictionary<string,object>();
			dict.Add( "id", "5" );
			Appington.control( "placement", dict );
		}

	}
#endif
}
