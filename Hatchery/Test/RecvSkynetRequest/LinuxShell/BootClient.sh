#!/bin/bash

parentPath=$(dirname $(pwd))
cd $parentPath/../TestDependency/shell/
./test.sh start ../RecvSkynetRequest/SkynetSender
