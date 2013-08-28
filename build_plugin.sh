#!/bin/bash

if [ $# -ne 1 -o ! -d "$1" ]
then
    echo "Supply the name of the output directory" >&2
    exit 1
fi

/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "`pwd`/Appington/" -exportPackage Assets "$1/AppingtonPlugin.unitypackage"

res=$?

if [ $res -ne 0 ]
then
    echo "Failed! $res"
    echo "See ~/Library/Logs/Unity/Editor.log"
    exit $res
fi
