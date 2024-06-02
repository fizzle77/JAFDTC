# JAFDTC: Just Another #%*@^!& DTC

Just Another #%*@^!% DTC (JAFDTC) is a native C# WinUI Windows application for Windows 10 and
Windows 11 that allows you to upload data typically saved on a data cartridge, such as
steerpoints/waypoints and other avionics setup, into a DCS module at the start of a flight. The
application supports the following DCS airframes and systems,

- *A-10C Warthog* &ndash; DSMS, HMCS, Radios, TGP, Waypoints, Miscellaneous Systems
- *AV-8B Harrier* &ndash; Waypoints
- *F-14A/B Tomcat* &ndash; Waypoints
- *F-15E Strike Eagle* &ndash; MPD/MPCD Formats, Radios, Steerpoints, Miscellaneous Systems
- *F-16C Viper* &ndash; Countermeasures, Datalink, HARM (ALIC, HTS), MFD Formats, Radios,
  SMS Munitions, Steerpoints, Miscellaneous Systems
- *F/A-18C Hornet* &ndash; Countermeasures, Pre-Planned Weapons, Radios, Waypoints
- *Mirage M-2000C* &ndash; Waypoints

This document describes how to get JAFDTC installed and running on your system. See the
[_User's Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc)
in the JAFDTC repository for detailed discussion of how to use JAFDTC and operate its features.

## Quick Overview

JAFDTC manages a set of airframe-specific "Configurations" that each contain information on how
avionics systems (e.g., navigation, countermeasures, radios) within a supported airframe should
be set up. It integrates with DCS to drive these configurations into the jet in DCS through
clickable cockpit controls. Basically, JAFDTC cannot set up anything you couldn't set up on your
own through DCS but it can set up far faster than a human.

Configurations can share system setups allowing you to compose a setup from existing
configurations. For example, you could build a configuration with a common radio setup and then
"link" to that setup in other configurations so that changes to the common radio setup are
automatically reflected in other configurations.

When you run JAFDTC for the first time, it will setup a `JAFDTC` folder in your Windows
`Documents` folder to hold configurations and settings. It will also prompt you to
install DCS Lua support in any DCS installations in your `Saved Games` folder that
allows JAFDTC to interact with DCS.

A detailed user's guide is availble
[here](https://github.com/51st-Vfw/JAFDTC/tree/master/doc).

## Installing

A Windows `.msi` installation package for JAFDTC is available
[here](https://github.com/51st-Vfw/JAFDTC/releases).
Installation is easy,

1. Download the `.msi`
2. Double-click the `.msi`

The installation will place a shortcut to JAFDTC to your desktop.

> JAFDTC uses Microsoft's *Segoe Fluent Icons* font. This font is installed by default in
> Windows 11 but may be missing from some Windows 10 systems. When the font is missing, many
> icons in the JAFDTC interface will appear as empty boxes. See
> [troubleshooting](#troubleshooting)
> below for instructions on how to fix this issue.

You may also build and install JAFDTC from source by cloning the
[JAFDTC repository](https://github.com/51st-Vfw/JAFDTC)
and building the application using Microsoft
[Visual Studio](https://visualstudio.microsoft.com/vs/).
The JAFDTC solution includes a project for the application itself along with a project based
on the release build that packages the application into a `.msi` file.

## Updating

Generally, the safest way to update JAFDTC is to uninstall and reinstall. To do so,

1. Uninstall JAFDTC via Windows `Add/Remove Programs`
2. Check that there is no `51stVFW` directory in your `Program Files` directory or, if that
   directory exists, it is empty. If not, delete the `51stVFW` directory
3. Install the new version of JAFDTC by double-clicking on the `.msi` as described earlier.

Technically, JAFDTC should support in-place updates; however, there have been reports of
people having issues updating.

## Uninstalling

The settings dialog in the JAFDTC program allows you to remove the DCS Lua support. The support
can also be removed by hand by,

1. Delete the `Scripts\JAFDTC` directory from your DCS installation in `Saved Games`
2. Delete the `Scripts\Hooks\JAFDTCStatusMsgHook.lua` file from your DCS installation in
   `Saved Games`
3. Delete the `Scripts\Hooks\JAFDTCWyptCaptureHook.lua` file from your DCS installation in
   `Saved Games`
4. Edit the `Scripts\Export.lua` file with a text editor like `Notepad` and remove the line
   that starts with "`local JAFDTClfs=require('lfs');`"

You can remove JAFDTC itself using the Add/Remove Programs function in Windows.

## Troubleshooting

Sometimes stuff goes wrong. One thing to try is uninstalling JAFDTC and then re-installing from
a fresh `.msi`. After uninstalling from **Add/Remove Programs** in Windows, make sure the
`51stVFW` directory in `Program Files` is empty before you re-install. If it is not empty, you
can safely delete the `51stVFW` directory before re-installing.

JAFDTC uses the *Segoe Fluent Icons* font from Microsoft. If this font is not installed on your
system, icons in the JAFDTC UI may appear as boxes. To restore the icons,

1. Download the *Segoe Fluent Icons* font from Microsoft from the link on
   [this page](https://learn.microsoft.com/en-us/windows/apps/design/downloads/#fonts).
2. Right click the `.zip` file downloaded in step 1 and select "Extract" to unpack the `.zip`
   file.
3. In the extracted files, right-click on the `Segoe Fluent Icons.ttf` file and select
   "Install" or "Install for all Users" from the context menu.
4. Restart your system.

You only need to perfom these steps if simple square boxes show up as icons in the UI.

## JAFDTC and DCS

JAFDTC fills a niche for now, but it's lifetime and usefulness is likely bounded by Eagle
Dynamics. DCS will eventually acquire a native DTC solution for airframes that will likely
be more capable and better integrated than what the community can do with a separate
application.

## Shoutouts

JAFDTC is based on code from
[DCS-DTC](https://github.com/the-paid-actor/dcs-dtc),
[DCSWE](https://github.com/51st-Vfw/DCSWaypointEditor),
[DCSWaypointEditor](https://github.com/Santi871/DCSWaypointEditor),
[DCS-BIOS](https://github.com/DCS-Skunkworks/dcs-bios)
and a long line of similar tools that have been developed by the community over the years.
