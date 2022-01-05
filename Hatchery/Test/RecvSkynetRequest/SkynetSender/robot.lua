local skynet = require 'skynet'
local cluster = require 'cluster'
local crypt = require 'crypt'
local pb = require 'protobuf'
local protocols = require 'protocols'
local math = require 'math'

if _VERSION ~= 'Lua 5.3' then
    error 'Use lua 5.3'
end

pb.register_file('../../RecvSkynetRequest/Resource/Proto/netmsg.pb')
pb.register_file('../../RecvSkynetRequest/Resource/Proto/SkynetMessageReceiver.pb')

local CMD = {}
local command = {}

local session = 1
local responses = {}

local function print_tbl(root)
    if root == nil then
        return skynet.error('PRINT_T root is nil')
    end
    if type(root) ~= type({}) then
        return skynet.error('PRINT_T root not table type')
    end
    if not next(root) then
        return skynet.error('PRINT_T root is space table')
    end

    local cache = {[root] = '.'}
    local function _dump(t, space, name)
        local temp = {}
        for k, v in pairs(t) do
            local key = tostring(k)
            if cache[v] then
                table.insert(temp, '+' .. key .. ' {' .. cache[v] .. '}')
            elseif type(v) == 'table' then
                local new_key = name .. '.' .. key
                cache[v] = new_key
                table.insert(temp, '+' .. key .. _dump(v, space .. (next(t, k) and '|' or ' ') .. string.rep(' ', #key), new_key))
            else
                table.insert(temp, '+' .. key .. ' [' .. tostring(v) .. ']')
            end
        end
        return table.concat(temp, '\n' .. space)
    end
    skynet.error(_dump(root, '', ''))
end

local function pack_msg(proto, data)
    local payload = pb.encode(proto, data)
    local index = protocols[proto]

    local msg = pb.encode('netmsg.NetMsg', {action = index, payload = payload})
    local size = #msg + 4
    msg = string.pack('>I2', size) .. msg .. string.pack('>I4', session)
    return msg
end

--[[
local function req(proto, data, func)
    local msg = pack_msg(proto, data)
    local handler = {}
    handler["func"] = func
    responses[session] = handler
end
--]]
function command.init()
    skynet.timeout(1, command.update)
    math.randomseed(skynet.time())
end

local function test_remote_call(proxy, proto, data)
    local content = pb.encode(proto, data)
    local result_index, response = skynet.call(proxy, 'lua', 910, content)
    local rp = pb.decode('SkynetMessageReceiver.SkynetMessageReceiver_OnProcessRequestResponse', response)
    print_tbl(rp)
end

function command.update()
    local proxy = cluster.proxy('testserver', 'RecvSkynetSend')
    local msg =
        pb.encode(
        'SkynetMessageReceiver.SkynetMessageReceiver_OnProcessRequest',
        {
            request_count = math.floor(skynet.time()),
            request_text = 'hahahahahaha hohohohoho xixixixixi'
        }
    )
    test_remote_call(proxy, "SkynetMessageReceiver.RPC", {method = 'OnProcessRequest', param = crypt.base64encode(msg)})
    session = session + 1
    skynet.timeout(500, command.update)
end

skynet.start(
    function(...)
        skynet.dispatch(
            'lua',
            function(session, source, command, ...)
                local f = assert(CMD[command])
                skynet.retpack(f(...))
            end
        )
        command.init()
    end
)
