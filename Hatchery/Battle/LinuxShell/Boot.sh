#!/bin/bash
parentPath=$(dirname $(pwd))
cd ../../Proto/csharp/
python2 MsgIDGen.py
echo $parentPath
cd $parentPath/LinuxShell/
echo $(pwd)
mv ../../Proto/proto/*.cs ../Resource/Proto/
cd ../../bin/Debug/netcoreapp3.1/
./Hatchery Hatchery Hatchery.Battle.BattleManager ../../../Battle/Resource/Config/Startup.json BattleMgr
