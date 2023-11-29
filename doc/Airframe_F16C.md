# JAFDTC: F-16C Viper Configurations

*Version 1.0.0 of TODO*

This section discusses Viper configurations and the component systems JAFDTC can set up.

## Configurable Systems

A Viper configuration supports settings spanning eight different systems as described below.

### Countermeasures (CMDS)

TODO

![](images/Viper_Sys_CMDS.png)

TODO

### Datalink (DLNK)

TODO

### HARM ALIC

TODO

### HARM HTS

TODO

### MFD Formats

TODO

![](images/Viper_Sys_MFD.png)

TODO

### Miscellaneous

TODO

![](images/Viper_Sys_Misc.png)

TODO

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