--[[
********************************************************************************************************************

FA18CFunctions.lua -- hornet airframe-specific lua functions

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

dofile(lfs.writedir() .. 'Scripts/JAFDTC/commonFunctions.lua')

-- --------------------------------------------------------------------------------------------------------------------
--
-- display parsers
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_FA18C_GetLeftDDI()
    return JAFDTC_ParseDisplay(2)
end

function JAFDTC_FA18C_GetRightDDI()
    return JAFDTC_ParseDisplay(3)
end

function JAFDTC_FA18C_GetMPCD()
    return JAFDTC_ParseDisplay(4)
end

function JAFDTC_FA18C_GetIFEI()
    return JAFDTC_ParseDisplay(5)
end

function JAFDTC_FA18C_GetUFC()
    return JAFDTC_ParseDisplay(6)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- conditions: common
--
-- --------------------------------------------------------------------------------------------------------------------

--[[
function JAFDTC_FA18C_CheckCondition_RightMFDSUPT()
    local table = DTC_FA18C_GetRightDDI();
    local str = table["SUPT_id:13"] or ""
    return (str == "SUPT")
end

function JAFDTC_FA18C_CheckCondition_LeftMFDTAC()
    local table = DTC_FA18C_GetLeftDDI();
    local str = table["TAC_id:23"] or ""
    return (str == "TAC")
end
--]]

-- --------------------------------------------------------------------------------------------------------------------
--
-- conditions: cms system
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_FA18C_CheckCondition_IsDispenserOff()
    local table = JAFDTC_FA18C_GetLeftDDI();
    local str = table["EW_ALE47_MODE_label_cross_Root"] or "x"
    return (str == "")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- conditions: pre-planned system
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_FA18C_CheckCondition_IsNotLMFDTAC()
    local table = JAFDTC_FA18C_GetLeftDDI();
    local str = table["TAC_id:23"] or ""
    return (str ~= "TAC")
end

function JAFDTC_FA18C_CheckCondition_IsStationCarriesStore(stn, store)
    local table = JAFDTC_GetDisplay(2)
    local label = table["STA" .. stn .. "_Label_TYPE.1"] or ""
    return (label == store)
end

function JAFDTC_FA18C_CheckCondition_IsNotStationCarriesStore(stn, store)
    return not JAFDTC_FA18C_CheckCondition_IsStationCarriesStore(stn, store)
end

function JAFDTC_FA18C_CheckCondition_IsStationSelected(station)
    local table = JAFDTC_FA18C_GetLeftDDI();
    local str = table["STA"..station.."_Selective_Box_Line_02"] or "x"
    return (str == "")
end

function JAFDTC_FA18C_CheckCondition_IsNotStationSelected(station)
    return not JAFDTC_FA18C_CheckCondition_IsStationSelected(station)
end

function JAFDTC_FA18C_CheckCondition_IsInPPStation(station)
    local table = JAFDTC_GetDisplay(2)
    local str = table["Station_JDAM.2.CurrStation_JDAM.1"] or ""
    return (str == station)
end

function JAFDTC_FA18C_CheckCondition_IsNotInPPStation(station)
    return not JAFDTC_FA18C_CheckCondition_IsInPPStation(station)
end

function JAFDTC_FA18C_CheckCondition_IsNotPPSelected(number)
    local table = JAFDTC_FA18C_GetLeftDDI();
    local str = table["MISSION_Type"] or ""
    return (str ~= "PP" .. number)
end

function JAFDTC_FA18C_CheckCondition_IsTargetOfOpportunity()
    local table = JAFDTC_FA18C_GetLeftDDI();
    local str = table["Miss_Type"] or ""
    return (str == "TOO1")
end

--[[
function JAFDTC_FA18C_CheckCondition_NotBullseye()
    local table = JAFDTC_FA18C_GetRightDDI();
    local str = table["A/A WP_1_box__id:12"] or "x"
    return (str == "x")
end

function JAFDTC_FA18C_CheckCondition_MapBoxed()
    local table = JAFDTC_FA18C_GetRightDDI();
    local str = table["MAP_1_box__id:30"] or "x"
    return (str == "")
end

function JAFDTC_FA18C_CheckCondition_MapUnboxed()
    local table = JAFDTC_FA18C_GetRightDDI();
    local root = table["HSI_Main_Root"] or "x"
    local str = table["MAP_1_box__id:30"] or "x"
    return (root == "x" and str == "x")
end

function JAFDTC_FA18C_CheckCondition_IsBankLimitOnNav()
    local table = JAFDTC_FA18C_GetRightDDI();
    local str = table["_1__id:13"] or ""
    return (str == "N")
end
--]]

-- --------------------------------------------------------------------------------------------------------------------
--
-- conditions: radio system
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_FA18C_CheckCondition_IsRadioOnChannel(radio, channel)
    local table = JAFDTC_FA18C_GetUFC();
    local varName = "UFC_Comm"..radio.."Display"
    local str = table[varName] or ""
    local currChannel = str:gsub("%s+", ""):gsub("`", "1"):gsub("~", "2")
    return (currChannel == channel)
end

function JAFDTC_FA18C_CheckCondition_IsNotRadioOnChannel(radio, channel)
    return not JAFDTC_FA18C_CheckCondition_IsRadioOnChannel(radio, channel)
end

function JAFDTC_FA18C_CheckCondition_IsRadioGuardDisabled()
    local table = JAFDTC_FA18C_GetUFC();
    local str = table["UFC_OptionCueing1"] or ""
    return (str ~= ":")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- conditions: waypoint system
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_FA18C_CheckCondition_IsNotRMFDSUPT()
    local table = JAFDTC_FA18C_GetRightDDI();
    local str = table["SUPT_id:13"] or ""
    return (str ~= "SUPT")
end

function JAFDTC_FA18C_CheckCondition_IsNotAtWYPTn(num)
    local table = JAFDTC_FA18C_GetRightDDI();
    local str = table["WYPT_Page_Number"]
    return (str ~= num)
end


--[[
function JAFDTC_FA18C_CheckCondition_BingoIsZero()
    local table = JAFDTC_FA18C_GetIFEI();
    local str = table["txt_BINGO"]
    return (str == "0")
end
--]]

--[[
function JAFDTC_FA18C_CheckCondition_InSequence(i)
    local table =JAFDTC_FA18C_GetRightDDI();
    local str = table["WYPT_SequenceData"]
    local noSpaces = str:gsub("%s+", "")
    for token in string.gmatch(noSpaces, "[^-]+") do
        if token == i then 
            return true
        end
    end
    return false
end
--]]

-- --------------------------------------------------------------------------------------------------------------------
--
-- functions: pre-planned system
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_FA18C_Func_UnSelectStore()
    local table = JAFDTC_GetDisplay(2)
    local selOBS06 = table["_1__id:134.2.BP_6_Break_X_Root.1"]
    local selOBS07 = table["_1__id:137.2.BP_7_Break_X_Root.1"]
    local selOBS08 = table["_1__id:140.2.BP_8_Break_X_Root.1"]
    local selOBS09 = table["_1__id:143.2.BP_9_Break_X_Root.1"]
    local selOBS10 = table["_1__id:146.2.BP_10_Break_X_Root.1"]

    if selOBS06 == "" then
        JAFDTC_Core_PerformAction(35, 3016, 1, 0, 200)	-- LMFD, OSB-06
    elseif selOBS07 == "" then
        JAFDTC_Core_PerformAction(35, 3017, 1, 0, 200)	-- LMFD, OSB-07
    elseif selOBS08 == "" then
        JAFDTC_Core_PerformAction(35, 3018, 1, 0, 200)	-- LMFD, OSB-08
    elseif selOBS09 == "" then
        JAFDTC_Core_PerformAction(35, 3019, 1, 0, 200)	-- LMFD, OSB-09
    elseif selOBS10 == "" then
        JAFDTC_Core_PerformAction(35, 3020, 1, 0, 200)	-- LMFD, OSB-10
    else
        JAFDTC_Log("ERROR: JAFDTC_FA18C_Func_UnSelectStore found no match")
    end
end

function JAFDTC_FA18C_Func_SelectStore(store)
    JAFDTC_FA18C_Func_UnSelectStore()
    JAFDTC_Core_Wait(200)

    local table = JAFDTC_GetDisplay(2)
    local storeOBS06 = table["_1__id:134.1"] or ""
    local storeOBS07 = table["_1__id:137.1"] or ""
    local storeOBS08 = table["_1__id:140.1"] or ""
    local storeOBS09 = table["_1__id:143.1"] or ""
    local storeOBS10 = table["_1__id:146.1"] or ""

    if storeOBS06 == store then
        JAFDTC_Core_PerformAction(35, 3016, 1, 0, 200)	-- LMFD, OSB-06
    elseif storeOBS07 == store then
        JAFDTC_Core_PerformAction(35, 3017, 1, 0, 200)	-- LMFD, OSB-07
    elseif storeOBS08 == store then
        JAFDTC_Core_PerformAction(35, 3018, 1, 0, 200)	-- LMFD, OSB-08
    elseif storeOBS09 == store then
        JAFDTC_Core_PerformAction(35, 3019, 1, 0, 200)	-- LMFD, OSB-09
    elseif storeOBS10 == store then
        JAFDTC_Core_PerformAction(35, 3020, 1, 0, 200)	-- LMFD, OSB-10
    else
        JAFDTC_Log("ERROR: JAFDTC_FA18C_Func_SelectStore found no match")
    end
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- frame handler
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_FA18C_AfterNextFrame(params)
    local mainPanel = GetDevice(0);
    local ipButton = mainPanel:get_argument_value(99);
    local hudVideoSwitch = mainPanel:get_argument_value(144);

    if ipButton == 1 then params["uploadCommand"] = "1" end
    if hudVideoSwitch >= 0.2 then params["showJAFDTCCommand"] = "1" end
    if hudVideoSwitch > 0 and hudVideoSwitch < 0.2 then params["hideJAFDTCCommand"] = "1" end
end