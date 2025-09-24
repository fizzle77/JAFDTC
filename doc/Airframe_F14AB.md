# JAFDTC: Configuring F-14A/B Tomcat Airframes

**_Version 1.0.0 of 17-September-24_**

> Support for the *Tomcat* is experimental and has had limited testing.

JAFDTC supports configuration of the following systems in the F-14A/B Tomcat,

* Waypoints

Each of these areas is covered in more depth below. See the
[_User's Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md) and
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
for more on the aspects of JAFDTC that are common to multiple airframes.

# Configurable Systems

Tomcat configurations support settings spanning the systems listed earlier. The discussion in
this section focuses on elements that are unique to the Tomcat while the
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md)
covers elements that are common across all airframes in JAFDTC.

# DCS Cockpit Integration

The Tomcat does not map any cockpit controls to JAFDTC functions.

## Waypoints

JAFDTC uses the common core
[Navigation System](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md#navigation-system-editors)
interface to configure the Tomcat *Waypoint* system. The core system operates as described
in the 
[_Common Elements Guide_](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/Common_Elements.md).
The Tomcat version of the core navigation system uses DDM (Degrees, Decimal Minuts) formatted
latitudes and longitudes and captures elevations in feet.