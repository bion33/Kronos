# Kronos

### Latest release: v3.3.0 (2021-08-17)


#### Author
Krypton Nova (bion@disroot.org)</br>

Feel free to send me a mail if you have suggestions or concerns regarding this application. I will maintain it until 2024-07-01 before re-evaluating its usefulness. Let me know if it is useful to you :)


## PURPOSE & USE

Kronos was made in .NET, and runs in the command line. In windows you can double-click "Kronos-Console.exe" to use it, or run it using CMD or PowerShell. On Linux you can run Kronos from the terminal with "./Kronos-Console". The brackets around an option mean it is optional, you shouldn't type them.

Kronos runs the options in the same order you gave them.

Syntax:  ./Kronos [-d] [-s] [-o] [-t [region name]] [-q] </br>

-d, -detag  </br>
* Similar to -sheet, but will limit output to detag-able regions.

-s, -sheet  </br>
* Provides a sheet with the update time of every region. It will provide some additional useful information such as the amount of nations in a region, information on the delegate and founder, information on password status, and a link to the region in question. It only needs to be used once a day.

-o, -ops  </br>
* Outputs a file with the (likely) military operations from the last update.

-t, -timer  </br>
* You can pass a region name with this option. If you don't, it will ask for a target region later. It will then run a countdown to when the target region updates. The timer includes information on variance and triggers to give you an idea of how stable the update is and how much risk you can take. Keep in mind that the countdown is an estimate, and will speed up or slow down over the course of the update due to variance. It is especially difficult to make an estimate for regions which update shortly after very large regions, you will notice this is the case when the trigger counter is at the last or second-to-last trigger but the timer has more than a minute left. Except for such regions, the estimate is relatively accurate.

-q, -quit </br>
* Kronos will quit once it encounters this option. Without this option, and upon completion of other commands, Kronos will ask you again what you'd like to do. Keep in mind that the order of options matters, options after the -quit option will not be run.

## CONFIGURATION

All configuration is stored in a file called "config.txt". This file is created if it does not exist (yet). The format is `KeyWord: value`. Keywords are case-sensitive, and each keyword and value pair must be on it's own line, not taking up more than one line.

### Keywords

`UserInfo` - Contains the User-Agent, and must be configured to comply with NS script rules. The application will ask you to fill in your nation or email address at startup when this is empty or "config.txt" does not exist. Use a nation or email address you check frequently so that NS can contact you if something goes wrong.

`RaiderRegions` - Contains a comma-separated list of regions to consider "raider" (for example: `RaiderRegions: Lone Wolves United, HYDRA Command`). Regions which have an embassy with one of these regions, pending or not, will be marked as tagged in full sheets, included in detag sheets, and included under raider activity in operations reports. 

`IndependentRegions` - Similar to the above, but for regions to consider "independent". Used in operation reports, but has currently no effect on detag and full sheets.

`DefenderRegions` - Similar to the above, but for regions to consider "defender". Used in operation reports, but has currently no effect on detag and full sheets.

`PriorityRegions` - Similar to the above, but for regions to keep a close eye on. Used in operation reports, but has currently no effect on detag and full sheets.


## Common Issues

#### Missing libgdiplus

> System.DllNotFoundException: Unable to load DLL 'libgdiplus'

This error is encountered on Linux and MacOS when "libgdiplus" is not installed. It usually can be installed trough your distribution's package manager, or on MacOS through [brew](https://brew.sh) by running `brew install mono-libgdiplus`. It is also included in Mono, and can otherwise be found [here](https://github.com/mono/libgdiplus).

#### Paused timer in CMD or PowerShell 

On Windows if you click anywhere in the shell window, the timer will pause. It will continue when you right-click. 

This behaviour is native to CMD & PowerShell, but can be disabled by right-clicking the title bar, navigating to "properties", and in the tab "options" disabling "QuickEdit Mode".

#### Other Issues

If you have any other issues, feel free to open a new issue [here](https://github.com/Krypton-Nova/Kronos/issues) or contact me via mail (which can be found at the top of this document).


## CHANGELOG

### v3.3.1 (2022-08-18)

* Corrected a bug where the `README.md` file is not retrievable on UNIX and MacOS.
* Corrected a HTTP timeout bug where the latest version number was retrievable.

### v3.3.0 (2021-08-17)

* Improved delegacy change detection for operations reports
* Include embassies in categorisation for detag and full sheets, so that users can now for example add raider regions to their config and get more accurate detag sheets and full sheets

### v3.2.4 (2021-01-23)

* Timer: Fix region name detection, to allow the improved detection to be used everytime

#### v3.2.3 (2021-01-21)

* Operations Report: Reduce WAD move time to region from 24h to 12h to filter out false positives from the previous update.
* Operations Report: Filter "independent" operations and show them separately
* Operations Report: Allow user to manually configure regions to be considered as raider/independent/defender or be prioritized (shown apart from the rest at the top of the report). See config.txt, it accepts a comma-separated list of region names for each tag.
* Operations Report: Sort reports by update timestamp
* Timer: Allow region to be specified as link or as id.
* Added Windows build script


#### v3.2.2 (2021-01-08)

* Critical: Replace dead repository link in user-agent

#### v3.2.1 (2020-09-16)

* Timer now tells you when it's waiting for region parsing
* Typo

#### v3.2.0 (2020-09-16)

* Make Kronos store generated sheets & reports in dated folders to prevent file clutter (user request)
* Add a region's update time to operation reports (user request). This time is the same as when the delegate came to power, so I noted it with an "at" (@) sign
* Tell the user when a new release of Kronos is available. Kronos relies on the README.md file to know its current version, so don't delete it

#### v3.1.0 (2020-07-20)

* Split solution into library and console application so that library can be used in other projects.
* Renamed the "kronos" option to "sheet", so I can stop confusing everyone for legacy reasons.

#### v3.0.0 (2020-07-19)

* Ported code from Python to .NET Core.
* Made timer more accurate and less API intensive by spacing triggers apart exponentially instead of linearly.
* Gave founder, delegate, password and tagged status their own columns in sheets to improve sortability.
* Kronos will now only quit once it encounters the -quit command, otherwise it will ask again what you'd like to do.
* Improved informative messages.
* Various improvements & changes which came naturally trough porting the code, which have less direct effect on the user experience.

#### v2.2.0 (2018-06-08)

* Made Kronos more user-friendly and portable for Windows users by building it as an executable.
* Changed the default operation of Kronos from creating a Kronos sheet to asking the user what to do.

#### v2.1.0 (2018-06-07)

* Resolved timer saying region updated right after checking triggers due to bad default variables.

#### v2.0.0 (2018-06-05)

* Added timer

#### v1.9.0 (2018-05-27)

* Merged Kronos, Detag & OpFinder into one command line tool
* Reduced RAM usage to under 50MiB
* Added total data downloaded from the internet at end of script
* Cleaned up the code

#### v1.x.x

* Kronos sheet generator

