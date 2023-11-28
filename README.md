# JAFDTC: Just Another #%*@^!& DTC

*Just Another #%*@^!% DTC* (JAFDTC) is a Windows application that allows you to upload data
typically saved on a data cartridge, such as steerpoints/waypoints and other avionics setup, into
a DCS model at the start of a flight. JAFDTC is a native C# WinUI 3 application. The application
currently supports the following DCS airframes.

* A-10C Warthog
* F-16C Viper

Not all features are supported on all airframes. This document provides a quick overview
of JAFDTC. See the
[documentation](https://github.com/51st-Vfw/JAFDTC/tree/master/doc)
in the repository for detailed information.

JAFDTC is based on code from
[DCS-DTC](https://github.com/the-paid-actor/dcs-dtc),
[DCSWE](https://github.com/51st-Vfw/DCSWaypointEditor)
and a long line of similar tools floating around the DCS community for years.

## Installing & Building

A Windows `.msi` installation package for JAFDTC is available
[here](TODO)
Installation is easy,

1. Download the `.msi`
2. Double-click the `.msi`

The installation places a shortcut to JAFDTC to your desktop.

> On Windows 10, you may also need to install the *Segoe Fluent Icons* font from Microsoft
> that is available
> [here](https://learn.microsoft.com/en-us/windows/apps/design/downloads/#fonts).

When you run JAFDTC for the first time, it will setup a `JAFDTC` folder in your Windows
`Documents` folder to hold configurations and settings. It will also prompt you to
install DCS Lua support in any DCS installations in your `Saved Games` folder that
allows JAFDTC to be able to drive DCS.

## Uninstalling

You can remove JAFDTC using the Add/Remove Programs capability in Windows.

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


## JAFDTC and DCS

JAFDTC fills a niche for now, but it's lifetime and usefulness is likely bounded by Eagle
Dynamics. DCS will eventually acquire a native DTC solution for airframes that will likely
be more capable than what the community can do with a separate application.

## Other Credits

- [DCSWaypointEditor](https://github.com/Santi871/DCSWaypointEditor) Baseline source code
- [DCS-BIOS](https://github.com/DCSFlightpanels/dcs-bios) is redistributed under the GPLv3 license
- [PyMGRS](https://github.com/aydink/pymgrs) by aydink
