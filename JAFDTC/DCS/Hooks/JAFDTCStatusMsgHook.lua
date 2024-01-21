--[[
********************************************************************************************************************

JAFDTCCfgNameHook.lua -- dcs hook for configuration name display

Copyright(C) 2023-2024 ilominar/raven

This program is free software: you can redistribute it and/or modify it under the terms of the GNU General
Public License as published by the Free Software Foundation, either version 3 of the License, or (at your
option) any later version.

This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the
implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU General Public License
for more details.

You should have received a copy of the GNU General Public License along with this program.  If not, see
<https://www.gnu.org/licenses/>.

********************************************************************************************************************
--]]

package.path  = package.path..";.\\LuaSocket\\?.lua;"
package.cpath = package.cpath..";.\\LuaSocket\\?.dll;"
package.path = package.path .. ";.\\Scripts\\?.lua;.\\Scripts\\UI\\?.lua;"

local dxgui = require('dxgui')
local DialogLoader = require("DialogLoader")
local lfs = require("lfs")
local socket = require("socket")

local JAFDTCCfgNameHook =
{
    logFile = io.open(lfs.writedir() .. [[Logs\JAFDTCCfgNameHook.log]], "w"),
    inMission = false,
    visible = true,

    dialogWidth = 650,
    dialogHeight = 64,
    dialog = nil,

    configLabel = nil,

    lastTime = 0,
    hideTime = 0,

    tcpServer = nil,
    tcpPort = 42004
}

function JAFDTCCfgNameHook:log(str)
    self.logFile:write(str .. "\n");
    self.logFile:flush();
end

function JAFDTCCfgNameHook:receiveData()
    local data = nil
    local err = nil

    if not self.tcpServer then
        self.tcpServer = socket.tcp()
        local successful, err = self.tcpServer:bind("127.0.0.1", self.tcpPort)
        self.tcpServer:listen(1)
        self.tcpServer:settimeout(0)
        if not successful then
            self:log("Error opening tcp socket: " .. tostring(err))
        else
            self:log("Opened tcp socket")
        end
    end

    -- accept a connection from local host on the name port and grab the data (a configuration
    -- name). shutdown the accepted connection as jafdtc will create a new connection next time
    -- it needs to update the name.
    --
    local client, err = self.tcpServer:accept()
    if client then
        client:settimeout(1)
        data, err = client:receive()
        if err then
            self:log("Error during Rx: " .. tostring(err))
            data = nil
        end
        client:shutdown()
    end

    return data
end

function JAFDTCCfgNameHook:createDialog()
    if self.dialog then
        self.dialog:destroy()
    end

    local screenWidth, screenHeight = dxgui.GetScreenSize()
    local x = ((screenWidth - self.dialogWidth) / 2)
    local y = ((screenHeight - self.dialogHeight) / 2)

    self.dialog = DialogLoader.spawnDialogFromFile(lfs.writedir() .. "Scripts\\JAFDTC\\ConfigName.dlg")
    self.dialog:setVisible(true)
    self.dialog:setBounds(math.floor(x), math.floor(y), self.dialogWidth, self.dialogHeight)

    self.configLabel = self.dialog.configLabel

    self:hide()

    self:log("Dialog initialized & created")
end

function JAFDTCCfgNameHook:updateTick(curTime)
    local data = self:receiveData()
    if data then
        if not self.visible then
            self:show()
        end
        self.hideTime = curTime + 2
        self.configLabel:setText(data)
    elseif self.visible and curTime > self.hideTime then
        self:hide()
    end
    self.lastTime = curTime
end

function JAFDTCCfgNameHook:hide()
    if self.dialog then
        self.dialog:setHasCursor(false)
        self.dialog:setSize(0,0)
    end
    self.visible = false
end

function JAFDTCCfgNameHook:show()
    if self.inMission then
        if self.dialog then
            self.dialog:setHasCursor(true)
            self.dialog:setSize(self.dialogWidth, self.dialogHeight)
        end
        self.visible = true
    end
end

local function initJAFDTCCfgNameHook()
    local handler = {}

    function handler.onSimulationFrame()
        local curTime = socket.gettime()
        if curTime ~= JAFDTCCfgNameHook.lastTime and JAFDTCCfgNameHook.inMission then
            JAFDTCCfgNameHook:updateTick(curTime)
        end
    end

    function handler.onMissionLoadEnd()
        JAFDTCCfgNameHook:createDialog()
        JAFDTCCfgNameHook.inMission = true;
    end

    function handler.onSimulationStop()
        JAFDTCCfgNameHook:hide()
        JAFDTCCfgNameHook.inMission = false;
    end

    DCS.setUserCallbacks(handler)
end

local status, err = pcall(initJAFDTCCfgNameHook)
if not status then
    JAFDTCCfgNameHook:log("Initializtion failed: " .. err)
end
