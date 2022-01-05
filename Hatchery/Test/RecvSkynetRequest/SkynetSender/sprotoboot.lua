--[["
    desc: sproto启动服务
    author: manistein 
    since: 2019-02-19
"]]


local skynet		= require "skynet"
local sprotoparser	= require "sprotoparser"
local sprotoloader	= require "sprotoloader"
local sequence		= require "sequence"
local const			= require "const"

local folder_path = string.format("%s/../../RecvSkynetRequest/Resource/RPCProtoSchema/", skynet.getenv("root"))

local function read_file(path)
    local file_handle = assert(io.open(path))
    local line = file_handle:read("*a")
    file_handle:close()
    return line
end

local function read_all(seq, path)
    local line = ""
    local extension = ".sproto"

    for idx, file in ipairs(seq) do 
        local file_name = path .. file .. extension
        line = line .. read_file(file_name)
    end 

    return line
end 
--[[
local function load_c2s()
    local stream = read_all(sequence.c2s, folder_path .. "client2server/")  
    return sprotoparser.parse(stream)
end 

local function load_s2c()
    local stream = read_all(sequence.s2c, folder_path .. "server2client/")
    return sprotoparser.parse(stream)
end 
]]

local function load_sproto()
	local stream = read_all(sequence, folder_path)
	return sprotoparser.parse(stream)
end


skynet.start(function()
	sprotoloader.save(load_sproto(), const.SPROTO_LOADER_S2SS)

    -- don't call skynet.exit() , because sproto.core may unload and the global slot become invalid
end)
