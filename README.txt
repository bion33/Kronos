------------------------------------------------------------------------------------------------------
AUTHOR
------------------------------------------------------------------------------------------------------

Author: Krypton Nova (bion3@outlook.com)

Feel free to send me a mail if you have suggestions or concerns regarding this script.

------------------------------------------------------------------------------------------------------
DISCLAIMER
------------------------------------------------------------------------------------------------------

Use this script and anything that comes with it at your own risk. I bear no responsibility whatsoever. 

------------------------------------------------------------------------------------------------------
REQUIREMENTS:
------------------------------------------------------------------------------------------------------

- 50 MB disk space
- 100 MB RAM free during execution

- The latest version of Python 3
- The latest version of pip for Python 3
- Python packages ("pip install name" on Windows, "pip3 install name" on Linux):
	* pytz
	* xlsxwriter

------------------------------------------------------------------------------------------------------
PURPOSE & USE
------------------------------------------------------------------------------------------------------

The main function of this script is to provide the update time of every region. It will provide some
additional useful information such as the amount of nations in a region, information on the delegate
and founder, information on password status, and a link to the region in question. The script usually 
takes under one minute to finish, and only needs to be used once a day.

This script should not be used in any way or for any purpose that defies site rules. As said in the 
DISCLAIMER, I will bear no responsibility for anything you do with it.

NOTE: For up to date results, do not run during an update.

------------------------------------------------------------------------------------------------------
CONFIGURATION
------------------------------------------------------------------------------------------------------

A User-Agent must be configured. The script asks you to either fill in your nation or email address.
Use a nation or email address you check frequently so that you know it if something goes wrong.

------------------------------------------------------------------------------------------------------
SCRIPT INTERNET USAGE
------------------------------------------------------------------------------------------------------

The following is a description of the internet usage by this script. This script will NOT provide
accurate or reliable data without internet access, though it still may be of some use in a select
number of cases.

There is a delay of one second between every call made to be safely within the API-limits imposed 
by NS.

This script will download one xml of which the name starts with .Regions (20-50 MB), if it is not 
already present. This file is updated only once a day, even if you run the script more than once. 
This is to reduce bandwith, strain on the servers and time.

Additionally, it makes 4 API calls:
- one to retrieve the total nation count
- one to retrieve founderless regions
- one to retrieve passworded regions
- one to retrieve update length

The information retrieved from those calls is not stored, so these are made every time you run the 
script. Combined they are more or less 1 MB.
