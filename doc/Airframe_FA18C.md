# JAFDTC: Configuring F/A-18C Hornet Airframes

*Version 1.0.0-B.31 of 5-May-24*

JAFDTC supports configuration of the following systems in the Hornet,

* Countermeasures
* Pre-Planned Weapons
* Radios
* Waypoints

Each of these areas is covered in more depth below. See the
[_User's Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md) and
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
for more on the aspects of JAFDTC that are common to multiple airframes.

# DCS Cockpit Integration

> This functionality requires installation of the DCS Lua support. 

The Hornet allows the user to operate JAFDTC from buttons in the cockpit without needing to go
through the Windows UI. This is helpful for VR and other situations where you may not be able
to interact with the JAFDTC window. To support this capabilty, JAFDTC reuses controls from
the UFC panel that have no function in the the F/A-18C that the module models.

![](images/Hornet_Cockpits_JAFDTC.png)

JAFDTC currently supports the following functions from the Hornet cockpit,

* **IP** &ndash; Pressing and briefly holding this button causes JAFDTC to load the
  currently selected F/A-18C configuration into the jet. JAFDTC provides feedback during the
  upload according to the **Upload Feedback**
  [setting](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md#settings).

Other functions may be implemented later.

# Configurable Systems

Hornet configurations support settings spanning the systems listed earlier. The discussion in
this section focuses on elements that are unique to the Hornet while the
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
covers elements that are common across all airframes in JAFDTC.

## Countermeasures

TODO

## Pre-Planned Weapons

TODO

## Radios

TODO

## Waypoints

TODO
