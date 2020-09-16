# Kronos

### Latest release: v3.2.0 (2020-09-16)


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

A User-Agent must be configured to comply with NS script rules. The application asks you to either fill in your nation or email address. Use a nation or email address you check frequently so that you NS can contact you if something goes wrong. This information will be stored in a file called "config.txt".


## Common Issues

#### Missing libgdiplus

> System.DllNotFoundException: Unable to load DLL 'libgdiplus'

This error is encountered on Linux when "libgdiplus" is not installed. It usually can be installed trough your distribution's package manager. It is also included in Mono, and can otherwise be found [here](https://github.com/mono/libgdiplus).

#### Paused timer in CMD or PowerShell 

On Windows if you click anywhere in the shell window, the timer will pause. It will continue when you right-click. 

This behaviour is native to CMD & PowerShell, but can be disabled by right-clicking the title bar, navigating to "properties", and in the tab "options" disabling "QuickEdit Mode".

#### Other Issues

If you have any other issues, feel free to open a new issue [here](https://github.com/Krypton-Nova/Kronos/issues) or contact me via mail (which can be found at the top of this document).


## CHANGELOG

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

