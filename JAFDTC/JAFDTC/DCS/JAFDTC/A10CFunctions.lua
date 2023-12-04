dofile(lfs.writedir()..'Scripts/JAFDTC/CommonFunctions.lua')

-- TODO: eventually implement setup/configuration checks

function JAFDTC_A10C_AfterNextFrame(params)
--[[
	local mainPanel = GetDevice(0);
	local ipButtonFront = mainPanel:get_argument_value(297);
	local ipButtonRear = mainPanel:get_argument_value(1322);
	local emButtonFront = mainPanel:get_argument_value(287);
	local emButtonRear = mainPanel:get_argument_value(1312);

	if ipButtonFront == 1 then params["uploadCommand"] = "1" end
	if ipButtonRear == 1 then params["uploadCommand"] = "1" end
	if emButtonFront == 1 then params["toggleJAFDTCCommand"] = "1" end
	if emButtonRear == 1 then params["toggleJAFDTCCommand"] = "1" end
]]
end