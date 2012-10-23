using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Linq;


public class AppingtonMenuItem : MonoBehaviour
{
	private static string[] tokens = new string[] { "<!-- ACTIVITIES -->", "<!-- META-DATA -->", "<!-- PERMISSIONS -->" };
	private static readonly string kPackageNameKey = "CURRENT_PACKAGE_NAME";


	// returns either the Unity specific version of the UnityManifest (if available) or the old one (if available)
	private static string getBaseManifestFile()
	{
		// we need the Unity manifest to start with. the one we want depends on our current version
		var desiredFile = Application.unityVersion.StartsWith( "3.4" ) ? "Unity34Manifest.xml" : "Unity35Manifest.xml";
			
		var path = Path.Combine( Application.dataPath, "Plugins/Android" );
		if( new DirectoryInfo( path ).GetFiles( desiredFile ).Length == 0 )
		{
			UnityEngine.Debug.Log( "Could not find Unity3XManifest.xml file in the Plugins/Android directory." );
			throw new Exception( "Could not find Unity3XManifest.xml file in the Plugins/Android directory." );
		}
			
		return new DirectoryInfo( path ).GetFiles( desiredFile ).First().FullName;			
	}


	private static IEnumerable<string> getAllManifestFiles()
	{
		// grab out Plugins folder and find all the plugins that have Manifext.xml files
		string path = Path.Combine( Application.dataPath, "Plugins" );
		var allFiles = from dir in new DirectoryInfo( path ).GetDirectories( "*Android" )
					  where dir.Name != "Android" && dir.GetFiles( "Manifest.xml" ).Length > 0
					  select dir.GetFiles( "Manifest.xml" ).First().FullName;
			
		return allFiles;
	}
		
		
	private static Dictionary<string, List<string>> extractAttributesFromManifest( string path )
	{
		var dict = new Dictionary<string, List<string>>();
		var lastToken = string.Empty;
		var foundToken = false;
			
		foreach( var line in File.ReadAllLines( path ) )
		{
			// ditch empty lines
			if( line.Length == 0 )
				continue;
				
			// check for tokens
			foreach( var token in tokens )
			{
				if( line.Contains( token ) )
				{
					lastToken = token;
					foundToken = true;
					break;
				}
			}
				
			// did we find a token?
			if( foundToken )
			{
				foundToken = false;
				continue;
			}
				
			// add the token to the dict if we dont have it already
			if( !dict.ContainsKey( lastToken ) )
				dict.Add( lastToken, new List<string>() );
				
			// perform a replacement of the current package name if nececssary
			var updatedLine = line.Replace( kPackageNameKey, PlayerSettings.bundleIdentifier );
				
			// add the line
			dict[lastToken].Add( "\t\t" + updatedLine.Trim() );
		}
			
		return dict;
	}
		
		
	private static void mergeAttributeDictionaries( Dictionary<string, List<string>> root, Dictionary<string, List<string>> addon )
	{
		foreach( var kv in addon )
		{
			// easy case first: no need to merge anything because it doesnt exist in the root yet
			if( !root.ContainsKey( kv.Key ) )
			{
				root.Add( kv.Key, kv.Value );
			}
			else
			{
				// loop through the addon list and add any values that we are missing
				foreach( var item in kv.Value )
				{
					// only check for dupes if we are in the permissions section
					if( kv.Key == "<!-- PERMISSIONS -->" && root[kv.Key].Contains( item ) )
						continue;
					root[kv.Key].Add( item );
				}
			}
		}
	}


	// Android validators
	[MenuItem( "Appington/Generate AndroidManifest.xml File...", true )]
	static bool validateAndroid()
	{
		return EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Android;
	}


	[MenuItem( "Appington/Generate AndroidManifest.xml File...", false, 31 )]
	static void generateManifest()
	{
		// we need the Unity manifest to start with
		var baseManifest = getBaseManifestFile();
		if( baseManifest == null )
			return;
		
		var manifestFiles = getAllManifestFiles();
		var baseDict = new Dictionary<string, List<string>>();
	
		// first, we loop through our extra manifests, gather all the new attributes we need and unique them
		foreach( var manifestPath in manifestFiles )
		{
			var dict = extractAttributesFromManifest( manifestPath );
			mergeAttributeDictionaries( baseDict, dict );
		}
			
		// nothing changed?  get out of here
		if( baseDict.Count == 0 )
			return;
			
		// now we read in the UnityManifest which we will modify with our new values
		var allLines = File.ReadAllLines( baseManifest );
		var fixedLines = new List<string>( allLines.Length );
			
		// loop through all the lines and see if we have any new attributes to inject
		foreach( var line in allLines )
		{
			// add all the lines one by one
			fixedLines.Add( line );
				
			// check for tokens
			foreach( var token in tokens )
			{
				if( line.Contains( token ) && baseDict.Keys.Contains( token ) )
				{
					fixedLines.AddRange( baseDict[token] );
				}
			}
		}

			
		// get a path to the final location of the AndroidManifest.xml file that we will write
		var finalManifestPath = Path.Combine( Application.dataPath, "Plugins/Android/AndroidManifest.xml" );
		if( File.Exists( finalManifestPath ) )
			File.Delete( finalManifestPath );
			
		File.WriteAllLines( finalManifestPath, fixedLines.ToArray() );
			
		EditorUtility.DisplayDialog( "Appington Message", "Merged and created a new AndroidManifest.xml file!", "OK" );
	}

}