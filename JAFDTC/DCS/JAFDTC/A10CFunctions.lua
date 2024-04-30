--[[
********************************************************************************************************************

A10CFunctions.lua -- warthog airframe-specific lua functions

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

dofile(lfs.writedir() .. 'Scripts/JAFDTC/CommonFunctions.lua')

function JAFDTC_A10C_GetCDU()
	local table = JAFDTC_ParseDisplay(3)
    JAFDTC_DebugDisplay(table);
    return table;
end

function JAFDTC_A10C_GetCDU_value(key)
	local table = JAFDTC_A10C_GetCDU();
    local value = table[key] or "---";
    JAFDTC_Log("CDU table[" .. key .. "]: " .. value);
    return value
end

function JAFDTC_A10C_CheckCondition_IsCoordFmtLL()
    local value = JAFDTC_A10C_GetCDU_value("WAYPTCoordFormat");
    if string.sub(value, 1, 3) == "L/L" then
        return true
    end
    return false
end

function JAFDTC_A10C_CheckCondition_IsCoordFmtNotLL()
    local value = JAFDTC_A10C_GetCDU_value("WAYPTCoordFormat");
    if string.sub(value, 1, 3) ~= "L/L" then
        return true
    end
    return false
end

function JAFDTC_A10C_CheckCondition_IsBullsNotOnHUD()
    local value = JAFDTC_A10C_GetCDU_value("HUD_OFF");
    if value == "OFF" then
        return true
    end
    return false
end

function JAFDTC_A10C_CheckCondition_IsFlightPlanNotManual()
    local value = JAFDTC_A10C_GetCDU_value("FPMode");
    if value ~= "MAN" then
        return true
    end
    return false
end

function JAFDTC_A10C_CheckCondition_SpeedIsAvailable()
    local table = JAFDTC_A10C_GetCDU();
    return table["STRSpeedMode4"] == "IAS" or table["STRSpeedMode5"] == "TAS" or table["STRSpeedMode6"] == "GS"
end

function JAFDTC_A10C_CheckCondition_SpeedIsNotAvailable()
    local table = JAFDTC_A10C_GetCDU();
    return table["STRSpeedMode4"] ~= "IAS" and table["STRSpeedMode5"] ~= "TAS" and table["STRSpeedMode6"] ~= "GS"
end

function JAFDTC_A10C_CheckCondition_SpeedIsNot(speed)
    JAFDTC_Log("SpeedIsNot(" .. speed .. ")");
    local table = JAFDTC_A10C_GetCDU();
    if speed == "IAS" then
        return table["STRSpeedMode4"] ~= "IAS"
    end
    if speed == "TAS" then
        return table["STRSpeedMode5"] ~= "TAS"
    end
    if speed == "GS" then
        return table["STRSpeedMode6"] ~= "GS"
    end
    return true
end

--[[
local vhf_lut1 = {
    ["0.0"] = "3",
    ["0.15"] = "3",
    ["0.20"] = "4",
    ["0.25"] = "5",
    ["0.30"] = "6",
    ["0.35"] = "7",
    ["0.40"] = "8",
    ["0.45"] = "9",
    ["0.50"] = "10",
    ["0.55"] = "11",
    ["0.60"] = "12",
    ["0.65"] = "13",
    ["0.70"] = "14",
    ["0.75"] = "15"
}

local function getVhfFmFreqency()
    local freq1 = vhf_lut1[string.format("%.2f",GetDevice(0):get_argument_value(157))]
	if freq1 == nil then freq1 = " " end
    local freq2 = string.format("%1.1f", GetDevice(0):get_argument_value(158)):sub(3)
	if freq2 == nil then freq2 = " " end
    local freq3 = string.format("%1.1f", GetDevice(0):get_argument_value(159)):sub(3)
	if freq3 == nil then freq3 = " " end
    local freq4 = string.format("%1.2f", GetDevice(0):get_argument_value(160)):sub(3)
	if freq4 == nil then freq4 = "  " end

    return freq1 .. freq2 .. "." .. freq3 .. freq4
end
--]]

function JAFDTC_A10C_AfterNextFrame(params)
    local mainPanel = GetDevice(0);
    local comSec = mainPanel:get_argument_value(532);           -- UFC_COM_SEC
    local eccm = mainPanel:get_argument_value(535);             -- UFC_ECCM
    local iff = mainPanel:get_argument_value(533);              -- UFC_IFF
    local idmrt = mainPanel:get_argument_value(536);            -- UFC_IDM

    -- for some reason, UFC_COM_SEC pushbutton activated state is -1, not 1 like the other buttons. dcs works in
    -- strange and mysterious ways...
    if iff == 1 then params["uploadCommand"] = "1" end
    if comSec == -1 then params["incCommand"] = "1" end
    if eccm == 1 then params["decCommand"] = "1" end
    if idmrt == 1 then params["toggleJAFDTCCommand"] = "1" end
end