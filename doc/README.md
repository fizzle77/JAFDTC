# JAFDTC: User's Guide
*Version 1.0.0 of TODO*

_Just Another #%*@^!% DTC_ (JAFDTC) is a Windows application that allows you to upload data
typically saved on a data cartridge, such as steerpoints/waypoints and other avionics setup,
into a DCS model at the start of a flight.

This document covers the basic usage of the tool and describes general concepts applicable to
all supported airframes. After reading through this overview, look over the airframe-specific
documentation for the supported airframes.

| Airframe | Configurable Systems |
|:--------:|---------|
| [A-10C Warthog](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_A10C.md) | Waypoints
| [AV-8B Harrier](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_AV8B.md) | Waypoints
| [F-14A/B Tomcat](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F14AB.md) | Waypoints
| [F-15E Strike Eagle](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F15E.md) | Radios, Miscellaneous Systems
| [F-16C Viper](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F16C.md) | Countermeasures, Datalink, HARM (ALIC, HTS), MFD Formats, Radios, Steerpoints, Miscellaneous DED Systems
| [F/A-18C Hornet](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_FA18C.md) | Countermeasures, Waypoints, Radios
| [Mirage M-2000C](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_M2000C.md) | Waypoints

The above links provide additional details on JAFDTC's capabilities and operation on a specific
airframe.

# Preliminaries

Before discussing the user interface, it is helpful to outline some of the key abstractions
that JAFDTC uses.

## Configurations & Systems

JAFDTC allows you to manage multiple avionics *Configurations*. In JAFDTC, a *Configuration* is
composed of multiple *System Configurations*, or *Systems*, that each correspond to systems
in an airframe such as radios, countermeasures, navigation, and so on. *Configurations* and
*Systems* are **unique** to a specific airframe, though differnt airframes may have systems that
provide similar functionality.

A name identifies a configuration in the user interface. This name is set up when the
configuration is first created and may be changed later.

> Configuration names must be unique across all configurations for an airframe and may contain
> any character. Names are case-insensitive so "A2G" and "a2g" are treated as the same name.

The specific systems available in a configuration, along with the system parameters that
JAFDTC can apply, vary from airframe to airframe (see the airframe-specific documentation
mentioned above for more information). Some systems may not exist in some airframes and
"common" systems may operate differently and track different information in different
airframes.

## Linking Systems

JAFDTC allows you to link *System Configurations* between different *Configurations* for the
same airframe. When linked, changes to the source system configuration are automatically
reflected in all linked systems. This allows you to "compose" configurations from shared
components.

Links are particularly useful when you have basic setups that you tend to reuse often. For
example, you might want to always configure your MFDs one way for A2G and another way for
A2A. Let's assume configurations for our airframe support an MFD system (MFD) that sets up
cockpit displays and a navigation system (NAV) that sets up steerpoints.

Once you setup your A2G and A2A MFD configurations, you can simply link to them from new
configurations to avoid having to set the MFDs up again in the new configuration.

![](images/Core_Cfg_Links.png)

Any change you make to the MFD system in "A2G Fav" or "A2A Fav" will be immediately
reflected in the configurations that link to these system configurations; in this example,
"A2G Mission", "A2A Mission", and "Range A2A". Once linked, only the original is editable.
That is, the A2G MFD system will be read-only in "A2G Mission" but may be edited through
"A2G Fav".

Links connect individual systems in two different configurations. Though "A2A Fav" and
"A2G Mission" have linked their MFD system configurations, they have completely independent
NAV system configurations.

Furhter, different systems can link to different configurations. In the above picutre,
"Range A2A" gets it's MFD setup from "A2A Fav" and its NAV setup from "KLAS STPTs". There
is no limit to the number of systems that may link to a particular setup.

> Linking system configurations is described further
> [below](#system-editor-page).

## Uploading Configurations to DCS

Once built, a *Configuration* can be uploaded into the corresponding airframe in DCS through
the scripting engine that DCS exposes to the local system. To upload, JAFDTC walks through the
configuration, updating system parameters that differ from their defaults in the jet. For
example, consider a BINGO warning system. If you change the BINGO value from the default for
the airframe, JAFDTC will update the BINGO value in the avionics when uploading. If you do
not change the value, JAFDTC will not make any changes to that parameter in the airframe.

See the
[discussion below](#interacting-with-dcs)
for more details.

# Configuration Storage

JAFDTC stores configuration and supporting files, such as settings, in the `Documents\JAFDTC`
folder. Configurations are found in the `Configs` folder. Generally, you should not need to
access the files in the `JAFDTC` folder as JAFDTC provides import and export functions (see
below) to allow the exchange of information.

> As with all things, there are exceptions. A good general rule is if the JAFDTC UI can do
> something, use the UI and don't try to work around it.

# User Interface Overview

The JAFDTC user interface is based around a single window that displays a list of configrations
for an airframe and allows you to edit the specfic systems in a configuration. This section
covers some of the user interface features that are common to multiple airframes.

## Configuration List Page

The main page of the JAFDTC user interface is the *Configuration List Page* that provides
a number of controls to manipulate configurations,

![](images/Core_Cfg_List_Page.png)

The primary components of this page include,

- **Filter Search Box** &ndash; Filters the configurations shown in the configuration list.
- **Current Airframe** &ndash; Selects a supported airframe.
- **Command Bar** &ndash; Triggers commands to manipulate configurations.
- **Configuration List** &ndash; Lists the available configurations for the selected airframe.
- **Status Area** &ndash; Shows information on the current DCS status and pilot.

The reaminder of this section will discuss each of these elements in more detail.

### Filter Search Box

The filter box controls which configurations the
[configuration list](#configuration-list)
in the center of the page displays. To be displayed in the list, a configuration must match the
filter by containing the specified text. For example, typing `test` will match configurations
that contain "test" anywhere in their name (comparisons are always case-insensitive).

![](images/Core_Cfg_List_Filter.png)

As you type, the application will show a list of matching configurations. Typing `Return` or
clicking on the *Accept Filter Icon* will select the filter. You can pick a specific
configuration by clicking on its name in the matching configuration list. Clicking on the `X`
icon will remove any filtering and display all configurations.

### Current Airframe Selection

The combo box control in the upper right of the interface allows you to select the airframe
currently in use. The
[configuration list](#configuration-list)
making up the bulk of the page only displays configurations for the selected airframe. Changing
the value in this control will update the list to show only those configurations for the
selected airframe.

JAFDTC remembers the last airframe you selected and will return to that airframe the next time
it is launched.

### Configuration List

The bulk of the page is taken up by a list of defined configurations for the selected airframe.
Each row in this list corresponds to a configuration. On the left side of a row is the name of
the configuration and a brief summary of what it changes in the avionics. On the right side of
the row is a set of icons that also indicate which systems the configuration modifies. Systems
that are linked to other systems are shown with a small gold dot in the lower right corner.
this page only allows at most configuration to be selected at a time.

Double-clicking a row will open up the
[System Editor Page](#system-editor-page)
for the configuration that allows you to edit information in the configuration. Right-clicking
on a row will bring up a context menu with additional options. 

### Command Bar

The command bar at the top of the page provides quick access to the operations you can perform
on Configurations. The following screen shot shows the command bar in its "open" state (accessed
by clicking on the "..." button).

![](images/Core_Command_Bar.png)

The command bar includes the following commands,

- **Add** &ndash; Adds a new configuration to the database after prompting for a name for the new
  configuration.
- **Edit** &ndash; Opens the
  [System Editor Page](#system-editor-page)
  for the selected configuration to allow you to edit the configuration. You can also edit a
  configuration by double-clicking on the configuration in the configuration list.
- **Duplicate** &ndash; Creates a copy of the selected configuration after prompting for a name for the
  copy of the configuration.
- **Paste** &ndash; Pastes information from the clipboard into the selected configuration.
- **Rename** &ndash; Renames the selected configuration.
- **Delete** &ndash; Removes the currently selected configuration from the database.
- **Import** &ndash; Creates a new configuration from a file previously created with the *Export*
  command.
- **Export** &ndash; Creates a file that contains the selected configuration suitable for import using
  the *Import* command.
- **Load to Jet** &ndash; Uploads the selected configuration to DCS, see
  [here](#interacting-with-dcs)
  for further details.

> Importing a configuration will implicitly clear all links to other configurations that may
> have been in place at the time of export.

The overflow menu (exposed by clicking on the "..." button) holds two commands,

- **Settings** &ndash; Opens up the
  [JAFDTC Settings](#settings)
  dialog to allow you to change JAFDTC settings.
- **About** &ndash; Opens a dialog box that identifies the JAFDTC version and similar information.

Depending on the state of the system, commands may be disabled. For example, **Edit** is disabled
when there is no configuration selected and **Load to Jet** is disabled if DCS is not running
a mission with the appropriate airframe.

### Status Area

The status area occupies the bottom part of the configuration list page. On the right side of
this region is information showing the current status of DCS. There are three pieces of
status here, each marked with a red cross or green checkmark,

- **Lua Installed** &ndash; Indicates that the Lua support is properly installed in DCS.
- **Running** &ndash; Indicates that DCS is currently running (though not necessarily running a mission).
- **Pilot in Pit** &ndash; Indicates that DCS is currently running a mission along with the type of
  airframe currently in use.

For example,

![](images/Core_Cfg_DCS_Status.png)

shows two different DCS statuses. On the left, DCS is not running but the Lua support is
installed. On the right, DCS is running a mission where the player is piloting an F-16C
Viper.

The left side of the status area identifies the pilot and wing as specified through the JAFDTC
[Settings](#settings).

## System Editor Page

The specific systems availble in a configuration vary from airframe to airframe. The system
editor page provides a list of systems from which you can display per-system editors as the
following figure illustrates.

![](images/Core_Cfg_Edit_Page.png)

At the top of the window is the name of the current configuration being edited along with a
back button that returns you to the
[Configuration List](#configuration-list-page)
page when clicked. Below these two items is text identifying the *Current Airframe*.

### System List

The *System List* provides the systems that make up the configuration. Each system has an
associated icon. The color of the icon indicates the state of the system: blue icons mark
systems whose configuration has changed from defaults, white icons mark systems that have not
been changed.

> JAFDTC uses the system highlight color; if you change it, the blue icons may be a different
> color based on your choice.

A small gold dot marks those systems that are linked to other configurations.

> A white icon with a gold dot indicates a system that is linked to another configuration
> in which the system has not been changed from defaults.

Clicking on a row in this list changes the system editor to the right to edit the selected
system.

### System Configuration Editor

The bulk of the page is taken up by the system editor on the right. The content of this editor
depends on which system seleted from the *System List* to the left. In the figure above, the
editor is showing the steerpoint list associated with the selected steerpoints system. See the
[airframe discussions](#airframe-specific-documentation)
for further details on the specific system editors. Though largely specific to a particular
airframe, many systems provide common *Reset* and *Link* buttons along the bottom of the
editor.

### Common Editor Controls

Depending on the system, the bottom edge of the system configuration editor may contain link
and reset buttons that provide common link and reset functions for systems.

The **Reset** button restores the default settings to the selected system. This button is
disabled when the system is in its default configruation.

The **Link** button connects the system to another configuration. This allows you to, for
example, have a single common radio system configuration that you can share across different
configurations (as discussed
[here](#linking-systems)).
This way, you can make a single change to the shared system configuration and have the linked
systems automatically update.

> Links are tracked per system. That is, Systems A and B in Configuration X can be linked
> to completely different configurations if desired.

> At present, links are **not** preserved across export/import operations.

Changes to a system are pushed to all linked (either directly or indirectly) systems.

> For example, assume System X in Configuration A is linked to Configuration B and System X
> in Configuration B is linked to Configuration C. Changes to System X in Configuration
> C will be reflected in the Configraution A and B setups for System X.

While linked, edits to the system configuration are disabled (the system configuration is
edited through the source configuration). The *Link* button changes based on whether or not
the system is linked,

![](images/Core_Cfg_Edit_Link.png)

When unlinked, the button displays "Link To". Clicking the button brings up a list of
potential source configurations the system can be linked to.

> In the earlier example
> [here](#linking-systems),
> to link the MFD configuration in "A2G Fav" to "A2G Mission" you would click the "Link To"
> button in the MFD system editor in "A2G Mission" and select "A2G Fav" from the list of
> possible configurations to link to.

Once linked, the button changes to "Unlink From" and identifies the specific configuration the
system is presently linked to. When unlinking, the system configuration does not change, but
will no longer receive updates from the source configuration. Icons for linked systems are
badged with a small gold dot as described earlier.

## System Editor Pages for Navigation Systems

Most aircraft in JAFDTC support a navigation system that allows _Navigation Points_ (i.e.,
waypoints or steerpoints) to be input into the avionics as a part of a configuration. While
the interface for managing navigation points does depend on the airframe, we will describe
some typically common behaviors here.

> This is an overview of common functionality. Specific support may differ from airframe to
> airframe. As usual, consult the airframe-specific documentation linked above for further
> details on a particular airframe.

### Navigation Point List
The first level of the interface presents a list of known navigation points,

![](images/Core_Base_Nav_List.png)

The navigation point list that makes up the bulk of the editor lists the known navigation
points, their coordinates, name, and other airframe-specific information. You can select one
or more navigation points from the list using the usual Windows interactions. The command bar
at the top of the editor allows you to manipulate selected items in the navigation point list.

![](images/Core_Base_Nav_Cmd.png)

The command bar includes the following commands,

- **Add** &ndash; Adds a new navigation point and opens up the detail editor to set it up.
- **Edit** &ndash; Opens the selected navigation point in the detail editor.
- **Copy** &ndash; Copy the selected navigation points to the clipboard.
- **Paste** &ndash; Paste navigation points from the clipboard into the system.
- **Delete** &ndash; Deletes the currnetly selected navigation points from the configuration.
- **Renumber** &ndash; Renumbers the navigation points starting from a specified number.
- **Capture** &ndash; Capture navigation points from the DCS F10 view and add them to the
  system.
- **Import** &ndash; Import navigation points from a file.
- **Export** &ndash; Export navigation points to a file.

All of these commands are typically available on all airframes.

### Navigation Point Editor

Editing a navigation point brings up an editor page,

![](images/Core_Base_Nav_Edit.png)

The page can differ significantly from airframe to airframe to adapt to the capabilities of the
navigation system in the airframe. For example, on the Viper, the navigation point editor page
would also include reference points such as OAP1 and OAP2.

The top section of the page allows you to set up the navigation point from a known
*point of interest*, such as an airfield, or capture its coordinates from the F10 map in DCS as
[discussed below](#capturing-coordinates). You can select a point of interest from the drop-
down menu; pressing the `+` button to the right of the drop-down copies its parameters (such
as latitude or longitude) into the navigation point.

The row below that identifies the navigation point being edited and, on the right, provides
three controls. The upward- and downward-pointing chevrons advance to the previous and next
navigation point, respectively, in the list. The `+` button in between the chevrons adds a new
navigation point to the end of the list.

The bulk of the page is taken up by an area for the navigation point parameters. In this
example, there are only four: name, latitude, longitude, and altitude. The specific format of
the information (for example, latitude in degrees-minutes-seconds versus degrees-decimal
minutes) depends on the airframe. As mentioned earlier, airframes may have additional fields
here depending on the capabilities of the system. These fields will turn red if they contain
an illegal value.

![](images/Core_Base_Nav_Error.png)

At the bottom of the page are “OK” and “Cancel” buttons to accept or cancel outstanding
changes. Clicking either of these buttons will take you back to the navigation point list.

### Capturing Coordinates for Navpoints

Both the navigation point list and navigation point editor pages allow you to capture
coordinates for navigation points from the DCS F10 map. When capturing from the list page,
you may capture multiple coordinates and either append new navigation points to the list or
update existing navigation points in the list,

![](images/Core_Base_Capture_C1.png)

When capturing from a navigation point editor, you are always replacing the coordinate of the
navigation point being edited. If you capture more than one coordinate in this case, only the
first coordinate is used.

Once capture begins, JAFDTC displays a dialog while you interact with DCS as
[discussed below](#capturing-coordinates),

![](images/Core_Base_Capture_C2.png)

JAFDTC will interact with the coordinate capture in DCS as long as this dialog is active. After
you have completed the capture in DCS, you click “Done” in this dialog to incorporate the
captured coordinates in the navigation system.

## Settings

You can access the JAFDTC settings through the Settings button on the command bar overflow menu
as
[described earlier](#configuration-list-page). The settings dialog box appears as follows,

![](images/Core_Settings.png)

There are multiple controls in the settings,

- **Wing Name**, **Callsign** &ndash; Specifies your wing and callsign. This information
  appears in the
  [status area](#status-area)
  of the
  [configuration list page](#configuration-list-page).
  Some airframes also use this inforamtion for configuration.
- **JAFDTC Window Remains on Top** &ndash; Selects whether JAFDTC will always remain on
  top of the window stack, even while DCS has focus. This allows you to keep the DCS UI
  visible in non-VR operation.
- **Check for New Versions at Launch** &ndash; Selects whether JAFDTC will check if a new
  version is available each time it is launched.
- **Install DCS Lua Support** &ndash; Installs
  [DCS Lua support](#support-scripts)
  if the support is not currently installed (the button is disabled if support is in place).
- **Uninstall DCS Lua Support** &ndash; Uninstalls
  [DCS Lua support](#support-scripts)
  if the support is currently installed (the button is disabled if support is not in place).

JAFDTC saves its settings to a file in `Documents\JAFDTC`. Clicking "OK" will accept any
changes in the dialog, while "Cancel" will discard any changes.

# DCS Integration

JAFDTC integrates with DCS to allow you to setup your jet according to a configuration and
perform other operations with DCS.

## Applying Configurations

The primary interaction between JAFDTC and DCS involves uploading configurations to the jet.
Some airframes also support additional interactions with the JAFDTC UI as the
[airframe-specific documentation](#airframe-specific-documentation)
describes.

To be able to upload a configuration for a particular airframe, four conditions must hold,

1. The DCS scripting support must be
   [installed](#support-scripts)
2. A configuration must be selected
3. DCS must be running
4. A mission must be running with a pilot must be in pit in an airframe that matche the
   airframe of the configuration selected in (1)

The lower left corner of the main configuration list page indicates the status some of these
conditions as
[discussed earlier](#status-area).
In the earlier example, all three conditions hold with an F-16C Viper active. At this point, we
can use the **Load to Jet** button from the
[command bar](#command-bar)
or context menu item, to load the currently selected configuration into the jet. Note that,
generally, the upload should take place before any changes are made with the avionics.

> In some cases, it is difficult to impossible for JAFDTC to get the jet in a known
> configuration from a non-default starting point. In these situations, JAFDTC must rely on the
> avionics being in a known state coming out of a cold or hot start. For example, if there is a
> 4-position switch whose setting JAFDTC is unable to read, JAFDTC will not be able to reliably
> set the switch to a particular setting except if assumes the switch hasn't changed positions
> since mission start.
>
> For these reasons, it is generally advisable to perform uploads prior to manually changing
> any avionics settings overlapping with the configuration.

Some airframes can also trigger JAFDTC operations through buttons in the cockpit. For example,
in the Viper, JAFDTC allows you to use the **FLIR WX** button to trigger configuration upload

> For further information on JAFDTC actions that can be triggered from within the cockpit in
> DCS, see the
> [airframe-specific documentation](#jafdtc-users-guide)
> listed at the start of this user's guide.

## Capturing Coordinates

JAFDTC can capture coordinates from the DCS F10 map to use in navigation points for the
navigation system in a configuration.

> As always, consult the
> [airframe-specific documentation](#jafdtc-users-guide)
> for details on what a specific airframe supports.

The JAFDTC side of this interaction was
[described earlier](#capturing-coordinates-for-navpoints).
Once JAFDTC presents the “Capturing” dialog, the interaction switches to the DCS F10 map.

> You must be in an in-mission slot viewing the F10 map in order to capture coordiantes.
> Capture does not work from the DCS Mission Editor.

Once in the F10 map, type `CTRL`-`SHIFT`-`J` to show the JAFDTC capture overlay on the F10
map,

![](images/Core_Base_Capture.png)

At the upper left of the overlay is a cursor made up of a `+` icon within a circle that
indicates where on the map coordinates are captured from. To the right of this are the
latitude, longitude, and elevation of the point under the `+`. To change the coordinate, move
the F10 map as normal.

> The overlay remains at a fixed location on the screen while the map moves under the overlay.

The remainder of the overlay includes a list of captured navigation points along with several
buttons to interact with the list.

- **Add STPT** &ndash; Adds the location under the cursor to the list of navigation points as a
  steerpoint.
- **Add TGT** &ndash; Adds the location under the cursor to the list of navigation points as a
  target.
- **Undo** &ndash; Removes the last navigation point added to the list.
- **Clear** &ndash; Clears the list of navigation points.
- **Send to JAFDTC** &ndash; Sends the navigation points in the list to JAFDTC to incorporate.

The handling of target versus steerpoints added by **Add STPT** and **Add TGT** commands
depends on the specific airframe.

After sending the navigation points to JAFDTC, you must dismissing the “Capturing” dialog as
[discussed above](#capturing-coordinates-for-navpoints).

## Support Scripts

To interoperate with DCS, JAFDTC installs Lua within the `Scripts` hierarchy in the DCS
installation(s) present in the `Saved Games` folder associated with your profile. JAFDTC can
install this support in two places,

- `Saved Games\DCS\Scripts`
- `Saved Games\DCS.openbeta\Scripts`

JAFDTC will install in one or both of these directories based on which versions of DCS are
installed on your system. Within these areas, JAFDTC makes three changes,

- Adds scripts in the `Scripts\JAFDTC` folder that enable integration with supported airframes
- Adds a `JAFDTCHooks.lua` script to the `Scripts\Hooks` folder that enables integration with DCS
- Adds a line to `Scripts\Export.lua` to load JAFDTC support into DCS at mission start

JAFDTC will automatically update these files as needed, notifying you when an update is made.

> If DCS is running when JAFDTC installs or updates the DCS support scripts, you should restart
> DCS to make sure DCS picks up the latest version of the DCS support.

While JAFDTC allows you to decline the installation, doing so will prevent JAFDTC from
interacting with DCS in any capacity.
