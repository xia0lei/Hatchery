#!/bin/bash

parentPath=$(dirname $(pwd))
cd $parentPath/../../bin/Debug/netcoreapp3.1/
./Hatchery TestCases RecvSkynetRequest
