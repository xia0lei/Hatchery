#!/bin/bash

# boot gate server

parentPath=$(dirname $(pwd))

echo $parentPath
cd $parentPath/../../bin/Debug/netcoreapp3.1/

./Hatchery TestCases GatewayClientCase
 
