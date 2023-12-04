function JAFDTC_Log(str)
	JAFDTC_logFile:write(str .. "\n");
	JAFDTC_logFile:flush();
end

function JAFDTC_DCSLogInfo(str)
	log.write("JAFDTC", log.INFO, str)
end

function JAFDTC_DCSLogError(str)
	log.write("JAFDTC", log.ERROR, str)
end

function JAFDTC_ParseDisplay(indicator_id)  -- Thanks to [FSF]Ian code
	local t = {}
	local li = list_indication(indicator_id)
	local m = li:gmatch("-----------------------------------------\n([^\n]+)\n([^\n]*)\n")
	while true do
    	local name, value = m()
    	if not name then break end
   			t[name]=value
	end
	return t
end

function JAFDTC_GetPlayerAircraftType()
    local data = LoGetSelfData();
    if data then
	    local model = data["Name"];
        if model == "TODO_HARRIER" then return "AV8B" end
        if model == "A-10C_2" then return "A10C" end
        if model == "F-15ESE" then return "F15E" end
        if model == "F-16C_50" then return "F16CM" end
        if model == "FA-18C_hornet" then return "FA18C" end
	    return model;
    end
    return "Unknown"
end

function JAFDTC_SerializeDisplay(val, name, skipnewlines, depth)
    skipnewlines = skipnewlines or false
    depth = depth or 0

    local tmp = string.rep(" ", depth)

    if name then tmp = tmp .. name .. " = " end

    if type(val) == "table" then
        tmp = tmp .. "{" .. (not skipnewlines and "\n" or "")

        for k, v in pairs(val) do
            tmp =  tmp .. JAFDTC_SerializeDisplay(v, k, skipnewlines, depth + 1) .. "," .. (not skipnewlines and "\n" or "")
        end

        tmp = tmp .. string.rep(" ", depth) .. "}"
    elseif type(val) == "number" then
        tmp = tmp .. tostring(val)
    elseif type(val) == "string" then
        tmp = tmp .. string.format("%q", val)
    elseif type(val) == "boolean" then
        tmp = tmp .. (val and "true" or "false")
    else
        tmp = tmp .. "\"[inserializeable datatype:" .. type(val) .. "]\""
    end

    return tmp
end

function JAFDTC_DebugDisplay(display)
	local tbl = JAFDTC_SerializeDisplay(display);
	JAFDTC_Log(tbl);
end