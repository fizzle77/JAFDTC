dofile(lfs.writedir()..'Scripts/JAFDTC/commonFunctions.lua')

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
-- 9 - UFC
-- 10 - RLMPCD
-- 12 - RLMPD
-- 14 - RRMPD
-- 16 - RRMPCD

function JAFDTC_F15E_GetFrontLeftMPD()
	return JAFDTC_ParseDisplay(3)
end

function JAFDTC_F15E_GetFrontRightMPD()
	return JAFDTC_ParseDisplay(7)
end

function JAFDTC_F15E_GetFrontMPCD()
	return JAFDTC_ParseDisplay(5)
end

function JAFDTC_F15E_GetRearLeftMPCD()
	return JAFDTC_ParseDisplay(10)
end

function JAFDTC_F15E_GetRearLeftMPD()
	return JAFDTC_ParseDisplay(12)
end

function JAFDTC_F15E_GetRearRightMPD()
	return JAFDTC_ParseDisplay(14)
end

function JAFDTC_F15E_GetRearRightMPCD()
	return JAFDTC_ParseDisplay(16)
end

function JAFDTC_F15E_GetUFC()
	return JAFDTC_ParseDisplay(9)
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

function JAFDTC_F15E_CheckCondition_GoToFrontCockpit()
	LoSetCommand(7)
	if JAFDTC_F15E_CheckCondition_IsInFrontCockpit() == false then
		LoSetCommand(1602)
	end
	return true
end

function JAFDTC_F15E_CheckCondition_GoToRearCockpit()
	LoSetCommand(7)
	if JAFDTC_F15E_CheckCondition_IsInRearCockpit() == false then
		LoSetCommand(1602)
	end
	return true
end

function JAFDTC_F15E_CheckCondition_IsInFrontCockpit()
	local table = JAFDTC_F15E_GetFrontLeftMPD()
	if next(table) == nil then
		return false
	end
	return true
end

function JAFDTC_F15E_CheckCondition_IsInRearCockpit()
	local table = JAFDTC_F15E_GetRearLeftMPD()
	if next(table) == nil then
		return false
	end
	return true
end

--[[
function JAFDTC_ClickCockpit(device, cmd)
	device:performClickableAction(cmd, 1)
	device:performClickableAction(cmd, 0)
end

function JAFDTC_F15E_GetDisplayDevice(disp)
	if disp == "FLMPD" then
		return 34;
	elseif disp == "FRMPD" then
		return 36;
	elseif disp	== "FMPCD" then
		return 35;
	end
end

function JAFDTC_F15E_CheckCondition_HasProgrammedDisplays(disp)
	local table = JAFDTC_F15E_GetDisplay(disp);
	local p1 = table["PRG_Label_1"] or ""
	local p2 = table["PRG_Label_2"] or ""
	local p3 = table["PRG_Label_3"] or ""

	if p1 ~= "" then JAFDTC_F15E_ClearProgrammedDisplay(disp, p1) end
	if p2 ~= "" then JAFDTC_F15E_ClearProgrammedDisplay(disp, p2) end
	if p3 ~= "" then JAFDTC_F15E_ClearProgrammedDisplay(disp, p3) end

	return true
end

function JAFDTC_F15E_ClearProgrammedDisplay(disp, page)
	local d = GetDevice(JAFDTC_F15E_GetDisplayDevice(disp))
	if page == "ADI" then
		JAFDTC_ClickCockpit(d, 3061)
	elseif page == "ARMT" then
		JAFDTC_ClickCockpit(d, 3062)
	elseif page == "HSI" then
		JAFDTC_ClickCockpit(d, 3063)
	elseif page == "TSD" then
		JAFDTC_ClickCockpit(d, 3065)
	elseif page == "TPOD" then
		JAFDTC_ClickCockpit(d, 3072)
	elseif page == "TEWS" then
		JAFDTC_ClickCockpit(d, 3073)
	elseif page == "A/G RDR" then
		JAFDTC_ClickCockpit(d, 3074)
	elseif page == "A/A RDR" then
		JAFDTC_ClickCockpit(d, 3075)
	elseif page == "HUD" then
		JAFDTC_ClickCockpit(d, 3077)
	elseif page == "ENG" then
		JAFDTC_ClickCockpit(d, 3078)
	elseif page == "A/G DLVRY" then
		JAFDTC_ClickCockpit(d, 3071)
		JAFDTC_ClickCockpit(d, 3062)
		JAFDTC_ClickCockpit(d, 3071)
		JAFDTC_ClickCockpit(d, 3071)
	end
end
--]]

function JAFDTC_F15E_CheckCondition_IsRadioPresetOrFreqSelected(radio, mode)
	local table = JAFDTC_F15E_GetUFC(disp);
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

function JAFDTC_F15E_CheckCondition_IsRadioGuardEnabledDisabled(radio, mode)
	local table = JAFDTC_F15E_GetUFC(disp);
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

function JAFDTC_F15E_CheckCondition_IsTACANBand(band)
	local table = JAFDTC_F15E_GetUFC(disp);
	local str = table["UFC_SC_01"] or "";
	if str ~= "" and str.sub(str, -1) == band then
		return true
	end
	return false
end

function JAFDTC_F15E_CheckCondition_IsStrDifferent(expected)
	local table = JAFDTC_F15E_GetUFC(disp);
	local str = table["UFC_SC_01"] or "";
	if str ~= expected then
		return true
	end
	return false
end

function JAFDTC_F15E_CheckCondition_NoDisplaysProgrammed(disp)
	local table = JAFDTC_F15E_GetDisplay(disp);
	local str = table["PRG_Label_1"] or table["PRG_Label_2"] or table["PRG_Label_3"] or ""
	if str == "" then
		return true
	end
	return false
end

function JAFDTC_F15E_CheckCondition_IsProgBoxed(disp)
	local table = JAFDTC_F15E_GetDisplay(disp);
	local pb06 = table["PRG_PB06_T"] or "";
	if pb06 == "PROG" then
		return true
	end
	return false
end

function JAFDTC_F15E_CheckCondition_DisplayNotInMainMenu(disp)
	local table = JAFDTC_F15E_GetDisplay(disp);
	local pb06 = table["PB06"] or "";
	local pb11 = table["PB11"] or "";
	if pb06 == "PROG" and pb11 == "M2" then
		return false
	end
	return true
end

function JAFDTC_F15E_AfterNextFrame(params)
	local mainPanel = GetDevice(0);
	local ipButtonFront = mainPanel:get_argument_value(297);
	local ipButtonRear = mainPanel:get_argument_value(1322);
	local emButtonFront = mainPanel:get_argument_value(287);
	local emButtonRear = mainPanel:get_argument_value(1312);

	if ipButtonFront == 1 then params["uploadCommand"] = "1" end
	if ipButtonRear == 1 then params["uploadCommand"] = "1" end
	if emButtonFront == 1 then params["toggleJAFDTCCommand"] = "1" end
	if emButtonRear == 1 then params["toggleJAFDTCCommand"] = "1" end
end