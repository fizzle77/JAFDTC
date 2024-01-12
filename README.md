# JAFDTC: Just Another #%*@^!& DTC

Just Another #%*@^!% DTC (JAFDTC) is a native C# WinUI Windows application that allows you to
upload data typically saved on a data cartridge, such as steerpoints/waypoints and other
avionics setup, into a DCS model at the start of a flight. The application supports the
following DCS airframes and systems,

- *A-10C Warthog* &ndash; Waypoints
- *AV-8B Harrier* &ndash; Waypoints
- *F-14A/B Tomcat* &ndash; Waypoints
- *F-15E Strike Eagle* &ndash; Radios, Steerpoints, Miscellaneous Systems
- *F-16C Viper* &ndash; Countermeasures, Datalink, HARM ALIC, HARM HTS, MFD Formats, Miscellaneous DED
  Systems, Radios, Steerpoints
- *F/A-18C Hornet* &ndash; Countermeasures, Radios, Waypoints
- *Mirage M-2000C* &ndash; Waypoints

This document describes how to get JAFDTC installed and running on your system. See the
[user's guide](https://github.com/51st-Vfw/JAFDTC/tree/master/doc)
in the JAFDTC repository for detailed discussion of how to use JAFDTC and operate its features.

## Installing

A Windows `.msi` installation package for JAFDTC is available
[here](TODO).
Installation is easy,

1. Download the `.msi`
2. Double-click the `.msi`

The installation will place a shortcut to JAFDTC to your desktop.

> On Windows 10, you may also need to install the *Segoe Fluent Icons* font from Microsoft
> that is available
> [here](https://learn.microsoft.com/en-us/windows/apps/design/downloads/#fonts).

You may also build and install JAFDTC from source by cloning the
[JAFDTC repository](https://github.com/51st-Vfw/JAFDTC)
and building the application using Microsoft
[Visual Studio](https://visualstudio.microsoft.com/vs/).
The JAFDTC solution includes a project for the application itself along with a project based
on the release build that packages the application into a `.msi` file.

## Uninstalling

You can remove JAFDTC using the Add/Remove Programs function in Windows.

TODO: uninstall DCS Lua support instructions here...

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

## JAFDTC and DCS

JAFDTC fills a niche for now, but it's lifetime and usefulness is likely bounded by Eagle
Dynamics. DCS will eventually acquire a native DTC solution for airframes that will likely
be more capable and better integrated than what the community can do with a separate
application.

## Shoutouts

JAFDTC is based on code from
[DCS-DTC](https://github.com/the-paid-actor/dcs-dtc),
[DCSWE](https://github.com/51st-Vfw/DCSWaypointEditor),
and a long line of similar tools that have been developed by the community over the years.
