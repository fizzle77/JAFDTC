# JAFDTC: F-16C Viper Configurations

*Version 1.0.0 of TODO*

This section discusses Viper configurations and the component systems JAFDTC can set up.

## Configurable Systems

A Viper configuration supports settings spanning eight different systems as described below.
Each of these systems implements the link and reset functionality mentioned in the overview
of the
[system editor page](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md#system-editor-page).

### Countermeasures (CMDS)

TODO

![](images/Viper_Sys_CMDS.png)

TODO

### Datalink (DLNK)

The datalink system manages parameters set through the DED DLNK page. These parameters control
operation of the datalink system and allow you to assign aircraft to your "team" for the
purpose of sharing information. By default, the system shares contact information between the
team and may optionally share information used by the HTS pod to locate emitters.

As an example, consider the following setup for a sortie where Venom and Jedi will work
together on SEAD tasking,

|Flight     |Pilot     |TNDL   |   |Flight    |Pilot      |TNDL   |
|:---------:|:--------:|:-----:|---|:--------:|:---------:|:-----:|
| Venom 1-1 | Rage     | 67333 |   | Jedi 1-1 | Raven     | 67327 |
| Venom 1-2 | Blackdog | 67404 |   | Jedi 1-2 | Cadet FNG | 12377 |

We have two 2-ships that are going to be part of the same team. We will set the data link up
such that Venom shows up as #1 and #2 on HSD and Jedi shows up as #5 and #6. All four jets will
share HTS information. From Raven's perspective, the datalink system editor would look as
follows,

![](images/Viper_Sys_DLNK_Top.png)

The top-most section of the page allows you to select the table entry for your ownship and
whether or not you are a flight lead. As we shall discuss shortly, JAFDTC will automatically
set the ownship table entry when it can determine which entry corresponds to the ownship.

> JAFDTC does not currently handle callsigns through the DED DLNK page.

The middle part of the page allows you to edit the entries in the table that specify the team
members. For each table entry, there are three controls,

* *TDOA* enables or disables sharing of HTS information within the team.
* *TNDL* specifies the five-digit octal code assigned to the jet (that is, 5 digits where all
  digits are between 0 and 7, inclusive).
* *Callsign* allows you to select a pilot from the pilot database or a "generic" pilot.

The TNDL field is only editable if the callsign control does not select a pilot from the pilot
database (in this case, the callsign control is blank as in entry 6 above). Further, selecting
yourself from the pilot database will cause JAFDTC to track the corresponding entry as your
ownship. Your pilot entry in the database is indicated by a bullet (see entry 5 above) and is
determined by looking for a callsign in the pilot database that matches the callsign set through
the
[JAFDTC settings](https://github.com/51st-Vfw/JAFDTC/tree/master/doc/README.md#settings).

All four pilots will set up the datalink team table with the same pilots in the *same* entries.
That is, all four pilots will have Rage in entry 1, Blackdog in entry 2, Raven in entry 5 and
FNG in entry 6. What each of them sees will differ though,

![](images/Viper_Sys_DLNK_Views.png)

Here, Rage's perspective is on the left and FNG's perspective is on the right. For Rage,

* JAFDTC recognizes entry 1 as ownship since the callsign ("Rage") matches the callsign in
  the JAFDTC settings and sets the ownship entry to 1 (note the ownship control is disabled).
* JAFDTC fills in the TNDL with the value from the pilot database (67333).
* As Rage is flight lead for Venom, he will check "Flight Lead".
* The table matches Raven's table above in terms of who within the sortie occupies which
  entries. Note that Rage's callsign is shown with the bullet from his perspective.

For FNG,

* The callsign is set to "generic" (that is, blank), FNG must set his ownship entry manually
  to 6 (note the ownship control is enabled).
* As FNG is a wingman ("...and if you're not a wingman, then to hell with you..." - DG), she
  does not check flight lead.
* As the callsign is generic, FNG must enter her TNDL (12377) into the TNDL field in entry 6.
* Again, the team members are the same.

The button below the team member table allows you to modify the pilot database. This database
associates callsigns with fixed TNDL numbers to allow you to rapidly construct datalink
configurations. Clicking on this button brings up the pilot database editor,

![](images/Viper_Sys_DLNK_PDb.png)

The dialog is made up of three regions:

* The list in the center lists the currently defined pilots and their assigned TNDLs.
* The top part of the dialog allows you to enter a new pilot and TNDL.
* The right edge of the dialog has controls to manipulate the database.

To add a new pilot, simply enter a unique callsign (case is ignored) along with a vaild 5-digit
TNDL, then click the `+` button. You will not be allowed to add a pilot until the callsign and
TNDL value are both valid.

To delete pilots, select them in the pilot list in the center of the display. The list support
multiple selections. After selecting the pilots to delete, click the trashcan icon to perform
the delete.

The import and export buttons allow you to share pilot databases between different systems. To
export, you select the pilots you want to share and click the export button. This will bring up
a standard file selection dialog that will let you specify a file to save the exported
information to.

### HARM ALIC

TODO

### HARM HTS

TODO

### MFD Formats

TODO

![](images/Viper_Sys_MFD.png)

TODO

### Miscellaneous

The miscellaneous system covers a number of smaller systems accessed through the Viper DED
including TACAN/ILS, ALOW, BNGO, BULL, LASR, and HMCS DED/UFC pages.

![](images/Viper_Sys_Misc.png)

Most of these settings should be self-apparent. Yardstick setup will set the TACAN to A/A mode.
The Symbology Intensity control will set the intensity knob to control the HMCS brighness.

### Radios (COM1/COM2)

TODO

### Steerpoints (STPT)

TODO

## DCS Cockpit Interactions

The Viper allows the user to operate JAFDTC from buttons in the cockpit without needing to go
through the Windows UI.

> This capability requires installation of the DCS Lua support. 

This capability reuses controls from the FLIR panel on the UFC as these controls are not used
by the Block 50 Viper that DCS models as the following figure illustrates,

![](images/Viper_UFC_JAFDTC.png)

JAFDTC currently supports three functions from the Viper cockpit,

* Pressing and holding the FLIR `WX` button for about 0.25s causes JAFDTC to start loading
  the currently selected configuration into the jet if it is compatible. JAFDTC provides
  audio feedback for the start (single beep), end (two beeps), and status (error buzz) of
  this operation.
* Flipping the 3-position FLIR `GAIN/LVL/AUTO` switch to `GAIN` will keep the JAFDTC window
  on top of the DCS window in the window stack, regardless of the "on top" setting.
* Flipping the 3-position FLIR `GAIN/LVL/AUTO` switch to `LVL` will allow the JAFDTC window
  to be below the DCS window in the window stack, regardless of the "on top" setting.

Other functions may be implemented later.