#!/bin/bash

set -e

if [ $# -ne 1 -o ! -d "$1" ]
then
    echo "Supply the name of the output directory" >&2
    exit 1
fi

/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath ~/projects/unity-plugin/Appington/ -exportPackage Assets "$1/AppingtonPlugin.unitypackage"
