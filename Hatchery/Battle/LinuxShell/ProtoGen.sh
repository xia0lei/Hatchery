#!/bin/bash

parentPath=$(dirname $(pwd))
cd ../../Proto && git pull
cd ./csharp/
python2 MsgIDGen.py
cd ../proto/
sh gen.sh
echo $parentPath
cd $parentPath/LinuxShell/
echo $(pwd)
mv ../../Proto/proto/*.cs ../Resource/Proto/

