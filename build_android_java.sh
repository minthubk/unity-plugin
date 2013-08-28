#!/bin/bash

set -e

javac -bootclasspath androidlibs/android.jar -cp "androidlibs/*" -source 1.6 -target 1.6 AndroidPluginSource/com/appington/*.java

cd AndroidPluginSource

jar -cf ../Appington/Assets/Plugins/Android/AppingtonPlugin.jar com/appington/*.class
