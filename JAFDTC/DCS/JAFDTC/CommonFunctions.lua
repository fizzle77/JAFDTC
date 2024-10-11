--[[
********************************************************************************************************************

CommonFunctions.lua -- jafdtc common functions

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

function JAFDTC_Log(str)
    if JAFDTC_LogFile then
        JAFDTC_LogFile:write(str .. "\n");
        JAFDTC_LogFile:flush();
    else
        log.write("JAFDTC", log.ERROR, str)
    end
end

function JAFDTC_ParseDisplay(indicator_id)  -- Thanks to [FSF]Ian code
    local t = { }
    local li = list_indication(indicator_id)
    local m = li:gmatch("-----------------------------------------\n([^\n]*)\n([^\n]*)\n")
    while true do
        local name, value = m()
        if not name then
            break
        elseif string.len(name) > 0 then
            t[name] = value
        end
    end
    return t
end

function JAFDTC_LinesToList(s)
    local i = 1
    local list = { }
    s:gsub("(.-)\n", function(s)
        list[i] = s; i = i + 1
    end)
    return list
end

function JAFDTC_FlattenTable(table)
    local flattened = { }
    for key, value in pairs(table) do
        if type(value) == "table" then
            local nested_flattened = JAFDTC_FlattenTable(value)
            for nested_key, nested_value in pairs(nested_flattened) do
                flattened[key .. "." .. nested_key] = nested_value
            end
        else
            flattened[key] = value
        end
    end
    return flattened
end

function JAFDTC_TrimString(str)
    return str:gsub("^%s*(.-)%s*$", "%1")
end

function JAFDTC_SplitString(str, delimiter)
    local result = {}
    for token in string.gmatch(str, "[^" .. delimiter .. "]+") do
        table.insert(result, token)
    end
    return result
end

function JAFDTC_GetDisplay(id)
    local SEP = "-----------------------------------------"
    local s = list_indication(id)
    local err = false
    if not s then
        return { }
    end
    local cur_table = { }
    local tables = { cur_table }
    local strlist = JAFDTC_LinesToList(s)
    if #strlist == 0 then
        return { }
    end
    local i = 1
    repeat
        if strlist[i] == SEP then
            i = i + 1
            while i <= #strlist and JAFDTC_TrimString(strlist[i]) == "" do
                i = i + 1
            end
            local name = JAFDTC_TrimString(strlist[i])
            i = i + 1
            local value = {}
            cur_table[name] = value
            while i <= #strlist and strlist[i] ~= SEP and strlist[i] ~= "}" and strlist[i] ~= "children are {" do
                value[#value + 1] = JAFDTC_TrimString(strlist[i])
                i = i + 1
            end
            if strlist[i] == "children are {" then
                new_table = {}
                tables[#tables + 1] = new_table
                value[#value + 1] = new_table
                cur_table = new_table
                i = i + 1
            end
        elseif strlist[i] == "}" then
            tables[#tables] = nil
            cur_table = tables[#tables]
            i = i + 1
        else
            err = true
            JAFDTC_Log("ERROR: Unexpected output, " .. strlist[i])
            i = i + 1
        end
    until i > #strlist
    if err then
        JAFDTC_Log("ERROR: " .. s)
    end
    return JAFDTC_FlattenTable(cur_table)
end

-- TODO: do a table lookup here
function JAFDTC_GetPlayerAircraftType()
    local data = LoGetSelfData();
    if data then
        local model = string.upper(data["Name"])
        if model == "AV8BNA" then return "AV8B" end
        if model == "A-10C_2" then return "A10C" end
        if model == "F-14A-135-GR" then return "F14AB" end
        if model == "F-14B" then return "F14AB" end
        if model == "F-15ESE" then return "F15E" end
        if model == "F-16C_50" then return "F16CM" end
        if model == "FA-18C_HORNET" then return "FA18C" end
        if model == "M-2000C" then return "M2000C" end
        return model;
    end
    return "Unknown"
end

function JAFDTC_SerializeDisplay(val, name, skipnewlines, depth)
    skipnewlines = skipnewlines or false
    depth = depth or 0

    local tmp = string.rep(" ", depth)

    if name then tmp = tmp .. "<" .. name .. "> = " end

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