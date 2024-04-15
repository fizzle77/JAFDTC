# JAFDTC: Configuring F-15E Strike Eagle Airframes

*Version 1.0.0-B.21 of 28-Jan-24*

JAFDTC supports configuration of the following systems in the Strike Eagle,

- Communications
- Miscellaneous systems such as TACAN, ILS, BINGO, and altitude warning
- Steerpoints including routes, target points, and reference points

Each of these areas is covered in more depth below. See the
[user's guide](https://github.com/51st-Vfw/JAFDTC/tree/master/doc)
for more on the aspects of JAFDTC that are common to multiple airframes.

# Configurable Systems

A Strike Eagle configuration supports settings spanning three systems as described below. Each
of these systems implements the link and reset functionality mentioned in the overview of the
[system editor page](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md#system-editor-page).

Priort to uploading, you should ensure the relevant systems are powered up and functional.
Typically, uploads should occur as one of the last steps prior to taxi once you have systems
powered up, stores loaded, and so on.

## Communications

TODO

## Miscellaneous

TODO

## Steerpoints

The steerpoint configuration allows you to update the navigation system on the Mudhen. This
configuration includes steerpoints as well as target points and reference points relative to a
steerpoint. This editor extends the interface of the common navigation system editor the
[user's guide](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md#navigation-system-editors)
describes.

### Steerpoint List User Interface

The *Steerpoint List* editor lists the steerpoints currently known to the configuration.
This editor extends the interface of the common *Navigation Point List* the
[user's guide](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md#navigation-point-list)
describes.

TODO

### Steerpoint Editor User Interface

The *Steerpoint* editor edits a steerpoint currently known to the configuration along with any
associated referenced points. This editor extends the interface of the common
*Navigation Point Editor* the
[user's guide](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md#navigation-point-editor)
describes.

TODO

### Importing

TODO: also need to handle "navigation target points" from me lua

When importing steerpoints from `.miz` or `.cf` files, JAFDTC can set up target and reference
points based on the steerpoint names provided by the file. This allows imports to include
information that the source tool (DCS Mission Editor, CombatFlite) may not directly support.

> When importing from `.json`, these reference points are automatically handled without
> requring any specific naming conventions.

JAFDTC looks for steerpoints that include a hashtag (that is, "`#`" followed by text) in their
name to set up reference points,

| Hashtag   |Purpose|
|:---------:|:------|
| `#T`      | Makes the steerpoint a target point |
| `#.<num>` | Reference point `<num>`, where `<num>` is between 1 and 7, inclusive |

Steerpoints whose name contains an unknown hashtag are always ignored.

For example, assume you have a `.miz` file with a Mudhen flight `DUDE1` with the following
steerpoints,

| # | Position | Name |
|:-:|:--------:|------|
| 1 |    P1    | `Ingress` |
| 2 |    P2    | `Enroute` |
| 3 |    P3    | `Enroute#.1` |
| 4 |    P4    | `IP` |
| 5 |    P5    | `Target` |
| 6 |    P6    | `Target#.1` |
| 7 |    P7    | `Homeplate` |

In this table, each steerpoint has a number, a position (latitude, longitude, and elevation),
and a name set through the appropriate fields in, for example, the DCS Mission Editor.
Importing the steerpoints for `DUDE1` leads to the following steerpoints and reference
points being set up in the navigation system,

| #  | Position | Name        | Target? | Reference Point 1 |
|:--:|:--------:|:-----------:|:-------:|:-----------------:|
| 1A |    P1    | `Ingress`   | No      | &ndash;
| 2A |    P2    | `Enroute`   | No      | 2.1A at P3
| 3A |    P4    | `IP`        | No      | &ndash;
| 4A |    P5    | `Target`    | Yes     | 5.01A at P6
| 5A |    P7    | `Homeplate` | No      | &ndash;

For brevity, this table only includes reference point 1 as other reference points do not appear
in this example.

# DCS Cockpit Integration

The Mudhen does not map any cockpit controls to JAFDTC functions.