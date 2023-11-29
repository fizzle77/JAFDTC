# JAFDTC: Just Another #%*@^!& DTC

*Version 1.0.0 of TODO*

*Just Another #%*@^!% DTC* (JAFDTC) is a Windows application that allows you to upload data
typically saved on a data cartridge, such as steerpoints/waypoints and other avionics setup, into
a DCS model at the start of a flight.

This document covers the basic usage of the tool and describes general concepts applicable to all
supported airframes.

## Preliminaries

JAFDTC allows you to manage multiple avionics *Configurations*. In JAFDTC, a *Configuration* is
composed of individual *System Configurations*, or *Systems*, that correspond to systems in
the airframe such as radios, countermeasures, navigation, and so on. *Configurations* and
*Systems* are unique to a specific airframe. Each configuration has an associated name.

> Configuration names must be unique across all configurations for an airframe and may contain
> any character. Names are case-insensitive so "A2G" and "a2g" are treated as the same name.

JAFDTC allows you to link system configurations between different *Configurations* that target
the same airframe as we shall see below. When linked, changes to the source system configuration
are automatically reflected in all linked systems.

> Linking system configurations is described further
> [below](#system-editor-page).

The systems available in a configuration, along with the specific parameters within a system
that can be changed, vary from airframe to airframe. Some systems may not exist in some
airframes and "common" systems may operate differently and track different information in
different airframes.

Once built, a configuration can be uploaded into the airframe in DCS. JAFDTC walks through
the configuration, updating parameters in systems that do not match the default. For example,
consider a BINGO warning system. In this example, if you change the BINGO value from the
default, JAFDTC will update the BINGO value in the avionics when uploading. If you do not
change the value, JAFDTC will skip the system. 

## Configuration List Page

The main page of the JAFDTC user interface is the *Configuration List Page* shwon below. This
page provides a number of controls to manipulate configurations.

![](images/Core_Cfg_List_Page.png)

The main components of this page include,

* The *Current Airframe* combo box that selects a supported airframe.
* The *Command Bar* that allows you to manipulate configurations.
* The *Configuration List* with individual rows for each configuration for the selected
  airframe.
* The *Status Area* that holds inforamtion on the current DCS status and pilot.

The reaminder of this section will discuss each of these elements in more detail.

### Current Airframe Selection

The combo box control in the upper right of the interface allows you to select the airframe
currently in use. The configuration list making up the bulk of the page only displays
configurations for the selected airframe.

JAFDTC remembers the last airframe you selected and will return to that airframe the next time
it is launched.

### Configuration List

The bulk of the page is taken up by a list of defined configurations for the selected airframe.
Each row in this list corresponds to a configuration. On the left side of a row is the name of
the configuration and a brief summary of what it changes in the avionics. On the right side of
the row is a set of icons that also indicate which systems the configuration modifies. The page
only allows at most configuration to be selected at a time.

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

* *Add* will add a new configuration to the database after prompting for a name for the new
  configuration.
* *Edit* will open the
  [System Editor Page](#system-editor-page)
  for the selected configuration to allow you to edit the configuration. You can also edit a
  configuration by double-clicking on the configuration in the configuration list.
* *Duplicate* creates a copy of the selected configuration after prompting for a name for the
  copy of the configuration.
* *Paste* pastes information from the clipboard into the selected configuration.
* *Rename* renames the selected configuration.
* *Delete* removes the currently selected configuration from the database.
* *Import* creates a new configuration from a file previously created with the *Export*
  command.
* *Export* creates a file that contains the selected configuration suitable for import using
  the *Import* command.
* *Load to Jet* uploads the selected configuration to DCS, see
  [here](#interacting-with-dcs)
  for further details.

The overflow menu (exposed by clicking on the "..." button) holds two commands,

* *Settings* opens up the
  [JAFDTC Settings](#settings)
  dialog to allow you to change JAFDTC settings.
* *About* opens a dialog box that identifies the JAFDTC version and similar information.

Depending on the state of the system, commands may be disabled. For example, *Edit* is disabled
when there is no configuration selected and *Load to Jet* is disabled if DCS is not running
a mission with the appropriate airframe.

### Status Area

The status area occupies the bottom part of the configuration list page. On the right side of
this region is information showing the current status of DCS. There are three pieces of
status here, each marked with a red cross or green checkmark,

* *Lua Installed* indicates that the Lua support is properly installed in DCS.
* *Running* indicates that DCS is currently running (though not necessarily running a mission).
* *Pilot in Pit* indicates that DCS is currently running a mission along with the type of
  airframe currently in use.

For example,

![](images/Core_Cfg_DCS_Status.png)

shows two different DCS statuses. On the left, DCS is not running but the Lua support is
installed. On the right, DCS is running a mission with an F-16C Viper.

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

The *System List* provides the systems that make up the configuration. Each system has an
associated icon. The color of the icon indicates the state of the system: blue icons mark
systems whose configuration has changed from defaults, white icons mark systems that have not
been changed. Clicking on a row in this list changes the system editor to the right to edit
the selected system.

The bulk of the page is taken up by the system editor on the right. The content of this editor
depends on which system seleted from the *System List* to the left. See the
[airframe discussions](#airframe-specific-documentation)
for further details on the specific system editors. Though largely specific to a particular
airframe, many systems provide common *Reset* and *Link* buttons along the bottom of the
editor.

The *Reset* button restores the default settings to the selected system. The *Link* button
connects the system to another configuration. This allows you to, for example, have a single
common radio configuration that you can share across different configurations. This way,
you can make a single change to the shared configuration and have the linked systems update.

> Links are tracked per system. That is, Systems A and B in Configuration X can be linked
> to completely different configurations if desired.

Changes to a system are pushed to all linked (either directly or indirectly) systems.

> For example, assume Configuration A is linked to Configuration B and Configuration B is
> linked to Configuration C. All changes to Configuration C will be pushed to A and B.

While linked, edits to a linked configuration are disabled. The *Link* button changes based
on whether or not the system is linked,

![](images/Core_Cfg_Edit_Link.png)

When unlinked, the button displays "Link To". Clicking the button brings up a list of
potential configurations to link to. Once linked, the button changes to "Unlink From" and
identifies the specific configuration the system is presently linked to. When unlinking, the
system configuration is not changed, but will no longer receive updates.

## Settings

You can access the JAFDTC settings through the Settings button on the command bar overflow menu
as
[described earlier](#configuration-list-page). The settings dialog box appears as follows,

![](images/Core_Settings.png)

There are multiple controls in the settings,

* The *Wing Name* and *Callsign* text fields allow you to specify your wing and callsign.
  This information appears in the lower right corner of the
  [configuration list page](#configuration-list-page).
  Some airframes also use this inforamtion for configuration.
* The *JAFDTC Window Remains on Top* check box selects whether JAFDTC will always remain on
  top of the window stack, even while DCS has focus. This allows you to keep the DCS UI
  visible in non-VR operation.
* The *Install DCS Lua Support* button installs
  [DCS Lua support](#support-scripts)
  if the support is not currently installed (the button is disabled if support is in place).
* The *Uninstall DCS Lua Support* button will uninstall
  [DCS Lua support](#support-scripts)
  if the support is currently installed (the button is disabled if support is not in place).

JAFDTC saves its settings to a file in `Documents\JAFDTC`. Clicking "OK" will accept any
changes in the dialog, while "Cancel" will discard any changes.

## Local Configuration Storage

JAFDTC stores configuration and supporting files, such as settings, in the `Documents\JAFDTC`
folder. Configurations are found in the `Configs` folder. You can share configurations by
copying the appropriate `.json` file from the per-airframe directory in the `Configs` folder.

## DCS Integration

JAFDTC integrates with DCS to allow you to setup your jet according to a configuration.

### Interacting with DCS

The primary interaction between JAFDTC and DCS involves uploading configurations to the jet.
Some airframes support addition interactions with the JAFDTC UI as the
[airframe-specific documentation](#airframe-specific-documentation)
listed below describes.

To be able to upload a configuration for a particular airframe, four conditions must hold,

1. A configuration must be selected
2. The DCS scripting support must be
   [installed](#support-scripts)
3. DCS must be running
4. A mission must be running with a pilot must be in pit in an airframe matching that of the
   configuration

The lower left corner of the main configuration list page indicates the status of these three
conditions as
[discussed earlier](#status-area).
In the earlier example, all three conditions hold with an F-16C Viper active. At this point, we
can use the jet button from the
[command bar](#command-bar)
or context menu item, to load the currently selected configuration into the jet. Note that
generally, the upload should take place before any changes are made with the avionics.

> In some cases, it is difficult to impossible for JAFDTC to get the jet in a known configuration
> from a non-default starting point. In these situations, JAFDTC must rely on the avionics being
> in a known state coming out of a cold or hot start. For example, if there is a 4-position
> switch whose setting JAFDTC is unable to read, JAFDTC will not be able to reliably set the
> switch to a particular setting except if assumes the switch hasn't changed positions since
> mission start.
>
> For these reasons, it is generally advisable to perform uploads prior to manually changing
> any avionics settings overlapping with the configuration.

Some airframes can also trigger JAFDTC operations through buttons in the cockpit. For example,
in the Viper, JAFDTC allows you to use the FLIR WX button to trigger configuration upload

> For further information on JAFDTC actions that can be triggered from the cockpit within DCS,
> see the airframe-specific documentation listed
> [below](#airframe-specific-documentation).



### Support Scripts
To interoperate with DCS, JAFDTC installs Lua within the `Scripts` hierarchy in the DCS
installation(s) present in the `Saved Games` folder associated with your profile. JAFDTC can
install this support in two places,

* `Saved Games\DCS\Scripts`
* `Saved Games\DCS.openbeta\Scripts`

JAFDTC will install in one or both of these directories based on which versions of DCS are
installed on your system. Within these areas, JAFDTC makes three changes,

* Adds scripts in the `Scripts\JAFDTC` folder that enable integration with supported airframes
* Adds a `JAFDTCHooks.lua` script to the `Scripts\Hooks` folder that enables integration with DCS
* Adds a line to `Scripts\Export.lua` to load JAFDTC support into DCS at mission start

JAFDTC will automatically update these files as needed, notifying you when an update is made.
While JAFDTC allows you to decline the installation, doing so will prevent JAFDTC from
interacting with DCS in any capacity.

## Airframe-Specific Documentation

The available configuration editors and their operation are specific to each aiframe. See,

* [A-10C Warthog](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_A10C.md)
* [F-16C Viper](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F16C.md)

for additional information.