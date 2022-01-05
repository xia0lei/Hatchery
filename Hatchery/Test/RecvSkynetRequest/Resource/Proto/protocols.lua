local protocols = {}
protocols[101] = "SkynetMessageReceiver.SkynetMessageReceiver_OnProcessRequest"
protocols[102] = "SkynetMessageReceiver.SkynetMessageReceiver_OnProcessRequestResponse"
protocols[103] = "SkynetMessageReceiver.RPC"

protocols["SkynetMessageReceiver.SkynetMessageReceiver_OnProcessRequest"] = 101
protocols["SkynetMessageReceiver.SkynetMessageReceiver_OnProcessRequestResponse"] = 102
protocols["SkynetMessageReceiver.RPC"] = 103

return protocols
