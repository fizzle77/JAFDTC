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

dofile(lfs.writedir() .. 'Scripts/JAFDTC/commonFunctions.lua')

function JAFDTC_F16CM_GetDED()
	return JAFDTC_ParseDisplay(6)
end

function JAFDTC_F16CM_GetLeftMFD()
	return JAFDTC_ParseDisplay(4)
end

function JAFDTC_F16CM_GetRightMFD()
	return JAFDTC_ParseDisplay(5)
end

function JAFDTC_F16CM_CheckCondition_DebugDED()
	JAFDTC_DebugDisplay(JAFDTC_F16CM_GetDED())
	return true
end

function JAFDTC_F16CM_CheckCondition_CallSignChar1IsNot(letter)
    local table = JAFDTC_F16CM_GetDED();
    local value = table["CallSign Name char1_inv"] or "";
    if value ~= letter or str == "" then
        return true
    end
    return false
end

function JAFDTC_F16CM_CheckCondition_CallSignChar2IsNot(letter)
    local table = JAFDTC_F16CM_GetDED();
    local value = table["CallSign Name char2_inv"] or "";
    if value ~= letter or str == "" then
        return true
    end
    return false
end

function JAFDTC_F16CM_CheckCondition_FlightLead(status)
    local table = JAFDTC_F16CM_GetDED();
    local value = table["FL status"] or "";
    if value == status or str == "" then
        return true
    end 
    return false
end

function JAFDTC_F16CM_CheckCondition_TDOASet(slot)
	local table = JAFDTC_F16CM_GetDED()
	local value = table["STN TDOA value_" .. slot] or ""
	if value == "T" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_TDOANotSet(slot)
	local table = JAFDTC_F16CM_GetDED()
	local value = table["STN TDOA value_" .. slot] or ""
	if value ~= "T" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_LeftHdptNotOn()
	local switch = GetDevice(0):get_argument_value(670)
	if switch ~= 1 then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_RightHdptNotOn()
	local switch = GetDevice(0):get_argument_value(671)
	if switch ~= 1 then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_RadioNotBoth()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Receiver Mode"] or "";
	if str == "MAIN" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_HARM()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Misc Item 0 Name"];
	if str == "HARM" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_NotInAAMode()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Master_mode"];
	if str == "A-A" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_NotInAGMode()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Master_mode"];
	if str == "A-G" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_HTSAllNotSelected(mfd)
	local mfdTable;

	if mfd == "left" then
		mfdTable = JAFDTC_F16CM_GetLeftMFD();
	else
		mfdTable = JAFDTC_F16CM_GetRightMFD();
	end

	local str = mfdTable["ALL Table. Root. Unic ID: _id:178. Text"];
	if str == "ALL" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_HTSOnDED()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Misc Item E Name"];
	if str == "HTS" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_HTSOnMFD(mfd)
	local mfdTable;

	if mfd == "left" then
		mfdTable = JAFDTC_F16CM_GetLeftMFD();
	else
		mfdTable = JAFDTC_F16CM_GetRightMFD();
	end
	local str = table["HAD_OFF_Lable_name"];
	if str == "HAD" then
		return false
	end
	return true
end

function JAFDTC_F16CM_CheckCondition_BullseyeNotSelected()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["BULLSEYE LABEL"];
	if str == "BULLSEYE" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_TACANBandX()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["TCN BAND XY"];
	if str == "X" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_TACANBandY()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["TCN BAND XY"];
	if str == "Y" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_VIP_TO_TGT_NotSelected()
	local table = JAFDTC_F16CM_GetDED();
	local str1 = table["Visual initial point to TGT Label"] or "";
	local str2 = table["Visual initial point to TGT Label_inv"] or "";
	if (str1 == "VIP-TO-TGT") or (str2 == "VIP-TO-TGT") then
		return false
	end
	return true
end

function JAFDTC_F16CM_CheckCondition_VIP_TO_TGT_NotHighlighted()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Visual initial point to TGT Label"];
	if str == "VIP-TO-TGT" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_VIP_TO_PUP_NotHighlighted()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Visual initial point to TGT Label"];
	if str == "VIP-TO-PUP" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_TGT_TO_VRP_NotSelected()
	local table = JAFDTC_F16CM_GetDED();
	local str1 = table["Target to VRP Label"] or "";
	local str2 = table["Target to VRP Label_inv"] or "";
	if (str1 == "TGT-TO-VRP") or (str2 == "TGT-TO-VRP") then
		return false
	end
	return true
end

function JAFDTC_F16CM_CheckCondition_TGT_TO_VRP_NotHighlighted()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Target to VRP Label"];
	if str == "TGT-TO-VRP" then
		return true
	end
	return false
end

function JAFDTC_F16CM_CheckCondition_TGT_TO_PUP_NotHighlighted()
	local table = JAFDTC_F16CM_GetDED();
	local str = table["Target to VRP Label"];
	if str == "TGT-TO-PUP" then
		return true
	end
	return false
end

function JAFDTC_F16CM_AfterNextFrame(params)
	local mainPanel = GetDevice(0);
	local wxButton = mainPanel:get_argument_value(187);
	local flirIncDec = mainPanel:get_argument_value(188);
	local flirGLA = mainPanel:get_argument_value(189);

	if wxButton == 1 then params["uploadCommand"] = "1" end
	if flirIncDec == 1 then params["incCommand"] = "1" end
	if flirIncDec == -1 then params["decCommand"] = "1" end
	if flirGLA == 1 then params["showJAFDTCCommand"] = "1" end
	if flirGLA == 0 then params["hideJAFDTCCommand"] = "1" end
end