using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Linq;

using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;


public class AppingtonMenuItem : MonoBehaviour
{
	private static string[] tokens = new string[] { "<!-- ACTIVITIES -->", "<!-- META-DATA -->", "<!-- PERMISSIONS -->" };
	private static readonly string kPackageNameKey = "CURRENT_PACKAGE_NAME";


	private static string getBaseManifestFile()
	{
		// we need the Unity manifest to start with.
		var pathToManifest = Path.Combine( EditorApplication.applicationContentsPath, "PlaybackEngines/AndroidPlayer/AndroidManifest.xml" );
		if( !File.Exists( pathToManifest ) )
			throw new Exception( "Could not find an AndroidManifest.xml file in your Unity application directory" );
		
		return pathToManifest;		
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


	// Android validator and menu itemss
	[MenuItem( "Appington/Generate AndroidManifest.xml File...", true )]
	static bool validateAndroid()
	{
		return EditorUserBuildSettings.selectedBuildTargetGroup == BuildTargetGroup.Android;
	}


	[MenuItem( "Appington/Generate AndroidManifest.xml File...", false )]
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
	
	
	[MenuItem( "Appington/Install or Update Appington SDK...", false )]
	static void installOrUpdateAppingtonLibrary()
	{
		try
		{
			// first, fetch the newest SDK version
			var latestSDKVersionAvailable = getLatestSDKVersionFromServer();
			UnityEngine.Debug.Log( "latest Appington SDK version available: " + latestSDKVersionAvailable );
			
			// see what version we have installed (if any)
			var currentInstalledSDKVersion = getInstalledSDKVersion();
			UnityEngine.Debug.Log( "current installed Appington SDK: " + ( currentInstalledSDKVersion != null ? currentInstalledSDKVersion : "none installed" ) );
			
			if( shouldUpdateOrInstallSDK( currentInstalledSDKVersion, latestSDKVersionAvailable ) )
			{
				var destinationPath = Path.Combine( System.IO.Path.GetTempPath(), "AppingtonSDK.zip" );
				fetchSDKZip( latestSDKVersionAvailable, destinationPath );
				extractSDKAndImportFiles( destinationPath );
				
				UnityEngine.Debug.Log( "Appington SDK updated" );
			}
			else
			{
				UnityEngine.Debug.Log( "There is no new SDK to install available" );
			}
		}
		catch( Exception e )
		{
			EditorUtility.DisplayDialog( "Error Downloading and Unpacking Appington SDK", e.Message, "OK" );
		}
		finally
		{
			EditorUtility.ClearProgressBar();
		}
	}
	
	
	// fetches the version of the latest SDK
	private static string getLatestSDKVersionFromServer()
	{
		var url = "https://cdn.appington.com/updates/sdk/sdkinfo.json";
		var www = new WWW( url );
		
		while( !www.isDone )
			EditorUtility.DisplayProgressBar( "Locating Current Appington SDK Version...", string.Empty, www.progress );
		
		if( www.error != null )
			throw new Exception( www.error + "Try again later" );
		
		EditorUtility.ClearProgressBar();
		
		var dict = www.text.dictionaryFromJson();
		
		return dict["version"].ToString();
	}
	
	
	// Fetches the currently installed version number and returns it or null if no SDK is installed
	private static string getInstalledSDKVersion()
	{
		var path = Path.Combine( Application.dataPath, "StreamingAssets/appington/buildinfo.json" );
		
		if( !File.Exists( path ) )
			return null;
		
		var dict = File.ReadAllText( path ).dictionaryFromJson();
		
		return dict["version"].ToString();
	}
	
	
	// Checks to see if an install/update should occur based on the versions passed in
	private static bool shouldUpdateOrInstallSDK( string currentInstalledVersion, string latestAvailableVersion )
	{
		// early out if we have nothing installed
		if( currentInstalledVersion == null )
			return true;
		
		var currentVersion = new Version( currentInstalledVersion.Substring( 0, currentInstalledVersion.IndexOf( "-" ) ) );
		var latestVersion = new Version( latestAvailableVersion.Substring( 0, latestAvailableVersion.IndexOf( "-" ) ) );
		
		return currentVersion.CompareTo( latestVersion ) < 0;
	}
	

	private static void fetchSDKZip( string version, string destination )
	{
		var url = string.Format( "https://cdn.appington.com/updates/sdk/appington-sdk-{0}.zip", version );
		var www = new WWW( url );
		
		while( !www.isDone )
			EditorUtility.DisplayProgressBar( "Downloading Appington SDK...", string.Empty, www.progress );
		
		if( www.error != null )
			throw new Exception( www.error + "Try again later" );
		
		EditorUtility.ClearProgressBar();
		
		// write to disk then import
		File.WriteAllBytes( destination, www.bytes );
	}
	
	
	private static void extractSDKAndImportFiles( string zipFilePath )
	{
		EditorUtility.DisplayProgressBar( "", "Extracting Appington SDK...", 0.5f );

		var destinationDirectory = Path.Combine( System.IO.Path.GetTempPath(), "AppingtonSDK/" );
		extractZipToDestination( zipFilePath, destinationDirectory );

		// find the actual directory the files reside in
		var unzippedSDKDirectory = Directory.GetDirectories( destinationDirectory ).First();
		
		// find the goodes we need
		var assetsZipFile = Path.Combine( unzippedSDKDirectory, "assets.zip" );
		
		// extract the assets
		var assetsDestinationDirectory = Path.Combine( System.IO.Path.GetTempPath(), "AppingtonAssets/" );;
		extractZipToDestination( assetsZipFile, assetsDestinationDirectory );
		var unzippedAssetsDirectory = Path.Combine( assetsDestinationDirectory, "assets/appington/" );

		// make sure our target directory is empty
		var assetsHomeDir = Path.Combine( Application.dataPath, "StreamingAssets/appington/" );
		if( Directory.Exists( assetsHomeDir ) )
			Directory.Delete( assetsHomeDir, true );
		
		// make sure we have a StreamingAssets dir
		var streamingAssetsDir = Path.Combine( Application.dataPath, "StreamingAssets" );
		if( !Directory.Exists( streamingAssetsDir ) )
			Directory.CreateDirectory( streamingAssetsDir );

		// move the assets folder into place
		assetsHomeDir = Path.Combine( Application.dataPath, "StreamingAssets/appington/" );
		Directory.Move( unzippedAssetsDirectory, assetsHomeDir );
		
		
		// copy the jar to its new home
		var jarFile = Path.Combine( unzippedSDKDirectory, "appington.jar" );
		var jarHome = Path.Combine( Application.dataPath, "Plugins/Android/appington.jar" );
		if( File.Exists( jarHome ) )
			File.Delete( jarHome );
		File.Move( jarFile, jarHome );
		
		
		// copy the debugconfig.json file to its new home
		var jsonFile = Path.Combine( unzippedSDKDirectory, "debugconfig.json" );
		var jsonHome = Path.Combine( Application.dataPath, "Plugins/Android/debugconfig.json" );
		if( File.Exists( jsonHome ) )
			File.Delete( jsonHome );
		File.Move( jsonFile, jsonHome );
		
		AssetDatabase.Refresh();
	}
	
	
	// Extracts a zip file to a destination directory
	private static void extractZipToDestination( string sourceZipFile, string destinationDirectory )
	{
		if( Directory.Exists( destinationDirectory ) )
			Directory.Delete( destinationDirectory, true );
		Directory.CreateDirectory( destinationDirectory );
		
		using( ZipInputStream s = new ZipInputStream( File.OpenRead( sourceZipFile ) ) )
		{
			ZipEntry theEntry;
			while( ( theEntry = s.GetNextEntry() ) != null )
			{
				var directoryName = Path.GetDirectoryName( theEntry.Name );
				var fileName = Path.GetFileName( theEntry.Name );
				
				// create directory
				if ( directoryName.Length > 0 )
					Directory.CreateDirectory( destinationDirectory + directoryName );
				
				if( fileName != String.Empty )
				{
					using( var streamWriter = File.Create( destinationDirectory + theEntry.Name ) )
					{
						int size = 2048;
						byte[] data = new byte[2048];
						while( true )
						{
							size = s.Read( data, 0, data.Length );
							if( size > 0 )
								streamWriter.Write( data, 0, size );
							else
								break;
						}
					} // end using
				} // end if
			} // end while
		}
	}
	
}