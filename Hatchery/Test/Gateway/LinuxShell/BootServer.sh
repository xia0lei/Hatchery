#!/bin/bash

# boot gate server

parentPath=$(dirname $(pwd))
cd $parentPath/../../bin/Debug/netcoreapp3.1
#nohup mono spark-server.exe TestCases GatewayCase &
./Hatchery TestCases GatewayCase
