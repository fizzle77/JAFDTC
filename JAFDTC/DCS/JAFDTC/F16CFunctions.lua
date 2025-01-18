--[[
********************************************************************************************************************

F16CFunctions.lua -- viper airframe-specific lua functions

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

-- --------------------------------------------------------------------------------------------------------------------
--
-- display parsers
--
-- --------------------------------------------------------------------------------------------------------------------

-- 13 hsi
-- 16 cmds quantities

function JAFDTC_F16CM_GetParsedHUD()
    return JAFDTC_ParseDisplay(1)
end

function JAFDTC_F16CM_GetParsedLeftMFD()
    return JAFDTC_ParseDisplay(4)
end

function JAFDTC_F16CM_GetParsedRightMFD()
    return JAFDTC_ParseDisplay(5)
end

function JAFDTC_F16CM_GetParsedMFD(mfd)
    if mfd == "left" then
        return JAFDTC_F16CM_GetParsedLeftMFD()
    end
    return JAFDTC_F16CM_GetParsedRightMFD()
end

function JAFDTC_F16CM_GetParsedDED()
    return JAFDTC_ParseDisplay(6)
end

-- TODO: GetDisplay does a better job at representing the display at the expense of much longer keys than
-- TODO: ParseDisplay generates. for consistency, probably should move everything to that function. for now,
-- TODO: provide both options.

function JAFDTC_F16CM_GetLeftMFD()
    return JAFDTC_GetDisplay(4)
end

function JAFDTC_F16CM_GetRightMFD()
    return JAFDTC_GetDisplay(5)
end

function JAFDTC_F16CM_GetMFD(mfd)
    if mfd == "left" then
        return JAFDTC_F16CM_GetLeftMFD()
    end
    return JAFDTC_F16CM_GetRightMFD()
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- debug support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_DebugDumpDED(msg)
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDED - " .. msg)
    JAFDTC_DebugDisplay(JAFDTC_F16CM_GetParsedDED())
end

function JAFDTC_F16CM_Fn_DebugDumpLeftMFD(msg)
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDumpLeftMFD - " .. msg)
    JAFDTC_DebugDisplay(JAFDTC_F16CM_GetParsedLeftMFD())
end

function JAFDTC_F16CM_Fn_DebugDumpRightMFD(msg)
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDumpRightMFD (ParseDisplay) - " .. msg)
    JAFDTC_DebugDisplay(JAFDTC_F16CM_GetParsedRightMFD())
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDumpRightMFD (GetDisplay) - " .. msg)
    JAFDTC_DebugDisplay(JAFDTC_GetDisplay(5))
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- core support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_QueryNOP()
    return "NOP"
end

function JAFDTC_F16CM_Fn_NOP(msg)
    -- do nothing...
end

function JAFDTC_F16CM_Fn_IsLeftHdptOn()
    local switch = GetDevice(0):get_argument_value(670)
    return (switch == 1)
end

function JAFDTC_F16CM_Fn_IsRightHdptOn()
    local switch = GetDevice(0):get_argument_value(671)
    return (switch == 1)
end

-- NOTE: assumes ded is on master mode select page (list / 8)
--
function JAFDTC_F16CM_Fn_IsShowingAAMode()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Master_mode"] or table["Master_mode_inv"]
    return (str == "A-A")
end

-- NOTE: assumes ded is on master mode select page (list / 8)
--
function JAFDTC_F16CM_Fn_IsShowingAGMode()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Master_mode"] or table["Master_mode_inv"]
    return (str == "A-G")
end

-- NOTE: assumes ded is on master mode select page (list / 8)
--
function JAFDTC_F16CM_Fn_IsSelectingAAMode()
    local table = JAFDTC_F16CM_GetParsedDED()
    return (table["Master_mode_inv"] == "A-A")
end

-- NOTE: assumes ded is on master mode select page (list / 8)
--
function JAFDTC_F16CM_Fn_IsSelectingAGMode()
    local table = JAFDTC_F16CM_GetParsedDED()
    return (table["Master_mode_inv"] == "A-G")
end

function JAFDTC_F16CM_Fn_IsInNAVMode()
    local table = JAFDTC_F16CM_GetParsedHUD()
    local str = table["HUD_Window8_MasterMode"]
    return (str == "NAV")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- dlnk support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsCallSignChar(position, letter)
    local table = JAFDTC_F16CM_GetParsedDED()
    local value = table["CallSign Name char" .. position .. "_inv"]
    return (value == letter)
end

function JAFDTC_F16CM_Fn_IsFlightLead(status)
    local table = JAFDTC_F16CM_GetParsedDED()
    local value = table["FL status"]
    return (value == status)
end

function JAFDTC_F16CM_Fn_IsTDOASet(slot)
    local table = JAFDTC_F16CM_GetParsedDED()
    local value = table["STN TDOA value_" .. slot]
    return (value == "T")
end

--[[
function JAFDTC_F16CM_Fn_RadioNotBoth()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Receiver Mode"]
    if str == "MAIN" then
        return true
    end
    return false
end
--]]

-- --------------------------------------------------------------------------------------------------------------------
--
-- harm support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsHARMOnDED()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Misc Item 0 Name"]
    return (str == "HARM")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- hts support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsHTSOnDED()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Misc Item E Name"]
    return (str == "HTS")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- general mfd support
--
-- --------------------------------------------------------------------------------------------------------------------

-- return mfd state in format "<sel_osb>,<fmt_12>,<fmt_13>,<fmt_14>" where <sel_osb> is the number of the selected
-- osb ("0" if unknown), <fmt_x> is the format assigned to osb x.
--
function JAFDTC_F16CM_Fn_QueryMFDFormatState(mfd)
    local function GetSetOSBFormat(table, osb)
        local format = table["PB_Menu_Label_Black_PB_" .. osb] or table["PB_Menu_Label_" .. osb]
        if format == "   " then
            format = ""
        end
        return format
    end

    local function GetSetOSBSelected(table)
        if table["PB_Menu_Label_Black_PB_12"] ~= nil then
            return "12"
        elseif table["PB_Menu_Label_Black_PB_13"] ~= nil then
            return "13"
        elseif table["PB_Menu_Label_Black_PB_14"] ~= nil then
            return "14"
        end
        return "0"
    end

    local table = JAFDTC_F16CM_GetParsedMFD(mfd)
    return string.format("%s,%s,%s,%s",
                         GetSetOSBSelected(table),
                         GetSetOSBFormat(table, "12"), GetSetOSBFormat(table, "13"), GetSetOSBFormat(table, "14"))
end

function JAFDTC_F16CM_Fn_IsHTSOnMFD(mfd)
    local table = JAFDTC_F16CM_GetParsedMFD(mfd)
    local str = table["HAD_OFF_Lable_name"]
    return (str ~= "HAD")
end

function JAFDTC_F16CM_Fn_HTSAllNotSelected(mfd)
    local table = JAFDTC_F16CM_GetParsedMFD(mfd)
    local str = table["ALL Table. Root. Unic ID: _id:178. Text"]
    return (str == "ALL")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- sms format support
--
-- --------------------------------------------------------------------------------------------------------------------

-- return munition quantity and type from osb 6 on the mfd.
--
function JAFDTC_F16CM_Fn_QuerySMSMuniState(mfd)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    return table["Table. Root. Unic ID: _id:1321.2.Table. Root. Unic ID: _id:1321. Text.1"] or ""
end

function JAFDTC_F16CM_Fn_IsSMSMuniSelected(mfd, muniQT)
    local str = JAFDTC_F16CM_Fn_QuerySMSMuniState(mfd)
    return (str == muniQT)
end

function JAFDTC_F16CM_Fn_IsSMSCntlNumericPadNeg(mfd)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["DATA ENTRY Line PH 0.2.Scratchpad 0_txt_placeholder.2.Scratchpad 0_white_txt.1"] or ""
    return (string.find(str, "-%d*") ~= nil)
end

function JAFDTC_F16CM_Fn_IsSMSOnINV(mfd)
    local table = JAFDTC_F16CM_GetParsedMFD(mfd)
    --
    -- INV is easier to locate in the parsed output...
    --
    local str = table["INV Selectable Root. Unic ID: _id:3. Black Text"]
    return (str == "INV")
end

---- munition profile selection

function JAFDTC_F16CM_Fn_IsSMSProfile(mfd, prof)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:253.2.Table. Root. Unic ID: _id:253. Text.1"] or ""
    return (str == prof)
end

function JAFDTC_F16CM_Fn_IsSMSProfileGBU24(mfd, prof)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:1307.2.Table. Root. Unic ID: _id:1307. Text.2"] or ""
    return (str == prof)
end

---- munition employment selection

function JAFDTC_F16CM_Fn_IsSMSEmploymentWCMD(mfd, empl)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["WCMD_ROOT.2.WCMD_PAGE.2.PRE Table. Root. Unic ID: _id:1254.2.PRE Table. Root. Unic ID: _id:1254. Text.1"] or ""
    return (str == empl)
end

function JAFDTC_F16CM_Fn_IsSMSEmploymentJDAM(mfd, empl)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["JDAM_ROOT.2.JDAM_PAGE.2.PRE Table. Root. Unic ID: _id:1151.2.PRE Table. Root. Unic ID: _id:1151. Text.1"] or ""
    return (str == empl)
end

function JAFDTC_F16CM_Fn_IsSMSEmploymentGBU24(mfd, empl)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.PRE Table. Root. Unic ID: _id:1306.2.PRE Table. Root. Unic ID: _id:1306. Text.1"] or ""
    return (str == empl)
end

function JAFDTC_F16CM_Fn_IsSMSEmploymentMAV(mfd, empl)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["Table. Root. Unic ID: _id:1319.2.Table. Root. Unic ID: _id:1319. Text.1"] or ""
    return (str == empl)
end

---- munition fuze selection

function JAFDTC_F16CM_Fn_IsSMSFuze(mfd, fuze)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:257.2.Table. Root. Unic ID: _id:257. Text.1"] or ""
    return (str == fuze)
end

function JAFDTC_F16CM_Fn_IsSMSFuzeHD(mfd, fuze)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:257.2.Table. Root. Unic ID: _id:257. Text.2"] or ""
    return (str == fuze)
end

function JAFDTC_F16CM_Fn_IsSMSFuzeGBU24(mfd, fuze)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:1312.2.Table. Root. Unic ID: _id:1312. Text.1"] or ""
    return (str == fuze)
end

---- munition sgl/pair selection

function JAFDTC_F16CM_Fn_IsSMSReleaseType(mfd, rel)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:254.2.Table. Root. Unic ID: _id:254. Text.1"] or ""
    return (string.gsub(str, "%s*%d*%s*", "") == rel)
end

function JAFDTC_F16CM_Fn_IsSMSReleaseTypeGBU24(mfd, rel)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:1308.2.Table. Root. Unic ID: _id:1308. Text.1"] or ""
    return (str == rel)
end

function JAFDTC_F16CM_Fn_IsSMSReleaseTypeWCMD(mfd, rel)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = nil
    if rel == "SGL" then
        str = table["WCMD_ROOT.2.WCMD_PAGE.2.Impact_option_WCMD_PH.2.Single_WCMD_PH.2.Single_WCMD_triangle.1"]
    elseif rel == "PAIR_F2B" then
        str = table["WCMD_ROOT.2.WCMD_PAGE.2.Impact_option_WCMD_PH.2.Tandem_WCMD_PH.2.Tandem_WCMD_triangle_1.1"]
    elseif rel == "PAIR_L2R" then
        str = table["WCMD_ROOT.2.WCMD_PAGE.2.Impact_option_WCMD_PH.2.SBS_WCMD_PH.2.SBS_WCMD_triangle_1.1"]
    end
    return (str ~= nil)
end

---- munition spin selection

function JAFDTC_F16CM_Fn_IsSMSSpinWCMD(mfd, spin)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["WCMD_ROOT.2.WCMD_CNTL_PAGE.2.Table. Root. Unic ID: _id:1260.2.Table. Root. Unic ID: _id:1260. Text.2"] or ""
    return (str == spin)
end

---- munition arm delay selection

function JAFDTC_F16CM_Fn_IsSMSArmDelayGBU24(mfd, ad)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.GBU24_Arming_Delay.1"] or ""
    return (str == ad)
end

function JAFDTC_F16CM_Fn_IsSMSArmDelayJDAM(mfd, ad)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["JDAM_ROOT.2.JDAM_CNTL_PAGE.2.Table. Root. Unic ID: _id:1156.2.Table. Root. Unic ID: _id:1156. Text.1"] or ""
    return (str == ad)
end

---- munition ripple delay selection

function JAFDTC_F16CM_Fn_IsSMSRippleDelayGBU24(mfd, rd)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:1309.2.Table. Root. Unic ID: _id:1309. Text.1"] or ""
    return (str == rd)
end

---- munition ripple pulse selection

function JAFDTC_F16CM_Fn_IsSMSRipplePulseGBU24(mfd, rp)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:1310.2.Table. Root. Unic ID: _id:1310. Text.2"] or ""
    return (str == rp)
end

---- munition auto power selection

function JAFDTC_F16CM_Fn_IsSMSAutoPwrMAV(mfd, pwr)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["MAVERICK_ROOT.2.MAVERICK_CNTL_PAGE.2.Table. Root. Unic ID: _id:1104.2.Table. Root. Unic ID: _id:1104. Text.2"] or ""
    return (str == pwr)
end

function JAFDTC_F16CM_Fn_IsSMSAutoPwrModeMAV(mfd, app)
    local table = JAFDTC_F16CM_GetMFD(mfd)
    local str = table["MAVERICK_ROOT.2.MAVERICK_CNTL_PAGE.2.Table. Root. Unic ID: _id:1106.2.Table. Root. Unic ID: _id:1106. Text.1"] or ""
    return (str == app)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- miscellaneous support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsBullseyeSelected()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["BULLSEYE LABEL"]
    return (str ~= "BULLSEYE")
end

function JAFDTC_F16CM_Fn_IsTACANBand(band)
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["TCN BAND XY"]
    return (str == band)
end

function JAFDTC_F16CM_Fn_IsTACANMode(mode)
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["TCN Mode"]
    return (str == mode)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- steerpoint support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsI2TNotSelected()
    local table = JAFDTC_F16CM_GetParsedDED();
    local str1 = table["Visual initial point to TGT Label"]
    local str2 = table["Visual initial point to TGT Label_inv"]
    return not ((str1 == "VIP-TO-TGT") or (str2 == "VIP-TO-TGT"))
end

function JAFDTC_F16CM_Fn_IsI2TNotHighlighted()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Visual initial point to TGT Label"]
    return (str == "VIP-TO-TGT")
end

function JAFDTC_F16CM_Fn_IsI2PNotHighlighted()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Visual initial point to TGT Label"]
    return (str == "VIP-TO-PUP")
end

function JAFDTC_F16CM_Fn_IsT2RNotSelected()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str1 = table["Target to VRP Label"]
    local str2 = table["Target to VRP Label_inv"]
    return not ((str1 == "TGT-TO-VRP") or (str2 == "TGT-TO-VRP"))
end

function JAFDTC_F16CM_Fn_IsT2RNotHighlighted()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Target to VRP Label"]
    return (str == "TGT-TO-VRP")
end

function JAFDTC_F16CM_Fn_IsT2PNotHighlighted()
    local table = JAFDTC_F16CM_GetParsedDED()
    local str = table["Target to VRP Label"]
    return (str == "TGT-TO-PUP")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- frame handler
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_AfterNextFrame(params)
    local mainPanel = GetDevice(0)
    local wxButton = mainPanel:get_argument_value(187)
    local flirIncDec = mainPanel:get_argument_value(188)
    local flirGLA = mainPanel:get_argument_value(189)

    if wxButton == 1 then params["uploadCommand"] = "1" end
    if flirIncDec == 1 then params["incCommand"] = "1" end
    if flirIncDec == -1 then params["decCommand"] = "1" end
    if flirGLA == 1 then params["showJAFDTCCommand"] = "1" end
    if flirGLA == 0 then params["hideJAFDTCCommand"] = "1" end
end