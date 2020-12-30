# Space Engineers Trailer Manager

[On the Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=2341786466)

**Designed with [ISL](https://steamcommunity.com/sharedfiles/filedetails/?id=2316120850) compatibility as a goal**

## Features:

* Display information
* Allow bulk operations
  * Engage/disengage all parking brakes
  * Power down all wheels
  * All batteries to recharge
  * All batteries to auto
  * All H tanks to stockpile
  * All turrets on / off
  * Pack all
  * Unpack all
  * Unhitch rear-most trailer
  * Hitch to rear-most trailer
  * Toggle connector lock on rear-most trailer
* Allow individual operations
  * Pack
  * Unpack
  * Stockpile Hydrogen
  * Activate turrets
  * Hitch another trailer
  * Unhitch connected trailer
  * List and trigger timer blocks
  * Toggle connector lock
* Exceptional behaviour
  * Attempt to unpack recently unhitched trailers
  * Rebuild consist on demand (handy if trailers are altered whilst hitched)

## Trailer detection

### Names (legacy detection)

* Hinge bases with "Front" in their name
* Hinge bases with "Rear" or "Hitch" in their name
* Timers with "Unpack" in their name
* Timers with "Pack" in their name

### Custom Data

* Put an `.ini` section in the Custom Data of any block to be included

## ISL compatibility

### Packing

The pack operation will:

* Run the designated timer block
* Subsequently:
  * Ensure that parking brakes are off
  * Ensure that hinge locks are disabled

### Unpacking

The unpack operation will:

* Run the designated timer block
* Subsequently:
  * Ensure that parking brakes are on
  * Ensure that hinge locks are enabled

### Hitching

The hitch operation will:

* Attempt to hitch another trailer
* Attempt to pack the newly hitched trailer
* Re-build the internal model of the consist

### Unhitching

The unhitch operation will:

* Attempt to unpack the trailer attached to the hitch
* Unhitch the trailer
* Update the internal model of the consist (removing it from menus, etc)

# Menu

PAM-style menu (with optional back key) Lists trailers in order, front to back

* Bulk operations
  * Sub-menu with bulk operations (one screen, hopefully)
* Select trailer
  * Paged sub-menu with selectable trailers
    * Name of trailer
    * Trailer specific toggles
      * Parking state
      * Hinge lock state
      * Battery recharge
      * Rear hitch (unpack and unhitch, attempt hitch then pack)
      * Sub-menu with extra relevant trailer operations
        * Hydrogen stockpile

## Toolbar parameters

### Trailer operations

Argument      | Action
------------- | -------------
brakes on     | Apply handbrakes
brakes off    | Release handbrakes
deploy        | Deploy / Unpack trailer at rear
unpack        | Deploy / Unpack trailer at rear
detach        | Detach the rear-most trailer
hitch         | Couple a new trailer to the rear
attach        | Couple a new trailer to the rear
connector     | Switch connectors on rear trailer
weapons on    | Activate turrets
weapons off   | De-activate turrets
rebuild       | Check trailer consist for changes
LegacyUpdate  | Attempt to identify trailers (ISL)

### Menu navigation

Argument      | Action
------------- | -------------
up            | Move up one line on screen
down          | Move down one line on screen
apply         | Select current line on screen
select        | Select current line on screen
back          | Go back one menu screen

# Examples

There is no configuration to edit in this script. Configuration is set by means
of the Custom Data field in the trailer's hinges, the programmable block or in
any terminal block with a screen.

## Hinge example:

```ini
[trailer]
front=true
name=Trailer Name
```

At least one hinge must have front set to "true". This is the hinge at the
front, by which the trailer is towed. If there is a hinge at the rear, change
front to "false" in that hinge's Custom Data. If there is a non-hitch hinge,
don't set this value.

The name of the trailer defaults to the name of the grid, if the name line is
missing.

## Screen/Cockpit example:

```ini
[trailer]
display=0
scale=0.5
```

Change the number to the screen that you wish to use; numbers start at 0, so a
five-screen cockpit has screens 0, 1, 2, 3 and 4.

Scale is a scaling factor; adjust this if the font size is wrong.

## Programmable block example:

```ini
[trailer]
autodeploy=true
mirror=true
```

Auto-deploy can be turned off by setting autodeploy to "false". This will stop
the script attempting to trigger the unpack or deploy function after a trailer
has been disconnected. Change to false if you don't want this sort of magic.

Mirroring causes the trailers' batteries, hydrogen tanks, engines & generators,
and parking brakes to mirror those in use on the towing vehicle. 

## Timer example:

```ini
[trailer]
task=stow
```

The task setting can be "stow" or "pack", in which case it will be used to stow
a trailer for travel. It can also be set to "deploy" or "unpack", in which case
it will be used to deploy a travel for separate use in-position. If not set, or
if set to some other value, it will be treated like any other timer: its name
will be shown in the trailer's menu, where it can be triggered.

If you have a timer that is used to both deploy and stow a trailer, set task to
"toggle".
