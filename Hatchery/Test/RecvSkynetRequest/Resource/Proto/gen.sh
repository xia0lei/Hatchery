#!/bin/sh
protoc --descriptor_set_out=SkynetMessageReceiver.pb SkynetMessageReceiver.proto 
protoc --descriptor_set_out=netmsg.pb netmsg.proto 

