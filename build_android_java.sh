#!/bin/bash

set -e

javac -cp "androidlibs/*" AndroidPluginSource/com/appington/*.java

cd AndroidPluginSource

jar -cf ../Appington/Assets/Plugins/Android/AppingtonPlugin.jar com/appington/*.class
