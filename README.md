# Kronos

### Latest release: **V2**


## Author
Krypton Nova (bion3@outlook.com)</br>
Feel free to send me a mail if you have suggestions or concerns regarding this script.


#### **Licence:**
[GNU GPLv3](https://www.gnu.org/licenses/gpl.html)


## REQUIREMENTS
- 50 MB disk space
- 50 MB RAM during execution (150MB for timer)
- The latest version of [Python 3](https://www.python.org/downloads/), including pip
- Python packages (pip3 install ...):
	* pytz 
	* xlsxwriter


## PURPOSE & USE

Kronos was made in python, and runs in the command line (Command Prompt, Powershell, Terminal). The square brackets below mean an argument is optional. You shouldn't type them.

Syntax (Windows): 	python Kronos.py \[-d\] \[-k\] \[-o\] \[-t\]  </br>
Syntax (Linux):    python3 Kronos.py \[-d\] \[-k\] \[-o\] \[-t\]  </br>

-d, -detag  </br>
* Similar to --kronos, but will limit output to detag-able regions.

-k, -kronos  </br>
* The main function of this option is to provide the update time of every region. It will provide some additional useful information such as the amount of nations in a region, information on the delegate and founder, information on password status, and a link to the region in question. It usually takes under one minute to finish, and only needs to be used once a day.

	NOTE: The most optimal time to run -kronos or -detag is an hour before major, although the
	advantage is negligible.

-o, -ops  </br>
* This option allows you to find the (likely) military operations from the last update.

-t, -timer  </br>
* (This function is being tested for bugs and unexpected behaviour. Use at your own risk, and please report any inconsistencies you find.) Asks a target region, then runs a countdown to when it updates. Using this option implies -kronos if no sheet was found in the same folder as the script. The timer includes a variance indicator. This indicator is how much the update differs from the previous update in seconds, and is there to give you an idea on how stable it is. The timer's status gives you warnings when applicable: </br>
**"<!> Checking triggers."**: Kronos is going trough the list of triggers to see which ones have updated. The longer the time is between now and when the region updates, the longer this takes. During this status the prediction is based on the previous update.  </br>
**"<!> Using sheet times."**: The prediction is based on the previous update. This happens when update hasn't started yet, or for a short while after checking triggers.  </br>
**"<!> Large region ahead!"**: There is a large difference in nations due to a large region. Update speed may change while such a region updates, which Kronos can't detect. This may result in an unpredictable jump in the countdown. Feeders and sinkers have a large impact. Keep this in mind if you see this warning as the timer approaches zero.  </br>


## CONFIGURATION

A User-Agent must be configured to comply with NS script rules. The script asks you to either fill in your nation or email address. Use a nation or email address you check frequently so that you NS can contact you if something goes wrong.


## SCRIPT INTERNET USAGE

The following is a description of the internet usage by this script. This script will NOT provide accurate or reliable data without internet access, and is generally useless without it. 

There is a delay of at least one second (with the exception of the timer when its status is "Checking triggers", in that case it is at least 0.6 seconds) between every call made to be safely within the API-limits imposed by NS. Depending on the selected options, the following may be downloaded from the NS API by Kronos:

-kronos, -detag, -ops
* Regions tagged "invader"
* Regions tagged "defender"

-kronos, -detag
* Regions daily data dump
* "Changes" world happenings
* Total nation count
* Founderless regions
* Passworded regions

-ops
* "Members" world happenings
* "Move" nation happenings

-timer
* Lastupdate

When the script is finished it will tell you how much KiB of data it downloaded from the internet.


## CHANGELOG

#### V2 (2018-06-05)

* Added timer

#### C1 (2018-05-27)

* Merged Kronos, Detag & OpFinder into one command line tool
* Reduced RAM usage to under 50MiB
* Added total data downloaded from the internet at end of script
* Cleaned up the code


