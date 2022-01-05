--[["
    desc: battle client 
    author: manistein
    since: 2019-03-28
 "]]

local skynet 	    = require "skynet"
local cluster 	    = require "cluster"

local client = "robot"

skynet.start(function()
	cluster.open("testclient")
    skynet.error("hello battle server ===================")
    skynet.newservice(client)
end)

