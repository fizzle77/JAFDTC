--[[
********************************************************************************************************************

AV8BFunctions.lua -- harrier airframe-specific lua functions

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

-- TODO: implement clickable cockpit and setup/configuration checks for harrier

-- --------------------------------------------------------------------------------------------------------------------
--
-- general functions
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_AV8B_Fn_NOP(msg)
    -- do nothing...
end

-- --------------------------------------------------------------------------------------------------------------------
--
-- frame handler
--
-- --------------------------------------------------------------------------------------------------------------------

function JAFDTC_AV8B_AfterNextFrame(params)
--[[
    params["uploadCommand"] = "0"
    params["incCommand"] = "0"
    params["decCommand"] = "0"
    params["showJAFDTCCommand"] = "0"
    params["hideJAFDTCCommand"] = "0"
    params["toggleJAFDTCCommand"] = "0"
]]
end