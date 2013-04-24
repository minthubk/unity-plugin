AppingtonPluginSource/ contains the Java code used to interact between
Unity and Appington on Android.  When compiled the output ends up as
Appington/Assets/Plugins/Android/AndroidPlugin.jar

To build the plugin, open the Appington/ directory as a project in
Unity.  Choose Assets > Export Package which shows a dialog with all
files selected.  Enter the filename without extension (.unitypackage
is automatically added).

(It is recommended to do this in Unity 3 as the resulting plugin will
then work in Unity 3 and 4.  If you do it in Unity 4 then only Unity 4
can open the resulting plugin.)
