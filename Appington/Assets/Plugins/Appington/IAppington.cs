using UnityEngine;
using System.Collections.Generic;


public interface IAppington
{
	void init( string api_token );
	void control( string name, string parameters );
	void onResume();
	void onPause();
}
