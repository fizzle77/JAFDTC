--[[
********************************************************************************************************************

F16CFunctions.lua -- viper airframe-specific lua functions

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

-- NOTE: requires that CommonFunctions.lua has been loaded...

-- --------------------------------------------------------------------------------------------------------------------
--
-- display parsers
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_GetDED()
    return JAFDTC_ParseDisplay(6)
end

function JAFDTC_F16CM_GetLeftMFD()
    return JAFDTC_ParseDisplay(4)
end

function JAFDTC_F16CM_GetRightMFD()
    return JAFDTC_ParseDisplay(5)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- core support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_DebugDED(msg)
    JAFDTC_Log("JAFDTC_F16CM_Fn_DebugDED - " .. msg)
    JAFDTC_DebugDisplay(JAFDTC_F16CM_GetDED())
end

function JAFDTC_F16CM_Fn_IsLeftHdptOn()
    local switch = GetDevice(0):get_argument_value(670)
    return (switch == 1)
end

function JAFDTC_F16CM_Fn_IsRightHdptOn()
    local switch = GetDevice(0):get_argument_value(671)
    return (switch == 1)
end

function JAFDTC_F16CM_Fn_IsInAAMode()
    local table = JAFDTC_F16CM_GetDED()
    local str = table["Master_mode"]
    return (str == "A-A")
end

function JAFDTC_F16CM_Fn_IsInAGMode()
    local table = JAFDTC_F16CM_GetDED()
    local str = table["Master_mode"]
    return (str == "A-G")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- dlnk support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsCallSignChar(position, letter)
    local table = JAFDTC_F16CM_GetDED()
    local value = table["CallSign Name char" .. position .. "_inv"]
    return (value == letter)
end

function JAFDTC_F16CM_Fn_IsFlightLead(status)
    local table = JAFDTC_F16CM_GetDED()
    local value = table["FL status"]
    return (value == status)
end

function JAFDTC_F16CM_Fn_IsTDOASet(slot)
    local table = JAFDTC_F16CM_GetDED()
    local value = table["STN TDOA value_" .. slot]
    return (value == "T")
end

--[[
function JAFDTC_F16CM_Fn_RadioNotBoth()
    local table = JAFDTC_F16CM_GetDED()
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
    local table = JAFDTC_F16CM_GetDED()
    local str = table["Misc Item 0 Name"]
    return (str == "HARM")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- hts support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsHTSOnDED()
    local table = JAFDTC_F16CM_GetDED()
    local str = table["Misc Item E Name"]
    return (str == "HTS")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- mfd support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsHTSOnMFD(mfd)
    local mfdTable
    if mfd == "left" then
        mfdTable = JAFDTC_F16CM_GetLeftMFD()
    else
        mfdTable = JAFDTC_F16CM_GetRightMFD()
    end
    local str = table["HAD_OFF_Lable_name"]
    return (str ~= "HAD")
end

function JAFDTC_F16CM_Fn_HTSAllNotSelected(mfd)
    local mfdTable
    if mfd == "left" then
        mfdTable = JAFDTC_F16CM_GetLeftMFD()
    else
        mfdTable = JAFDTC_F16CM_GetRightMFD()
    end
    local str = mfdTable["ALL Table. Root. Unic ID: _id:178. Text"]
    return (str == "ALL")
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- miscellaneous support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsBullseyeSelected()
    local table = JAFDTC_F16CM_GetDED()
    local str = table["BULLSEYE LABEL"]
    return (str ~= "BULLSEYE")
end

function JAFDTC_F16CM_Fn_IsTACANBand(band)
    local table = JAFDTC_F16CM_GetDED()
    local str = table["TCN BAND XY"]
    return (str == band)
end

function JAFDTC_F16CM_Fn_IsTACANMode(mode)
    local table = JAFDTC_F16CM_GetDED()
    local str = table["TCN Mode"]
    return (str == mode)
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- steerpoint support
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_F16CM_Fn_IsI2TNotSelected()
    local table = JAFDTC_F16CM_GetDED();
    local str1 = table["Visual initial point to TGT Label"]
    local str2 = table["Visual initial point to TGT Label_inv"]
    return not ((str1 == "VIP-TO-TGT") or (str2 == "VIP-TO-TGT"))
end

function JAFDTC_F16CM_Fn_IsI2TNotHighlighted()
    local table = JAFDTC_F16CM_GetDED()
    local str = table["Visual initial point to TGT Label"]
    return (str == "VIP-TO-TGT")
end

function JAFDTC_F16CM_Fn_IsI2PNotHighlighted()
    local table = JAFDTC_F16CM_GetDED()
    local str = table["Visual initial point to TGT Label"]
    return (str == "VIP-TO-PUP")
end

function JAFDTC_F16CM_Fn_IsT2RNotSelected()
    local table = JAFDTC_F16CM_GetDED()
    local str1 = table["Target to VRP Label"]
    local str2 = table["Target to VRP Label_inv"]
    return not ((str1 == "TGT-TO-VRP") or (str2 == "TGT-TO-VRP"))
end

function JAFDTC_F16CM_Fn_IsT2RNotHighlighted()
    local table = JAFDTC_F16CM_GetDED()
    local str = table["Target to VRP Label"]
    return (str == "TGT-TO-VRP")
end

function JAFDTC_F16CM_Fn_IsT2PNotHighlighted()
    local table = JAFDTC_F16CM_GetDED()
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