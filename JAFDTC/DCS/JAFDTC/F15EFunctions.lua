--[[
********************************************************************************************************************

F15EFunctions.lua -- mudhen airframe-specific lua functions

Copyright(C) 2021-2023 the-paid-actor & dcs-dtc contributors
Copyright(C) 2023-2025 ilominar/raven

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

JAFDTC_Log("Loading F15EFunctions.lua");

-- --------------------------------------------------------------------------------------------------------------------
--
-- display parsers
--
-- --------------------------------------------------------------------------------------------------------------------

--Displays
-- 0 - NF FOV
-- 1 - HUD
-- 2 - Blank
-- 3 - Left MPD
-- 4 - ??
-- 5 - MPCD
-- 6 - ??
-- 7 - Right MPD
-- 8 - ??
-- 9 - Front UFC
-- 10 - RLMPCD
-- 12 - RLMPD
-- 14 - RRMPD
-- 16 - RRMPCD
-- 18 - Rear UFC

function JAFDTC_F15E_GetFrontLeftMPD()
    return JAFDTC_ParseDisplaySimple(3)
end

function JAFDTC_F15E_GetFrontRightMPD()
    return JAFDTC_ParseDisplaySimple(7)
end

function JAFDTC_F15E_GetFrontMPCD()
    return JAFDTC_ParseDisplaySimple(5)
end

function JAFDTC_F15E_GetRearLeftMPCD()
    return JAFDTC_ParseDisplaySimple(10)
end

function JAFDTC_F15E_GetRearLeftMPD()
    return JAFDTC_ParseDisplaySimple(12)
end

function JAFDTC_F15E_GetRearRightMPD()
    return JAFDTC_ParseDisplaySimple(14)
end

function JAFDTC_F15E_GetRearRightMPCD()
    return JAFDTC_ParseDisplaySimple(16)
end

function JAFDTC_F15E_GetUFC(disp)
    if disp == "UFC_PILOT" then
        return JAFDTC_ParseDisplaySimple(9)		-- front ufc
    end
        return JAFDTC_ParseDisplaySimple(18)		-- rear ufc
end

function JAFDTC_F15E_GetDisplay(disp)
    local table;
    if disp == "FLMPD" then
        table = JAFDTC_F15E_GetFrontLeftMPD();
    elseif disp == "FRMPD" then
        table = JAFDTC_F15E_GetFrontRightMPD();
    elseif disp	== "FMPCD" then
        table = JAFDTC_F15E_GetFrontMPCD();
    elseif disp	== "RLMPCD" then
        table = JAFDTC_F15E_GetRearLeftMPCD();
    elseif disp	== "RLMPD" then
        table = JAFDTC_F15E_GetRearLeftMPD();
    elseif disp	== "RRMPD" then
        table = JAFDTC_F15E_GetRearRightMPD();
    elseif disp	== "RRMPCD" then
        table = JAFDTC_F15E_GetRearRightMPCD();
    end
    return table
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- general functions
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F15E_Fn_NOP(msg)
    -- do nothing...
end

function JAFDTC_F15E_Fn_GoToFrontCockpit()
    LoSetCommand(7)
    if JAFDTC_F15E_Fn_IsInFrontCockpit() == false then
        LoSetCommand(1602)
    end
end

function JAFDTC_F15E_Fn_GoToRearCockpit()
    LoSetCommand(7)
    if JAFDTC_F15E_Fn_IsInRearCockpit() == false then
        LoSetCommand(1602)
    end
end

function JAFDTC_F15E_Fn_IsInFrontCockpit()
    local table = JAFDTC_F15E_GetFrontLeftMPD()
    return (next(table) ~= nil)
end

function JAFDTC_F15E_Fn_IsInRearCockpit()
    local table = JAFDTC_F15E_GetRearLeftMPD()
    return (next(table) ~= nil)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- mpd support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F15E_Fn_IsNoDisplaysProgrammed(disp)
    local table = JAFDTC_F15E_GetDisplay(disp);
    local str = table["PRG_Label_1"] or table["PRG_Label_2"] or table["PRG_Label_3"] or ""
    return (str == "")
end

function JAFDTC_F15E_Fn_IsProgBoxed(disp)
    local table = JAFDTC_F15E_GetDisplay(disp);
    local pb06 = table["PRG_PB06_T"] or "";
    return (pb06 == "PROG")
end

function JAFDTC_F15E_Fn_IsDisplayInMainMenu(disp)
    local table = JAFDTC_F15E_GetDisplay(disp);
    local pb06 = table["PB06"] or "";
    local pb11 = table["PB11"] or "";
    return (pb06 == "PROG" and pb11 == "M2")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- radio support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F15E_Fn_IsRadioPresetOrFreqSelected(ufc, radio, mode)
    local table = JAFDTC_F15E_GetUFC(ufc);
    local radio1Preset = table["UFC_SC_06"] or "x";
    local radio2Preset = table["UFC_SC_07"] or "x";
    local radio1Freq = table["UFC_SC_05"] or "x";
    local radio2Freq = table["UFC_SC_08"] or "x";

    if radio == "1" then
        if mode == "preset" then
            return (string.sub(radio1Preset, 1, 1) == "*")
        elseif mode == "freq" then
            return (string.sub(radio1Freq, 1, 1) == "*")
        end
    elseif radio == "2" then
        if mode == "preset" then
            return (string.sub(radio2Preset, -1) == "*")
        elseif mode == "freq" then
            return (string.sub(radio2Freq, -1) == "*")
        end
    end

    return false
end

function JAFDTC_F15E_Fn_IsRadioGuardEnabledDisabled(ufc, radio, mode)
    local table = JAFDTC_F15E_GetUFC(ufc);
    local radio1Freq = table["UFC_SC_05"] or "x";
    local radio2Freq = table["UFC_SC_08"] or "x";
    radio2Freq = radio2Freq:gsub("*", ""):gsub("%s+", "")

    if radio == "1" then
        if mode == "enabled" then
            return string.sub(radio1Freq, -1) == "G"
        elseif mode == "disabled" then
            return string.sub(radio1Freq, -1) ~= "G"
        end
    elseif radio == "2" then
        if mode == "enabled" then
            return string.sub(radio2Freq, -1) == "G"
        elseif mode == "disabled" then
            return string.sub(radio2Freq, -1) ~= "G"
        end
    end

    return false
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- steerpoint support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F15E_Fn_IsStrDifferent(ufc, expected)
    local table = JAFDTC_F15E_GetUFC(ufc);
    local str = table["UFC_SC_01"] or "";
    return (str ~= expected)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- ufc support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F15E_Fn_IsTACANBand(ufc, band)
    local table = JAFDTC_F15E_GetUFC(ufc);
    local str = table["UFC_SC_01"] or "";
    return (str ~= "" and str.sub(str, -1) == band)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- frame handler
--
-- --------------------------------------------------------------------------------------------------------------------

local jafdtc_prevNucSwitchFront = "0"
local jafdtc_prevNucSwitchRear = "0"

function JAFDTC_F15E_AfterNextFrame(params)
    local mainPanel = GetDevice(0);
    local ipButtonFront = mainPanel:get_argument_value(297);		-- F_UFC_KEY_IP
    local ipButtonRear = mainPanel:get_argument_value(1322);		-- R_UFC_KEY_IP
    local emButtonFront = mainPanel:get_argument_value(287);		-- F_UFC_EMISL_BTN
    local emButtonRear = mainPanel:get_argument_value(1312);		-- R_UFC_EMISL_BTN
    local nucSwitchFront = mainPanel:get_argument_value(451);		-- F_NUC_N_CONS_CVR
    local nucSwitchRear = mainPanel:get_argument_value(1402);		-- R_NUC_N_CONS_CVR

    if ipButtonFront == 1 then params["uploadCommand"] = "1" end
    if ipButtonRear == 1 then params["uploadCommand"] = "1" end
    if emButtonFront == 1 then params["toggleJAFDTCCommand"] = "1" end
    if emButtonRear == 1 then params["toggleJAFDTCCommand"] = "1" end
    if jafdtc_prevNucSwitchFront == "0" then
        if nucSwitchFront == 1  then
            params["incCommand"] = "1"
        elseif nucSwitchFront == -1 then
            params["decCommand"] = "1"
        end
    end
    if jafdtc_prevNucSwitchRear == "0" then
        if nucSwitchRear == 1  then
            params["incCommand"] = "1"
        elseif nucSwitchRear == -1 then
            params["decCommand"] = "1"
        end
    end

    jafdtc_prevNucSwitchFront = string.format("%d", nucSwitchFront)
    jafdtc_prevNucSwitchRear = string.format("%d", nucSwitchRear)
end