# Space Engineers Trailer Manager

**Designed with ISL compatibility as a goal**

## Intended features:

* Detect and name each trailer to form a consist
  * Follow the train of hinges with "Tow Hitch" or "Hinge Rear" in their name
  * Determine trailer name from the hitch and/or the grid name
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
* Allow individual operations
  * Pack
  * Unpack
  * Stockpile Hydrogen
  * Activate turrets
  * Hitch another trailer
  * Unhitch connected trailer
  * List and trigger timer blocks
* Exceptional behaviour
  * Attempt to unpack recently unhitched trailers
  * Rebuild consist on demand (handy if trailers are altered whilst hitched)

## Trailer detection

### Names (legacy detection)

* Hinge bases named "Hinge Front"
* Hinge bases named "Hinge Rear" or "Tow Hitch"
* Timers name "Unpack"
* Timers named "Pack"

### Tags

* Develop a tag scheme like `[Trailer]` or similar

### Custom Data

* Put an `.ini` section in the Custom Data of any block to be included

## ISL compatibility

### Packing

The pack operation will:

* Detect a timer block intended for packing
* Run that timer block
* Subsequently:
  * Ensure that parking brakes are off
  * Ensure that hinge locks are disabled

### Unpacking

The unpack operation will:

* Detect a timer block intended for unpacking
* Run that timer block
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
* Update the internal model of the consist

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

* Menu navigation
  * Up
  * Down
  * Apply
  * Back (optional; menus should have back available)
* Commands
  * All handbrakes on
  * All handbrakes off
  * Hinge locks on
  * Hinge locks off
* Update all (rebuild consist)