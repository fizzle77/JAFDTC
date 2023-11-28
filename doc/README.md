# JAFDTC: Just Another #%*@^!& DTC

*Version 1.0.0 of TODO*

*Just Another #%*@^!% DTC* (JAFDTC) is a Windows application that allows you to upload data
typically saved on a data cartridge, such as steerpoints/waypoints and other avionics setup, into
a DCS model at the start of a flight.

This document covers the basic usage of the tool and describes general concepts applicable to all
supported airframes.

## Configuration List Page

TODO

## System Editor Page

The specific systems availble in a configuration varies from airframe to airframe. The system
editor page provides a list of systems from which you can display per-system editors.

## Settings

TODO

## DCS Integration

JAFDTC integrates with DCS to allow you to setup your jet according to a configuration.

### Interacting with DCS

The primary interaction between JAFDTC and DCS involves uploading configurations to the jet.
Some airframes support addition interactions with the JAFDTC UI as the
[airframe-specific documentation](#airframe-specific-documentation)
listed below describes.

To be able to upload a configuration for a particular airframe, three conditions must hold,

1. The DCS scripting support must be
   [installed](#support-scripts)
2. DCS must be running
3. A mission must be running with a pilot must be in pit in an airframe matching that of the
   configuration

The lower left corner of the main configuration list page indicates the status of these three
conditions.

**TODO: status line image**

In this example, all three conditions hold with an F-16C Viper active. At this point, we can use
the jet button or context menu item,

**TODO: jet icon image**

to load the currently selected configuration (an F-16C configuration in this example) into the
jet. Note that generally, the upload should take place before any changes are made with the
avionics.

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

## Local Storage

JAFDTC stores configuration and supporting files, such as settings, in the `Documents\JAFDTC`
folder. Configurations are found in the `Configs` folder. You can share configurations by
copying the appropriate `.json` file from the per-airframe directory in the `Configs` folder.

## Airframe-Specific Documentation

The available configuration editors and their operation are specific to each aiframe. See,

* [A-10C Warthog](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_A10C.md)
* [F-16C Viper](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Airframe_F16C.md)

for additional information.