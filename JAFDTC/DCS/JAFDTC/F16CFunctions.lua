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

JAFDTC_Log("Loading F16CFunctions.lua");

-- --------------------------------------------------------------------------------------------------------------------
--
-- display parsers
--
-- --------------------------------------------------------------------------------------------------------------------

local DID_F16C_HUD = 1
local DID_F16C_L_MFD = 4
local DID_F16C_R_MFD = 5
local DID_F16C_DED = 6
local DID_F16C_HSI = 13
local DID_F16C_CMDS_QTY = 16

-- TODO: long term, move away from simple parsing in favor of flat parsing.

function JAFDTC_F16CM_ParseMFDSimple(mfd)
    if mfd == "left" then
        return JAFDTC_ParseDisplaySimple(DID_F16C_L_MFD)
    else
        return JAFDTC_ParseDisplaySimple(DID_F16C_R_MFD)
    end
end

function JAFDTC_F16CM_ParseMFDFlat(mfd)
    if mfd == "left" then
        return JAFDTC_ParseDisplayFlat(DID_F16C_L_MFD)
    else
        return JAFDTC_ParseDisplayFlat(DID_F16C_R_MFD)
    end
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- debug support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_DebugDumpDED(msg)
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDED - " .. msg)
    JAFDTC_LogSerializedObj(JAFDTC_ParseDisplaySimple(DID_F16C_DED))
end

function JAFDTC_F16CM_Fn_DebugDumpLeftMFD(msg)
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDumpLeftMFD (ParseDisplaySimple) - " .. msg)
    JAFDTC_LogSerializedObj(JAFDTC_ParseDisplaySimple(DID_F16C_L_MFD))
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDumpLeftMFD (ParseDisplayFlat) - " .. msg)
    JAFDTC_LogSerializedObj(JAFDTC_ParseDisplayFlat(DID_F16C_L_MFD))
end

function JAFDTC_F16CM_Fn_DebugDumpRightMFD(msg)
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDumpRightMFD (ParseDisplaySimple) - " .. msg)
    JAFDTC_LogSerializedObj(JAFDTC_ParseDisplaySimple(DID_F16C_R_MFD))
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDumpRightMFD (ParseDisplayFlat) - " .. msg)
    JAFDTC_LogSerializedObj(JAFDTC_ParseDisplayFlat(DID_F16C_R_MFD))
end

function JAFDTC_F16CM_Fn_DebugDumpMFD(mfd, msg)
    if mfd == "left" then
        JAFDTC_F16CM_Fn_DebugDumpLeftMFD(msg)
    else
        JAFDTC_F16CM_Fn_DebugDumpRightMFD(msg)
    end
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
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["Master_mode"] or table["Master_mode_inv"]
    return (str == "A-A")
end

-- NOTE: assumes ded is on master mode select page (list / 8)
--
function JAFDTC_F16CM_Fn_IsShowingAGMode()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["Master_mode"] or table["Master_mode_inv"]
    return (str == "A-G")
end

-- NOTE: assumes ded is on master mode select page (list / 8)
--
function JAFDTC_F16CM_Fn_IsSelectingAAMode()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    return (table["Master_mode_inv"] == "A-A")
end

-- NOTE: assumes ded is on master mode select page (list / 8)
--
function JAFDTC_F16CM_Fn_IsSelectingAGMode()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    return (table["Master_mode_inv"] == "A-G")
end

function JAFDTC_F16CM_Fn_IsInNAVMode()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_HUD)
    local str = table["HUD_Window8_MasterMode"]
    return (str == "NAV")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- dlnk support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsCallSignChar(position, letter)
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local value = table["CallSign Name char" .. position .. "_inv"]
    return (value == letter)
end

function JAFDTC_F16CM_Fn_IsFlightLead(status)
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local value = table["FL status"]
    return (value == status)
end

function JAFDTC_F16CM_Fn_IsTDOASet(slot)
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local value = table["STN TDOA value_" .. slot]
    return (value == "T")
end

--[[
function JAFDTC_F16CM_Fn_RadioNotBoth()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
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
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["Misc Item 0 Name"]
    return (str == "HARM")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- hts support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsHTSOnDED()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
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

    local table = JAFDTC_F16CM_ParseMFDSimple(mfd)
    return string.format("%s,%s,%s,%s",
                         GetSetOSBSelected(table),
                         GetSetOSBFormat(table, "12"), GetSetOSBFormat(table, "13"), GetSetOSBFormat(table, "14"))
end

function JAFDTC_F16CM_Fn_IsHTSOnMFD(mfd)
    local table = JAFDTC_F16CM_ParseMFDSimple(mfd)
    local str = table["HAD_OFF_Lable_name"]
    return (str ~= "HAD")
end

function JAFDTC_F16CM_Fn_HTSAllNotSelected(mfd)
    local table = JAFDTC_F16CM_ParseMFDSimple(mfd)
    local str = table["ALL Table. Root. Unic ID: _id:178. Text"]
    return (str == "ALL")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- sms format support
--
-- --------------------------------------------------------------------------------------------------------------------

-- QuerySMSMuniState            get munition quantity/type from sms osb 6 label
--
-- IsSMSMuniSelected            t => sms osb 6 label (selected munition) matches specified munition
-- IsSMSCntlNumericPadNeg       t => sms data entry field currently holds a negative number
-- IsSMSOnMode                  t => sms master mode matches specified mode
-- IsSMSOnINV                   t => sms currently on inv subpage
-- IsSMSOnCNTL                  t => sms currently on cntl subpage

function JAFDTC_F16CM_Fn_QuerySMSMuniState(mfd)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    return table["Table. Root. Unic ID: _id:119.2.Table. Root. Unic ID: _id:119. Text.1"] or ""
end

function JAFDTC_F16CM_Fn_IsSMSMuniSelected(mfd, muniQT)
    local str = JAFDTC_F16CM_Fn_QuerySMSMuniState(mfd)
    return (str == muniQT)
end

function JAFDTC_F16CM_Fn_IsSMSCntlNumericPadNeg(mfd)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["DATA ENTRY Line PH 0.2.Scratchpad 0_txt_placeholder.2.Scratchpad 0_white_txt.1"] or ""
    return (string.find(str, "-%d*") ~= nil)
end

function JAFDTC_F16CM_Fn_IsSMSOnMode(mfd, mode)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["A-G Table. Root. Unic ID: _id:116.2.A-G Table. Root. Unic ID: _id:116. Text.1"] or ""
    return (str == mode)
end

function JAFDTC_F16CM_Fn_IsSMSOnINV(mfd)
    local table = JAFDTC_F16CM_ParseMFDSimple(mfd)
    --
    -- INV is easier to locate in the parsed output...
    --
    local str = table["INV Selectable Root. Unic ID: _id:3. Black Text"]
    return (str == "INV")
end

function JAFDTC_F16CM_Fn_IsSMSOnCNTL(mfd)
    local table = JAFDTC_F16CM_ParseMFDSimple(mfd)
    --
    -- CNTL is easier to locate in the parsed output...
    --
    local str = table["CNTL Selectable Root. Unic ID: _id:53. Black Text"]
    return (str == "INV")
end

---- munition profile selection

-- IsSMSProfile                 t => sms osb 7 label (profile: 1-4, "PROFn") matches specified state, other than gbu-24
-- IsSMSProfileGBU24            t => sms osb 7 label (profile: 1-4, "PROFn") matches specified state, gbu-24

function JAFDTC_F16CM_Fn_IsSMSProfile(mfd, prof)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:19.2.Table. Root. Unic ID: _id:19. Text.1"] or ""
    return (str == prof)
end

function JAFDTC_F16CM_Fn_IsSMSProfileGBU24(mfd, prof)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:105.2.Table. Root. Unic ID: _id:105. Text.2"] or ""
    return (str == prof)
end

---- munition employment selection

-- IsSMSEmploymentWCMD      t => sms osb 2 label (mode: pre/vis) matches specified state, wcmd
-- IsSMSEmploymentJDAM      t => sms osb 2 label (mode: pre/vis) matches specified state, jdam
-- IsSMSEmploymentGBU24     t => sms osb 2 label (mode: pre/vis) matches specified state, gbu-24
-- IsSMSEmploymentMAV       t => sms osb 2 label (mode: pre/vis/bore) matches specified state, mav

function JAFDTC_F16CM_Fn_IsSMSEmploymentWCMD(mfd, empl)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["WCMD_ROOT.2.WCMD_PAGE.2.PRE Table. Root. Unic ID: _id:91.2.PRE Table. Root. Unic ID: _id:91. Text.1"] or ""
    return (str == empl)
end

function JAFDTC_F16CM_Fn_IsSMSEmploymentJDAM(mfd, empl)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["JDAM_ROOT.2.JDAM_PAGE.2.PRE Table. Root. Unic ID: _id:66.2.PRE Table. Root. Unic ID: _id:66. Text.1"] or ""
    return (str == empl)
end

function JAFDTC_F16CM_Fn_IsSMSEmploymentGBU24(mfd, empl)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.PRE Table. Root. Unic ID: _id:104.2.PRE Table. Root. Unic ID: _id:104. Text.1"] or ""
    return (str == empl)
end

function JAFDTC_F16CM_Fn_IsSMSEmploymentMAV(mfd, empl)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["Table. Root. Unic ID: _id:117.2.Table. Root. Unic ID: _id:117. Text.1"] or ""
    return (str == empl)
end

---- munition fuze selection

-- IsSMSFuze                t => sms osb 18 label (fusing: nose/tail/nstl) matches specified state, non gbu-24 and hd
-- IsSMSFuzeHD              t => sms osb 18 label (fusing: nose/tail/nstl) matches specified state, hd
-- IsSMSFuzeGBU24           t => sms osb 18 label (fusing: nose/tail/nstl) matches specified state, gbu-24

function JAFDTC_F16CM_Fn_IsSMSFuze(mfd, fuze)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:23.2.Table. Root. Unic ID: _id:23. Text.1"] or ""
    return (str == fuze)
end

function JAFDTC_F16CM_Fn_IsSMSFuzeHD(mfd, fuze)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:23.2.Table. Root. Unic ID: _id:23. Text.2"] or ""
    return (str == fuze)
end

function JAFDTC_F16CM_Fn_IsSMSFuzeGBU24(mfd, fuze)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:110.2.Table. Root. Unic ID: _id:110. Text.1"] or ""
    return (str == fuze)
end

---- munition sgl/pair selection

-- IsSMSReleaseType         t => sms osb 8 label (release: sgl/pair) matches specified state, non gbu-24 and wcmd
-- IsSMSReleaseTypeGBU24    t => sms osb 8 label (release: sgl/pair) matches specified state, gbu-24
-- IsSMSReleaseTypeWCMD     t => sms osb 19 icon (release: sgl/l-r/f-b) matches specified state, wcmd

function JAFDTC_F16CM_Fn_IsSMSReleaseType(mfd, rel)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["BOMB_ROOT.2.BOMB_PAGE.2.Table. Root. Unic ID: _id:20.2.Table. Root. Unic ID: _id:20. Text.1"] or ""
    return (string.gsub(str, "%s*%d*%s*", "") == rel)
end

function JAFDTC_F16CM_Fn_IsSMSReleaseTypeGBU24(mfd, rel)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:106.2.Table. Root. Unic ID: _id:106. Text.1"] or ""
    return (str == rel)
end

function JAFDTC_F16CM_Fn_IsSMSReleaseTypeWCMD(mfd, rel)
-- TODO check?
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
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

-- IsSMSSpinWCMD            t => sms/cntl osb 17 label (spin: various in RPM, "?RPM") matches specified state, cbu-103

function JAFDTC_F16CM_Fn_IsSMSSpinWCMD(mfd, spin)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["WCMD_ROOT.2.WCMD_CNTL_PAGE.2.Table. Root. Unic ID: _id:97.2.Table. Root. Unic ID: _id:97. Text.2"] or ""
    return (str == spin)
end

---- munition arm delay selection

-- IsSMSArmDelayGBU24       t => sms page text (ad: various in s, "AD ?SEC") matches specified state, gbu-24
-- IsSMSArmDelayJDAM        t => sms/cntl osb 19 label (ad: various in s, "AD ?SEC") matches specified state, jdam

function JAFDTC_F16CM_Fn_IsSMSArmDelayGBU24(mfd, ad)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.GBU24_Arming_Delay.1"] or ""
    return (str == ad)
end

function JAFDTC_F16CM_Fn_IsSMSArmDelayJDAM(mfd, ad)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["JDAM_ROOT.2.JDAM_CNTL_PAGE.2.Table. Root. Unic ID: _id:71.2.Table. Root. Unic ID: _id:71. Text.1"] or ""
    return (str == ad)
end

---- munition ripple delay selection

-- IsSMSRippleDelayGBU24    t => sms osb 9 label (delay: 50-500ms by 50ms, "?MSEC") matches specified state, gbu-24

function JAFDTC_F16CM_Fn_IsSMSRippleDelayGBU24(mfd, rd)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:107.2.Table. Root. Unic ID: _id:107. Text.1"] or ""
    return (str == rd)
end

---- munition ripple pulse selection

-- IsSMSRipplePulseGBU24    t => sms osb 10 label (rp: 1/2/3/4, "?") matches specified state, gbu-24

function JAFDTC_F16CM_Fn_IsSMSRipplePulseGBU24(mfd, rp)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["GBU24_ROOT.2.GBU24_PAGE.2.Table. Root. Unic ID: _id:108.2.Table. Root. Unic ID: _id:108. Text.2"] or ""
    return (str == rp)
end

---- munition auto power selection

-- IsSMSAutoPwrMAV          t => sms/cntl osb 7 label (auto pwr: on/off) matches specified state, mav
-- IsSMSAutoPwrModeMAV      t => sms/cntl osb 20 label (mode: n/s/e/w of) matches specified state, mav

function JAFDTC_F16CM_Fn_IsSMSAutoPwrMAV(mfd, pwr)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["MAVERICK_ROOT.2.MAVERICK_CNTL_PAGE.2.Table. Root. Unic ID: _id:58.2.Table. Root. Unic ID: _id:58. Text.2"] or ""
    return (str == pwr)
end

function JAFDTC_F16CM_Fn_IsSMSAutoPwrModeMAV(mfd, app)
    local table = JAFDTC_F16CM_ParseMFDFlat(mfd)
    local str = table["MAVERICK_ROOT.2.MAVERICK_CNTL_PAGE.2.Table. Root. Unic ID: _id:60.2.Table. Root. Unic ID: _id:60. Text.1"] or ""
    return (str == app)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- dte format support
--
-- --------------------------------------------------------------------------------------------------------------------

-- for DCS 2.9.15.9408, dte load is done once the INV button (OSB 18) on DTE page 1 switches to inverted text.
--
-- 1 MPD        MPD Selectable Root. Unic ID: _id:37. Black Text
-- 1 COMM       COMM Selectable Root. Unic ID: _id:34. Black Text
-- 1 INV        INV Selectable Root. Unic ID: _id:31. Black Text
-- 1 PROF       PROF Selectable Root. Unic ID: _id:28. Black Text
-- 1 MSMD       MSMD Selectable Root. Unic ID: _id:25. Black Text
-- 1 CLSD       CLSD Selectable Root. Unic ID: _id:3. Black Text
-- 1 FCR        FCR Selectable Root. Unic ID: _id:9. Black Text
-- 1 ELINT      ELINT Selectable Root. Unic ID: _id:12. Black Text
-- 1 SMDL       SMDL Selectable Root. Unic ID: _id:15. Black Text
-- 1 TNDL A     ???
-- 1 NCTR       NCTR Selectable Root. Unic ID: _id:21. Black Text
--
function JAFDTC_F16CM_Fn_IsDTELoadDone(mfd)
    local table = JAFDTC_F16CM_ParseMFDSimple(mfd)
    --
    -- inverted text is easier to locate in the parsed output...
    --
    local str = table["INV Selectable Root. Unic ID: _id:31. Black Text"]
    return (str == "INV")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- miscellaneous support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsBullseyeSelected()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["BULLSEYE LABEL"]
    return (str ~= "BULLSEYE")
end

function JAFDTC_F16CM_Fn_IsTACANBand(band)
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["TCN BAND XY"]
    return (str == band)
end

function JAFDTC_F16CM_Fn_IsTACANMode(mode)
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["TCN Mode"]
    return (str == mode)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- steerpoint support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsI2TNotSelected()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED);
    local str1 = table["Visual initial point to TGT Label"]
    local str2 = table["Visual initial point to TGT Label_inv"]
    return not ((str1 == "VIP-TO-TGT") or (str2 == "VIP-TO-TGT"))
end

function JAFDTC_F16CM_Fn_IsI2TNotHighlighted()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["Visual initial point to TGT Label"]
    return (str == "VIP-TO-TGT")
end

function JAFDTC_F16CM_Fn_IsI2PNotHighlighted()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["Visual initial point to TGT Label"]
    return (str == "VIP-TO-PUP")
end

function JAFDTC_F16CM_Fn_IsT2RNotSelected()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str1 = table["Target to VRP Label"]
    local str2 = table["Target to VRP Label_inv"]
    return not ((str1 == "TGT-TO-VRP") or (str2 == "TGT-TO-VRP"))
end

function JAFDTC_F16CM_Fn_IsT2RNotHighlighted()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
    local str = table["Target to VRP Label"]
    return (str == "TGT-TO-VRP")
end

function JAFDTC_F16CM_Fn_IsT2PNotHighlighted()
    local table = JAFDTC_ParseDisplaySimple(DID_F16C_DED)
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