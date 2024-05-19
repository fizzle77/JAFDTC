# JAFDTC: Configuring Mirage M-2000C Airframes

*Version 1.0.0-B.31 of 5-May-24*

JAFDTC supports configuration of the following systems in the Mirage,

* Waypoints

Each of these areas is covered in more depth below. See the
[_User's Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md) and
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
for more on the aspects of JAFDTC that are common to multiple airframes.

# DCS Cockpit Integration

The Mirage does not map any cockpit controls to JAFDTC functions.

# Configurable Systems

Mirage configurations support settings spanning the systems listed earlier. The discussion in
this section focuses on elements that are unique to the Mirage while the
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
covers elements that are common across all airframes in JAFDTC.

## Waypoints

JAFDTC uses the common core
[Navigation System](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md#navigation-system-editors)
interface to configure the Mirage *Waypoint* system. The core system operates as described
in the 
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md).
The Mirage version of the core navigation system uses DDM (Degrees, Decimal Minuts) formatted
latitudes and longitudes and captures elevations in feet.

> Though the avionics in the Mirage can accept elevations in either feet or meters, JAFDTC only
> supports feet at this time.
