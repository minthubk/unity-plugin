AppingtonPluginSource/ contains the Java code used to interact between
Unity and Appington on Android.  When compiled the output ends up as
Appington/Assets/Plugins/Android/AndroidPlugin.jar

To recompile the Android Java code create a directory named
androidlibs with the following contents and run build_android_java.sh.
A prebuilt copy is in Appington/Assets/Plugins/Android

  appington.jar from the Appington SDK
  android.jar from the Android SDK (eg platforms/android-10/android.jar)
  classes.jar from /Applications/Unity/Unity.app/Contents/PlaybackEngines/AndroidPlayer/bin/

To build the plugin, run build_plugin.sh

(It is recommended to do this in Unity 3 as the resulting plugin will
then work in Unity 3 and 4.  If you do it in Unity 4 then only Unity 4
can open the resulting plugin.)
