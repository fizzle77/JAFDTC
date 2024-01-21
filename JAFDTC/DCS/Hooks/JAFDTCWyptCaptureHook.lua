--[[
********************************************************************************************************************

JAFDTCHook.lua -- dcs hook for waypoint capture display

Copyright(C) 2021-2023 the-paid-actor & dcs-dtc contributors
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
local Terrain = require('terrain')
local lfs = require("lfs")
local socket = require("socket")

local JAFDTCHook =
{
    logFile = io.open(lfs.writedir() .. [[Logs\JAFDTCHook.log]], "w"),
    inMission = false,
    visible = true,

    dialogWidth = 430,
    dialogHeight = 300,
    dialog = nil,

    addButton = nil,
    addAsTgtButton = nil,
    clearButton = nil,
    coordListBox = nil,
    coordLabel = nil,
    sendToJAFDTCButton = nil,

    currentCoord = nil,
    coordList = {},

    udpSocket = nil,
    udpPort = 42003
}

function JAFDTCHook:log(str)
    self.logFile:write(str .. "\n");
    self.logFile:flush();
end

function JAFDTCHook:formatCoord(lat, lon, el)
    local originaLat = lat
    local originalLon = lon
    local latH = 'N'
    local lonH = 'E'

    if lat < 0 then
        latH = 'S'
        lat = -lat
    end

    if lon < 0 then
        lonH = 'W'
        lon = -lon
    end

    local latG = math.floor(lat)
    local latM = lat * 60 - latG * 60
    local lonG = math.floor(lon)
    local lonM = lon * 60 - lonG * 60

    local latitude = string.format("%s %02d°%06.3f’", latH, latG, latM)
    local longitude = string.format("%s %03d°%06.3f’", lonH, lonG, lonM)
    local elevation = string.format("%.0f", el*3.28084)

    return {
        string = latitude .. " " .. longitude .. " " .. elevation .. "ft",
        latitude = originaLat,
        longitude = originalLon,
        elevation = elevation,
        target = false
    }
end

function JAFDTCHook:sendData(data)
    if self.udpSocket == nil then
        self.udpSocket = socket.udp()
        self.udpSocket:settimeout(0)
    end

    local status, err = self.udpSocket:sendto(data, "127.0.0.1", self.udpPort)
    if status ~= 1 then
        self:log("Error: " .. err)
    end
end

function JAFDTCHook:updateCurrentCoord()
    local pos = Export.LoGetCameraPosition().p
    local alt = Terrain.GetSurfaceHeightWithSeabed(pos.x, pos.z)
    local lat, lon = Terrain.convertMetersToLatLon(pos.x, pos.z)
    local result = self:formatCoord(lat, lon, alt)
    self.currentCoord = result;
    self.coordLabel:setText(result.string)
end

function JAFDTCHook:updateCoordListBox()
    local text = ""
    for k,v in pairs(self.coordList) do
        local type = "STP"
        if v.target then type = "TGT" end
        text = text .. type .. " " .. string.format("%02d", k) .. " " .. v.string .. "\n"
    end
    self.coordListBox:setText(text)
end

function JAFDTCHook:addCoord(tgt)
    if self.currentCoord ~= nil then
        self.currentCoord.target = tgt
        table.insert(self.coordList, self.currentCoord)
        self:updateCoordListBox()
    end
end

function JAFDTCHook:clearCoords()
    self.coordList = {}
    self:updateCoordListBox()
end

function JAFDTCHook:sendToJAFDTC()
    local str = "["
    for k,v in pairs(self.coordList) do
        local json = '{"Latitude":"' .. v.latitude .. '", "Longitude":"' .. v.longitude .. '", "Elevation":"' .. v.elevation .. '", "IsTarget":' .. tostring(v.target) .. '}'
        str = str .. json .. ","
    end
    if str ~= "[" then
        str = str:sub(1,-2)
    end
    str = str .. "]"
    self:sendData(str)
end

function JAFDTCHook:createDialog()
    if self.dialog then
        self.dialog:destroy()
    end

    local screenWidth, screenHeight = dxgui.GetScreenSize()
    local x = (screenWidth / 2) - 19
    local y = (screenHeight / 2) - 19

    self.dialog = DialogLoader.spawnDialogFromFile(lfs.writedir() .. "Scripts\\JAFDTC\\WaypointCapture.dlg")
    self.dialog:setVisible(true)
    self.dialog:setBounds(math.floor(x), math.floor(y), self.dialogWidth, self.dialogHeight)
    self.dialog:addHotKeyCallback(
        "Ctrl+Shift+j",
        function()
            self:toggle()
        end
    )

    self.dialog:addHotKeyCallback(
        "Ctrl+Shift+r",
        function()
            self:createDialog()
        end
    )

    self.coordLabel = self.dialog.coordLabel
    self.coordListBox = self.dialog.coordListBox

    self.addButton = self.dialog.addButton
    self.addButton:addMouseUpCallback(
        function()
            self:addCoord(false)
        end
    )

    self.addAsTgtButton = self.dialog.addAsTgtButton
    self.addAsTgtButton:addMouseUpCallback(
        function()
            self:addCoord(true)
        end
    )

    self.clearButton = self.dialog.clearButton
    self.clearButton:addMouseUpCallback(
        function()
            self:clearCoords()
        end
    )

    self.sendToJAFDTCButton = self.dialog.sendToJAFDTCButton
    self.sendToJAFDTCButton:addMouseUpCallback(
        function()
            self:sendToJAFDTC()
        end
    )

    self:hide()
end

function JAFDTCHook:toggle()
    if self.visible then
        self:hide()
    else
        self:show()
    end
end

function JAFDTCHook:hide()
    self.dialog:setHasCursor(false)
    self.dialog:setSize(0,0)
    self.visible = false
end

function JAFDTCHook:show()
    if self.inMission == false then
        return
    end
    self.dialog:setHasCursor(true)
    self.dialog:setSize(self.dialogWidth, self.dialogHeight)
    self.visible = true
end

local function initJAFDTCHook()
    local handler = {}

    function handler.onSimulationFrame()
        if JAFDTCHook.inMission and JAFDTCHook.visible then
            JAFDTCHook:updateCurrentCoord()
        end
    end

    function handler.onMissionLoadEnd()
        JAFDTCHook:createDialog()
        JAFDTCHook.inMission = true;
    end

    function handler.onSimulationStop()
        JAFDTCHook:clearCoords()
        JAFDTCHook:hide()
        JAFDTCHook.inMission = false;
    end

    DCS.setUserCallbacks(handler)
end

local status, err = pcall(initJAFDTCHook)
if not status then
    JAFDTCHook:log("Error: " .. err)
end
