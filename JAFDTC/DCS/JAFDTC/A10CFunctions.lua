--[[
********************************************************************************************************************

A10CFunctions.lua -- warthog airframe-specific lua functions

Copyright(C) 2023-2024 ilominar/raven, JAFDTC contributors

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

-- NOTE: requires that CommonFunctions.lua has been loaded...

-- Displays
-----------
-- 1: LMFD
-- 2: RMFD
-- 3: CDU
-- 4: ?
-- 5: HUD
-- 6: ?
-- 7: CMS (countmeasures panel on right)
-- 8: CMSC (countermeasure and jammer display below UFC)

-- LFMD Routines

function JAFDTC_A10C_GetLMFD()
    local table = JAFDTC_ParseDisplay(1);
    JAFDTC_DebugDisplay(table);
    return table;
end

function JAFDTC_A10C_GetLMFD_value(key)
    local table = JAFDTC_A10C_GetLMFD();
    local value = table[key] or "---";
    JAFDTC_Log("LMFD table[" .. key .. "]: " .. value);
    return value;
end

function JAFDTC_A10C_Fn_IsCommPageOnDefaultButton()
    local value = JAFDTC_A10C_GetLMFD_value("label_12");
    return value == "COMM"
end

-- DSMS Routines

function JAFDTC_A10C_Fn_IsDSMSInDefaultMFDPosition()
    local value = JAFDTC_A10C_GetLMFD_value("label_13");
    return value == "DSMS";
end

function JAFDTC_A10C_Fn_QueryLoadout()
    local lmfd = JAFDTC_A10C_GetLMFD();
    local outputTable = { };
    for station = 1,11 do
        outputTable[station] = lmfd["STORE_NAME_" .. station] or "---";
    end
    local response = JAFDTC_A10C_TableToString(outputTable);
    JAFDTC_Log("QueryLoadout: " .. response);
    return response;
end

function JAFDTC_A10C_Fn_QueryDSMSProfiles()
    local lmfd = JAFDTC_A10C_GetLMFD();
    local outputTable = { };
    profile_index = 0;
    repeat
      profile_index = profile_index + 1;  
      value = lmfd["TABLE_NAMES" .. profile_index] or "###";
      if value ~= "###" then
        outputTable[profile_index] = value;
      end
    until(value == "###")
    local response = JAFDTC_A10C_TableToString(outputTable);
    JAFDTC_Log("QueryDSMSProfiles: " .. response);
    return response;
end

-- CDU Routines

function JAFDTC_A10C_GetCDU()
    local table = JAFDTC_ParseDisplay(3);
    JAFDTC_DebugDisplay(table);
    return table;
end

function JAFDTC_A10C_GetCDU_value(key)
    local table = JAFDTC_A10C_GetCDU();
    local value = table[key] or "---";
    JAFDTC_Log("CDU table[" .. key .. "]: " .. value);
    return value;
end

function JAFDTC_A10C_Fn_IsCoordFmtLL()
    local value = JAFDTC_A10C_GetCDU_value("WAYPTCoordFormat");
    if string.sub(value, 1, 3) == "L/L" then
        return true
    end
    return false
end

function JAFDTC_A10C_Fn_IsCoordFmtNotLL()
    local value = JAFDTC_A10C_GetCDU_value("WAYPTCoordFormat");
    if string.sub(value, 1, 3) ~= "L/L" then
        return true
    end
    return false
end

function JAFDTC_A10C_Fn_IsBullsNotOnHUD()
    local value = JAFDTC_A10C_GetCDU_value("HUD_OFF");
    if value == "OFF" then
        return true
    end
    return false
end

function JAFDTC_A10C_Fn_IsFlightPlanNotManual()
    local value = JAFDTC_A10C_GetCDU_value("FPMode");
    if value ~= "MAN" then
        return true
    end
    return false
end

function JAFDTC_A10C_Fn_SpeedIsAvailable()
    local table = JAFDTC_A10C_GetCDU();
    return table["STRSpeedMode4"] == "IAS" or table["STRSpeedMode5"] == "TAS" or table["STRSpeedMode6"] == "GS"
end

function JAFDTC_A10C_Fn_SpeedIsNotAvailable()
    local table = JAFDTC_A10C_GetCDU();
    return table["STRSpeedMode4"] ~= "IAS" and table["STRSpeedMode5"] ~= "TAS" and table["STRSpeedMode6"] ~= "GS"
end

function JAFDTC_A10C_Fn_SpeedIsNot(speed)
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

-- HUD Routines

function JAFDTC_A10C_GetHUD()
	local table = JAFDTC_ParseDisplay(5);
    JAFDTC_DebugDisplay(table);
    return table;
end

function JAFDTC_A10C_GetHUD_value(key)
	local table = JAFDTC_A10C_GetHUD();
    local value = table[key] or "---";
    JAFDTC_Log("HUD table[" .. key .. "]: " .. value);
    return value;
end

function JAFDTC_A10C_Fn_Arc210Com1IsOnHUD()
    return JAFDTC_A10C_GetHUD_value("ARC_210_Radio_1_Status") ~= "---"
end
function JAFDTC_A10C_Fn_Arc210Com2IsOnHUD()
    return JAFDTC_A10C_GetHUD_value("ARC_210_Radio_2_Status") ~= "---"
end

-- Utility

function JAFDTC_A10C_TableToString(table)
    local response = "";
    for k,v in pairs(table) do
        response = response .. k .. "=" .. v .. ";"; -- TODO escaping
    end
    return response
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

-- --------------------------------------------------------------------------------------------------------------------
--
-- frame handler
--
-- --------------------------------------------------------------------------------------------------------------------

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