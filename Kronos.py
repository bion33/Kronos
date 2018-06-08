#!/usr/bin/env python3

# Note for running automatically at specific times:
# For accurate results to be available as soon as possible before the next update, run right after any update.


# -------------------------------------------------- Imports --------------------------------------------------------- #

from calendar import timegm
from datetime import timedelta, datetime
from functools import lru_cache as cache
from gzip import open as gzopen
from math import ceil
from os import name as sysname
from os import path, remove, listdir, getcwd, system
from re import search, findall
from sys import argv, getsizeof
from threading import Thread
from time import sleep, strptime, time
from traceback import format_exc
from urllib.error import HTTPError
from urllib.request import urlopen, Request
from xml.etree.ElementTree import iterparse

try:
    from pytz import timezone, utc
except ImportError:
    timezone = None
    utc = None
    print('The pytz module is required for Kronos to work across time zones. Run "pip3 install pytz" in the command'
          'line to install it. You might need elevated permissions to do so.')
    quit()

try:
    from xlsxwriter import Workbook
except ImportError:
    Workbook = None
    print('The xlsxwriter module is required for Kronos to save .xlsx sheets. Run "pip3 install xlsxwriter" in the '
          'command line to install it. You might need elevated permissions to do so.')
    quit()

# ------------------------------------------------------ Setup ------------------------------------------------------- #


print("Kronos, at your service.")
print("Starting...")


# At its start, the program has not downloaded anything yet. This value (in bytes) will increase as stuff is downloaded.
downloaded = 0

# Set basic user-agent
user_agent = "WIP Kronos (https://github.com/Krypton-Nova/Kronos). User info: "
input_string = "\nYou need to provide your nation name or email address once. This is needed to\ncomply with NS " \
               "script rules. It will be saved in config.txt in this folder so\nthat you do not have to provide it " \
               "again.\n"

# Get User-Agent configured by user
if path.isfile("config.txt"):
    with open("config.txt", "r") as f:
        user = f.readlines()
    try:
        user_info = user[1]
    # If it has just one line, it's untouched by the user. Ask and add user info
    except IndexError:
        user_info = input(input_string)
        print("\n")
        with open("config.txt", "a") as f:
            f.write(user_info)
# If the file doesn't exist, create it
else:
    # Descriptive line for config
    config = "# Fill in your nation or email address on the line below. This is needed to comply with NS script rules" \
             "." + "\n"
    # Ask user info
    user_info = input(input_string)
    config += user_info
    with open("config.txt", "w") as f:
        f.write(config)
    # Clear screen
    if sysname == "nt":
        system("cls")
    else:
        system("clear")

# Set User-Agent
user_agent = {"User-Agent": user_agent + user_info}


# Everything date and time related
utc_timezone = timezone("UTC")
ns_timezone = timezone("America/New_York")
now = datetime.now(ns_timezone)
time_now = now.strftime("%H:%M:%S")
hour_now = int(now.strftime("%H"))
date_today = now.strftime("%Y-%m-%d")
posix_today = datetime.now(utc_timezone).strftime("%Y-%m-%d")
posix_today = timegm(strptime(posix_today, "%Y-%m-%d"))
dumb_saving_time = utc.localize(datetime.utcnow())
dumb_saving_time = (dumb_saving_time.astimezone(ns_timezone).dst() != timedelta(0))


# Command line options
cli_options = argv[1:]
arg_detag = False
arg_kronos = False
arg_ops = False
arg_timer = False
possible_options = ["-d", "-detag", "-k", "-kronos", "-o", "-ops", "-t", "-timer"]
# Command line help
help_string = "Kronos Quick Help\n" \
          "\n" \
          "    Syntax: Kronos [-d] [-k] [-o] [-t]\n" \
          "\n" \
          "Options:\n" \
          "  -d, -detag:   an update sheet limited to detag-able regions.\n" \
          "  -k, -kronos:  the full update times sheet.\n" \
          "  -o, -ops:     likely military operations from the last update.\n" \
          "  -t, -timer:   time to when a region updates. Implies [-k].\n" \
          "\n" \
          'See "Purpose & Use" in the README for more information.' \
          "\n"

correct_args = False
while not cli_options or correct_args is False:
    # If no options found, or they were reset previously because of a bad option
    if not cli_options:
        print(help_string)
        cli_options = input("What do you want me to do?\nKronos ")
        cli_options = cli_options.split(" ")
    # Check if the options provided are valid
    for option in cli_options:
        option = option.replace("[", "").replace("]", "")
        # If a bad option was found, user has to re-enter options
        if option not in possible_options:
            correct_args = False
            cli_options = []
            # Clear screen
            if sysname == "nt":
                system("cls")
            else:
                system("clear")
        # Otherwise set variables according with provided options
        elif option == "-d" or option == "-detag":
            arg_detag = True
            correct_args = True
        elif option == "-k" or option == "-kronos":
            arg_kronos = True
            correct_args = True
        elif option == "-o" or option == "-ops":
            arg_ops = True
            correct_args = True
        elif option == "-t" or option == "-timer":
            arg_timer = True
            correct_args = True
            filename = "Kronos_" + date_today + ".xlsx"
            # Timer is dependent on an up to date Kronos sheet
            if not path.isfile(filename):
                arg_kronos = True
    

# ------------------------------------------------- Common Functions ------------------------------------------------- #


# Get from the web
def wget(u):
    global downloaded
    req = Request(u, None, headers=user_agent)
    with urlopen(req) as a:
        g = a.read()
        downloaded += getsizeof(a)
    # API requests should be less than 50/30s. Wait 1s to be safe.
    sleep(0.6)
    return g


# Calculate hours, minutes and seconds from seconds
# This is cached for timer as it may call this more than once a second, which would be a waste of time
@cache(maxsize=16)
def hms(s):
    m = int(s / 60)
    s = s - (m * 60)
    h = int(m / 60)
    m = m - (h * 60)
    # "{:02}" a width of "2", and if it's shorter, add leading "0".
    # "{0:05.2f}" round to two decimals after the point. The total width is "5" including point. Add "0" if shorter.
    ti = "{:02}:{:02}:{:05.2f}".format(h, m, s)
    return ti


# ------------------------------------------------- Downloads -------------------------------------------------------- #


print("Downloading NationStates Data...")


# Variables
founderless_list = []
tagged_list = []
defenders_list = []
protected_list = []
wa_happenings = []
nations_total = 0
end_time = 0

# For ops only
if arg_ops is True:

    # Get a list of WA happenings
    def happenings_member(start, end):
        # World happenings filtered by WA membership, which includes nations becoming WA Delegate
        u = "https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=member;sincetime=" + str(start) + \
            ";beforetime=" + str(end) + ";limit=200"
        return str(wget(u).decode("utf-8")).replace("\n", "")

    # Set report name and intervals during which the update can happen
    if 0 <= hour_now <= 2:
        start_time = posix_today + 18000 - 86400
        end_time = posix_today + 25200 - 86400
    elif 2 < hour_now <= 14:
        start_time = posix_today + 18000
        end_time = posix_today + 25200
    else:
        start_time = posix_today + 61200
        end_time = posix_today + 68400

    # If DST is in effect for America/New_York, subtract one hour
    if dumb_saving_time is True:
        start_time -= 3600
        end_time -= 3600

    # Iterate trough world WA events during last update.
    more_wa_happenings = True
    i = 0
    # The last request will be empty, so timestamps will be empty and return false.
    # We must use this loop because we can only get 200 events at once, and sometimes there are more "member" happenings
    # during an update.
    while more_wa_happenings:
        # Get happenings
        more_wa_happenings = happenings_member(start_time, end_time - (i * 2400))
        # Check if there still are any happenings
        more_wa_happenings = findall("<EVENT(.*?)</EVENT>", str(more_wa_happenings))
        # Store events in list, unless they're already in there from the previous iteration
        # Yes, it's "<EVENT", there is a unique ID behind that.
        for wa_happening in more_wa_happenings:
            if wa_happening not in wa_happenings:
                wa_happenings.append(wa_happening)
        # Increase counter
        i += 1

# For kronos and detag:
if arg_kronos is True or arg_detag is True:
    # Download founderless REGIONS and save as list
    url = "https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=founderless"
    founderless_list = str(wget(url).decode("utf-8")).replace("\n", "")
    founderless_list = founderless_list.split(",")

    # Download REGIONS protected by a password and save as list
    url = "https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=password"
    protected_list = str(wget(url).decode("utf-8")).replace("\n", "")
    protected_list = protected_list.split(",")

    # Download numnations and save as integer
    url = "https://www.nationstates.net/cgi-bin/api.cgi?q=numnations"
    nations_total = str(wget(url).decode("utf-8")).replace("\n", "")
    nations_total = int(search("<NUMNATIONS>(.*)</NUMNATIONS>", nations_total).group(1))

    # If .Regions_date_today.xml doesn't exist already, remove previous versions and download current version
    filename = ".Regions_" + date_today + ".xml"
    if path.isfile(filename) is False:

        # Remove previous versions
        pattern = ".Regions"
        for f in listdir(getcwd()):
            if search(pattern, f):
                remove(path.join(getcwd(), f))

        # Download regions.xml
        url = "https://www.nationstates.net/pages/regions.xml.gz"
        data = wget(url)

        # Save downloaded archive as regions.xml.gz
        with open("regions.xml.gz", "wb") as local_file:
            local_file.write(data)
        # Don't wate RAM, set large variables to None when not used further in the program
        data = None

        # Extract archive to .Regions_date_today.xml
        gz = gzopen("regions.xml.gz", "rb")
        extracted = gz.read()
        filename = ".Regions_" + date_today + ".xml"
        with open(filename, "wb") as local_file:
            local_file.write(extracted)
        # Don't wate RAM, set large variables to None when not used further in the program
        extracted = None
        local_file = None

        # Close gzip, otherwise it remains "in use"
        gz.close()
        # Remove original archive
        remove("regions.xml.gz")


# For detag, kronos and ops
if arg_detag is True or arg_kronos is True or arg_ops is True:
    # Download regions marked with the invader tag and save as list
    url = "https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=invader"
    tagged_list = str(wget(url).decode("utf-8")).replace("\n", "")
    # Filter out information and save as list
    tagged_list = findall("<REGIONS>(.*?)</REGIONS>", str(tagged_list))
    tagged_list = str(tagged_list).replace("['", "").replace("']", "")
    tagged_list = tagged_list.split(",")

    # Download regions marked with the defender tag and save as list
    url = "https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=defender"
    defenders_list = str(wget(url).decode("utf-8")).replace("\n", "")
    # Filter out information and save as list
    defenders_list = findall("<REGIONS>(.*?)</REGIONS>", str(defenders_list))
    defenders_list = str(defenders_list).replace("['", "").replace("']", "")
    defenders_list = defenders_list.split(",")


# ---------------------------------------------- Calibrate Update ---------------------------------------------------- #

# Variables
changes_list = []
update_length_major = 0
update_length_minor = 0

# For kronos and detag
if arg_detag is True or arg_kronos is True:

    print("Calibrating Update...")

    # Get end of update
    def end_of_update(start, end):
        # World happenings filtered by nation changes, which includes influence changes
        u = "https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=change;sincetime=" + str(start) + \
            ";beforetime=" + str(end) + ";limit=200"
        c = wget(u).decode("utf-8").split(".")
        # Find the first occurrence of "influence" and return its timestamp
        for x in c:
            if "influence" in x:
                return [c, int(search("<TIMESTAMP>(.*)</TIMESTAMP>", x).group(1))]
        # If no occurrence of influence, return the list of changes and None
        return [c, None]

    # Time intervals to check for MAJOR (see posix time)
    possible_times = [[posix_today + 18000, posix_today + 25200],
                      [posix_today + 18000, posix_today + 24300],
                      [posix_today + 18000, posix_today + 23400],
                      [posix_today + 18000, posix_today + 22500],
                      [posix_today + 18000, posix_today + 21600],
                      [posix_today + 18000, posix_today + 20700],
                      [posix_today + 18000, posix_today + 19800],
                      [posix_today + 18000, posix_today + 18900]]

    # Initial variables
    update_expected_start = None
    update_expected_end = None
    update_end = None
    t = 0

    # Loop trough possible time intervals from late to early
    while update_end is None and t <= (len(possible_times) - 1):

        # When requesting a sheet during MAJOR we can only get accurate results for MAJOR from yesterday
        if 0 <= hour_now <= 2:
            update_expected_start = possible_times[t][0] - 86400
            update_expected_end = possible_times[t][1] - 86400
        else:
            update_expected_start = possible_times[t][0]
            update_expected_end = possible_times[t][1]

        # If DST is in effect for America/New_York, subtract one hour
        if dumb_saving_time is True:
            update_expected_start -= 3600
            update_expected_end -= 3600

        # Get end of update #
        eou = end_of_update(update_expected_start, update_expected_end)
        changes_list = eou[0]
        update_end = eou[1]

        t += 1

    # Calculate MAJOR update length
    try:
        update_length_major = update_end - update_expected_start
    # Unless update_end wasn't found
    except TypeError as error:
        error_message = "update_length_major = update_end - update_expected_start: " + str(error) + "\n\n"
        error_message += "Variable contents:\n" \
                         "hour_now = " + str(hour_now) + "\n" \
                         "date_today = " + date_today + "\n" \
                         "posix_today = " + str(posix_today) + "\n" \
                         "dumb_saving_time = " + str(dumb_saving_time) + "\n" \
                         "update_expected_start = " + str(update_expected_start) + "\n" \
                         "update_expected_end = " + str(update_expected_end) + "\n" \
                         "update_end = " + str(update_end) + "\n" \
                         "t = " + str(t) + "\n" \
                         "changes_list = " + str(changes_list) + "\n\n" \
                         "Caught error, please send error-report.txt to KN#4693 on Discord or bion3@outlook.com"
        with open(date_today + "_error-report.txt", "w") as file:
            file.write(error_message)
        print(error_message)
        input("\n\nPress enter to continue.")
        quit()

    # Time intervals to check for MINOR (see posix time)
    possible_times = [[posix_today + 61200, posix_today + 68400],
                      [posix_today + 61200, posix_today + 67500],
                      [posix_today + 61200, posix_today + 66600],
                      [posix_today + 61200, posix_today + 65700],
                      [posix_today + 61200, posix_today + 64800],
                      [posix_today + 61200, posix_today + 63900],
                      [posix_today + 61200, posix_today + 63000],
                      [posix_today + 61200, posix_today + 62100]]

    # Initial variables
    update_expected_start = None
    update_expected_end = None
    update_end = None
    t = 0

    # Loop trough possible time intervals from late to early
    while update_end is None and t <= (len(possible_times) - 1):

        # When requesting a sheet during or before MINOR we can only get results for MINOR from yesterday
        if 0 <= hour_now < 14:
            update_expected_start = possible_times[t][0] - 86400
            update_expected_end = possible_times[t][1] - 86400
        else:
            update_expected_start = possible_times[t][0]
            update_expected_end = possible_times[t][1]

        # If DST is in effect for America/New_York, subtract one hour
        if dumb_saving_time is True:
            update_expected_start -= 3600
            update_expected_end -= 3600

        # Get end of update
        eou = end_of_update(update_expected_start, update_expected_end)
        changes_list = eou[0]
        update_end = eou[1]

        t += 1

    # Calculate MINOR update length
    try:
        update_length_minor = update_end - update_expected_start
    # Unless update_end wasn't found
    except TypeError as error:
        error_message = "update_length_minor = update_end - update_expected_start: " + str(error) + "\n\n"
        error_message += "Variable contents:\n" \
                         "hour_now = " + str(hour_now) + "\n" \
                         "date_today = " + date_today + "\n" \
                         "posix_today = " + str(posix_today) + "\n" \
                         "dumb_saving_time = " + str(dumb_saving_time) + "\n" \
                         "update_expected_start = " + str(update_expected_start) + "\n" \
                         "update_expected_end = " + str(update_expected_end) + "\n" \
                         "update_end = " + str(update_end) + "\n" \
                         "t = " + str(t) + "\n" \
                         "changes_list = " + str(changes_list) + "\n\n" \
                         "Caught error, please send error-report.txt to KN#4693 on Discord or bion3@outlook.com"
        with open("error-report.txt", "w") as file:
            file.write(error_message)
        print(error_message)
        input("\n\nPress enter to continue.")
        quit()

    # To calculate update time per nation for MAJOR and MINOR
    # major_time = update_length_major / nations_total
    # minor_time = update_length_minor / nations_total
    # Using the fraction itself (length / total) is a tiny bit more accurate than using the pre-calculated
    # variables.


# ----------------------------------------------- Process Regions ---------------------------------------------------- #


# Variables
regions_names = []
regions_links = []
regions_nation_counts = []
regions_delegate_votes = []
regions_delegate_authority = []

# For kronos and detag
if arg_detag is True or arg_kronos is True:

    print("Processing Regions...")

    # Getting information from regions.xml
    filename = ".Regions_" + date_today + ".xml"

    for event, elem in iterparse(filename):
        if elem.tag == "REGION":
            name = elem.find("NAME").text
            regions_names += [name]
            regions_links += [str("https://www.nationstates.net/region=" + name).replace(" ", "_")]
            regions_nation_counts += [int(elem.find("NUMNATIONS").text)]
            regions_delegate_votes += [int(elem.find("DELEGATEVOTES").text)]
            authority = elem.find("DELEGATEAUTH").text
            if authority[0] == "X":
                regions_delegate_authority += [True]
            else:
                regions_delegate_authority += [False]
            elem.clear()


# ------------------------------------------- Calculate Update Times ------------------------------------------------- #


# Variables
major_list = []
minor_list = []
regions_nations_cumulative = []

# For kronos and detag
if arg_detag is True or arg_kronos is True:

    print("Calculating Update Times...")

    # Grabbing the cumulative number of nations that've updated by the time a region has.
    for found in regions_nation_counts:
        if len(regions_nations_cumulative) == 0:
            regions_nations_cumulative.extend([int(found)])
        else:
            regions_nations_cumulative.extend([int(found) + regions_nations_cumulative[-1]])

    # Getting the approximate major/minor update times for regions
    for found in regions_nations_cumulative:

        # Calculate MAJOR update time
        seconds = found * (update_length_major / nations_total)
        # Translate seconds to h:m:s
        string = hms(seconds)
        # Store in list
        major_list.append(string)

        # Calculate MINOR update time
        seconds = found * (update_length_minor / nations_total)
        # Translate seconds to h:m:s
        string = hms(seconds)
        # Store in list
        minor_list.append(string)


# --------------------------------------------------- Detag Sheet ---------------------------------------------------- #


if arg_detag is True:
    print("Preparing Detag Sheet...")

    # Creating virtual sheet. "constant_memory" ensures low  memory usage.
    filename = "Kronos-Detag_" + date_today + ".xlsx"
    wbd = Workbook(filename, {"constant_memory": True})
    wsd = wbd.add_worksheet()

    # Set formatting (doesn't need most of the formatting Kronos uses, so doesn't have those)
    right = wbd.add_format({"align": "right"})
    shrink = wbd.add_format({"shrink": True})
    header = wbd.add_format({"bold": True, "bg_color": "gray"})
    right_header = wbd.add_format({"bold": True, "align": "right", "bg_color": "gray"})

    # Set headers
    # wsd.write(row, column, cell content, format)
    # Note that "A1" is row 0 and column 0, "B2" is row 1 and column 1, etc.
    wsd.write(0, 0, "Region", header)
    wsd.write(0, 1, "Major", header)
    wsd.write(0, 2, "Minor", header)
    wsd.write(0, 3, "Nations", header)
    wsd.write(0, 4, "Cumulative", header)
    wsd.write(0, 5, "Endo's", header)
    wsd.write(0, 6, "Link", header)
    wsd.write(0, 8, "World", right_header)
    wsd.write(0, 9, "Data", header)

    # Building the sheet (this is where Detag differs from Kronos)
    counter = 1
    counter_regions = 0

    for found in regions_names:
        region = found

        # Add tags and write region name
        _tags = ''

        # If the region is tagged and does not have a password, it's detag-able
        if found in tagged_list and found not in protected_list and regions_delegate_authority[counter_regions] is True:
            # No Founder
            if found in founderless_list:
                tags = "[!!!~] "
            # Founder
            else:
                tags = "[!!~+] "

            # Write region properties
            wsd.write(counter, 0, tags + region, shrink)
            wsd.write(counter, 1, major_list[counter_regions])
            wsd.write(counter, 2, minor_list[counter_regions])
            wsd.write(counter, 3, regions_nation_counts[counter_regions])
            wsd.write(counter, 4, regions_nations_cumulative[counter_regions])
            wsd.write(counter, 5, regions_delegate_votes[counter_regions])
            wsd.write(counter, 6, regions_links[counter_regions], shrink)

            # Next row
            counter += 1

        # Next region
        counter_regions += 1

        # Extra info
        # Placed here with if statements because of a xlsxwriter memory optimization which works row per row. Due to
        # this you can't edit a cell if you already passed the row, so these "extra" cells are processed in their
        # respective rows. This doesn't have the colour legend Kronos has because it's unneeded.
        # See {"constant_memory": True})
        if counter < 14:
            if counter == 1:
                wsd.write(counter, 8, "Nations: ", right)
                wsd.write(counter, 9, str(nations_total))
            if counter == 2:
                wsd.write(counter, 8, "Last Major: ", right)
                wsd.write(counter, 9, str(update_length_major) + " seconds")
            if counter == 3:
                wsd.write(counter, 8, "Secs/Nation: ", right)
                wsd.write(counter, 9, str(update_length_major / nations_total))
            if counter == 4:
                wsd.write(counter, 8, "Nations/Sec: ", right)
                wsd.write(counter, 9, str(1 / (update_length_major / nations_total)))
            if counter == 5:
                wsd.write(counter, 8, "Last Minor: ", right)
                wsd.write(counter, 9, str(update_length_minor) + " seconds")
            if counter == 6:
                wsd.write(counter, 8, "Secs/Nation: ", right)
                wsd.write(counter, 9, str((update_length_minor / nations_total)))
            if counter == 7:
                wsd.write(counter, 8, "Nations/Sec: ", right)
                wsd.write(counter, 9, str(1 / (update_length_minor / nations_total)))
            if counter == 9:
                wsd.write(counter, 8, "Legend", right_header)
                wsd.write(counter, 9, ":", header)
            if counter == 10:
                wsd.write(counter, 8, "[+] :", right)
                wsd.write(counter, 9, "Founder")
            if counter == 11:
                wsd.write(counter, 8, "[~] :", right)
                wsd.write(counter, 9, "Executive Delegacy")
            if counter == 12:
                wsd.write(counter, 8, "[!] :", right)
                wsd.write(counter, 9, "Founderless")
            if counter == 13:
                wsd.write(counter, 8, "[!!] :", right)
                wsd.write(counter, 9, 'Tagged "Invader"')

    # Make columns fit their content
    wsd.set_column(0, 0, 50)
    wsd.set_column(1, 1, 12)
    wsd.set_column(2, 2, 12)
    wsd.set_column(3, 3, 10)
    wsd.set_column(4, 4, 10)
    wsd.set_column(5, 5, 10)
    wsd.set_column(6, 6, 60)
    wsd.set_column(8, 8, 11)
    wsd.set_column(9, 9, 24)

    # Save Sheet
    print("Saving Detag Sheet...")
    wbd.close()
    # Don't wate RAM, set large variables to None when not used further in the program
    wbd = None
    wsd = None


# --------------------------------------------------- Kronos Sheet --------------------------------------------------- #


if arg_kronos is True:
    print("Preparing Kronos Sheet...")

    # Creating virtual sheet. "constant_memory" ensures low  memory usage.
    filename = "Kronos_" + date_today + ".xlsx"
    wbk = Workbook(filename, {"constant_memory": True})
    wsk = wbk.add_worksheet()

    # Set formatting
    right = wbk.add_format({"align": "right"})
    green = wbk.add_format({"bg_color": "green"})
    yellow = wbk.add_format({"bg_color": "yellow"})
    orange = wbk.add_format({"bg_color": "orange"})
    red = wbk.add_format({"bg_color": "red"})
    shrink = wbk.add_format({"shrink": True})
    header = wbk.add_format({"bold": True, "bg_color": "gray"})
    right_header = wbk.add_format({"bold": True, "align": "right", "bg_color": "gray"})
    info_green = wbk.add_format({"align": "right", "bg_color": "green"})
    info_yellow = wbk.add_format({"align": "right", "bg_color": "yellow"})
    info_orange = wbk.add_format({"align": "right", "bg_color": "orange"})
    info_red = wbk.add_format({"align": "right", "bg_color": "red"})
    region_green = wbk.add_format({"shrink": True, "bg_color": "green"})
    region_yellow = wbk.add_format({"shrink": True, "bg_color": "yellow"})
    region_orange = wbk.add_format({"shrink": True, "bg_color": "orange"})
    region_red = wbk.add_format({"shrink": True, "bg_color": "red"})

    # Set headers
    # wsk.write(row, column, cell content, format)
    # Note that "A1" is row 0 and column 0, "B2" is row 1 and column 1, etc.
    wsk.write(0, 0, "Region", header)
    wsk.write(0, 1, "Major", header)
    wsk.write(0, 2, "Minor", header)
    wsk.write(0, 3, "Nations", header)
    wsk.write(0, 4, "Cumulative", header)
    wsk.write(0, 5, "Endo's", header)
    wsk.write(0, 6, "Link", header)
    wsk.write(0, 8, "World", right_header)
    wsk.write(0, 9, "Data", header)

    # Building the sheet
    counter = 1

    for found in regions_names:
        region = found

        # Add tags and write region name
        _tags = ''
        # Has Founder
        if found not in founderless_list:
            _tags += "+"
            tags = "[" + _tags + "] "
            wsk.write(counter, 0, tags + region, region_green)
        # Has password
        if found in protected_list:
            _tags = "#" + _tags
            tags = "[" + _tags + "] "
            wsk.write(counter, 0, tags + region, region_green)
        # Has executive Delegacy and password
        if regions_delegate_authority[counter - 1] is True and found in protected_list:
            _tags = "~" + _tags
            tags = "[" + _tags + "] "
            wsk.write(counter, 0, tags + region, region_green)
        # Has executive Delegacy, but no password
        if regions_delegate_authority[counter - 1] is True and found not in protected_list:
            _tags = "~" + _tags
            tags = "[" + _tags + "] "
            wsk.write(counter, 0, tags + region, region_yellow)
        # Has no Founder, but has a password
        if found in founderless_list and found in protected_list:
            _tags = "!" + _tags
            tags = "[" + _tags + "] "
            wsk.write(counter, 0, tags + region, region_green)
        # Has no Founder and no password
        if found in founderless_list and found not in protected_list:
            _tags = "!" + _tags
            tags = "[" + _tags + "] "
            wsk.write(counter, 0, tags + region, region_orange)
        # Has been tagged "Invader"
        if found in tagged_list:
            _tags = "!!" + _tags
            tags = "[" + _tags + "] "
            wsk.write(counter, 0, tags + region, region_red)
        else:
            tags = "[]"

        # Write region properties
        wsk.write(counter, 1, major_list[counter - 1])
        wsk.write(counter, 2, minor_list[counter - 1])
        wsk.write(counter, 3, regions_nation_counts[counter - 1])
        wsk.write(counter, 4, regions_nations_cumulative[counter - 1])
        wsk.write(counter, 5, regions_delegate_votes[counter - 1])
        wsk.write(counter, 6, regions_links[counter - 1], shrink)

        # Extra info
        # Placed here with if statements because of a xlsxwriter memory optimization which works row per row. Due to
        # this you can't edit a cell if you already passed the row, so these "extra" cells are processed in their
        # respective rows. See {"constant_memory": True})
        if counter < 14:
            if counter == 1:
                wsk.write(counter, 8, "Nations: ", right)
                wsk.write(counter, 9, str(nations_total))
            if counter == 2:
                wsk.write(counter, 8, "Last Major: ", right)
                wsk.write(counter, 9, str(update_length_major) + " seconds")
            if counter == 3:
                wsk.write(counter, 8, "Secs/Nation: ", right)
                wsk.write(counter, 9, str((update_length_major / nations_total)))
            if counter == 4:
                wsk.write(counter, 8, "Nations/Sec: ", right)
                wsk.write(counter, 9, str(1 / (update_length_major / nations_total)))
            if counter == 5:
                wsk.write(counter, 8, "Last Minor: ", right)
                wsk.write(counter, 9, str(update_length_minor) + " seconds")
            if counter == 6:
                wsk.write(counter, 8, "Secs/Nation: ", right)
                wsk.write(counter, 9, str((update_length_minor / nations_total)))
            if counter == 7:
                wsk.write(counter, 8, "Nations/Sec: ", right)
                wsk.write(counter, 9, str(1 / (update_length_minor / nations_total)))
            if counter == 9:
                wsk.write(counter, 8, "Legend", right_header)
                wsk.write(counter, 9, ":", header)
            if counter == 10:
                wsk.write(counter, 8, "Green:", info_green)
                wsk.write(counter, 9, "Founder [+] or Password [#]", green)
            if counter == 11:
                wsk.write(counter, 8, "Yellow:", info_yellow)
                wsk.write(counter, 9, "Executive Delegacy [~]", yellow)
            if counter == 12:
                wsk.write(counter, 8, "Orange:", info_orange)
                wsk.write(counter, 9, "Founderless [!]", orange)
            if counter == 13:
                wsk.write(counter, 8, "Red:", info_red)
                wsk.write(counter, 9, 'Tagged "Invader" [!!]', red)

        counter += 1

    # Make columns fit their content
    wsk.set_column(0, 0, 50)
    wsk.set_column(1, 1, 12)
    wsk.set_column(2, 2, 12)
    wsk.set_column(3, 3, 10)
    wsk.set_column(4, 4, 10)
    wsk.set_column(5, 5, 10)
    wsk.set_column(6, 6, 60)
    wsk.set_column(8, 8, 11)
    wsk.set_column(9, 9, 24)

    # Save Sheet
    print("Saving Kronos Sheet...")
    wbk.close()
    # Don't wate RAM, set large variables to None when not used further in the program
    wbk = None
    wsk = None
    founderless_list = None
    protected_list = None
    major_list = None
    minor_list = None
    regions_nation_counts = None
    regions_nations_cumulative = None
    regions_delegate_votes = None
    regions_links = None


# ------------------------------------------------- Operation Finder ------------------------------------------------- #


# For ops only
if arg_ops is True:

    print("Processing world happenings...")

    # Go trough events to filter out new WA Delegates and the regions they became delegate for
    new_delegates = []
    timestamps = []
    new_regions = []
    for wa_happening in wa_happenings:
        if "WA Delegate" in wa_happening:
            new_delegates.append(search("@@(.*?)@@", wa_happening).group(1))
            timestamps.append(search("<TIMESTAMP>(.*?)</TIMESTAMP>", wa_happening).group(1))
            new_regions.append(search("%%(.*?)%%", wa_happening).group(1))

    # Iterate trough the new delegates
    invaded_regions = []
    defended_regions = []
    suspicious_regions = []
    i = 0
    for delegate in new_delegates:
        # Download delegate's activity, filtered by moves, from the last 24 hours before the end of last update
        try:
            url = "https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;view=nation." + delegate + \
                  ";filter=move;sincetime=" + str(end_time - 86400)
            html = str(wget(url).decode("utf-8")).replace("\n", "")
            # Extract
            moved = findall("<EVENT(.*?)</EVENT>", str(html))
        except HTTPError:
            moved = False

        # If the delegate moved in the last 24 hours before the end of last update, specifically before they became WAD
        if moved:
            moved_before_WAD = False
            for event in moved:
                timestamp = search("<TIMESTAMP>(.*?)</TIMESTAMP>", event).group(1)
                if int(timestamp) < int(timestamps[i]):
                    moved = search("<TEXT>(.*?)</TEXT>", event).group(1)
                    moved_before_WAD = True
                    break

            if moved_before_WAD is True:
                region = new_regions[i]
                # Check if it has the invader tag (using this expression because tagged_list has uppercase and spaces,
                # whereas region is lowercase and uses underscores).
                if any(x.lower().replace(" ", "_") == region for x in tagged_list):
                    # Log
                    print("* Possible raider activity in " + region + ".")
                    # Generate link
                    link = "https://www.nationstates.net/region=" + region
                    # Append report
                    invaded_regions.append(str("> Possible raider activity in " + region + ".\n" + link))
                # If not, move on to check if it was a defender operation
                else:
                    moved_from = search("%%(.*?)%%", moved).group(1)
                    # If the region they moved from is defender, it was a defender operation (using this expression
                    # because tagged_list has uppercase and spaces, whereas moved_from is lowercase and uses
                    if any(x.lower().replace(" ", "_") == moved_from for x in defenders_list):
                        # Log
                        print("* Likely defence operation in " + region + ".")
                        # Generate link
                        link = "https://www.nationstates.net/region=" + region
                        # Append report
                        defended_regions.append(str("> Likely defence operation in " + region + ".\n" + link))
                    # Otherwise, it was either probably invaded by independents or imperialists, or a false alarm.
                    else:
                        # Log
                        print("* Suspicious activity in " + region + ".")
                        # Generate link
                        link = "https://www.nationstates.net/region=" + region
                        # Append report
                        suspicious_regions.append(str("> Suspicious delegacy change in " + region + ".\n" + link))

        # Increase counter
        i += 1

    # Build report

    print("Saving Ops...")

    if invaded_regions or suspicious_regions or defended_regions:
        # Build report
        string = "Date: " + date_today + " \n" + "Time: " + time_now + " \n"
        if invaded_regions:
            string += "\n\n=== Possible Raider Activity ===\n"
            for report in invaded_regions:
                string += report + "\n"
        if suspicious_regions:
            string += "\n\n=== Suspicious Delegacy Changes ===\n"
            for report in suspicious_regions:
                string += report + "\n"
        if defended_regions:
            string += "\n\n=== Likely Defence Operations ===\n"
            for report in defended_regions:
                string += report + "\n"
    else:
        # Log
        print("* No suspicious activity found.")
        # Build report
        string = "Date: " + date_today + " \n" + "Time: " + time_now + " \n"
        string += "\n\n=== No suspicious activity found ===\n"

    # Write report to file
    filename = "Kronos-Ops_" + date_today + ".txt"
    with open(filename, "w") as file:
        file.write(string)


# ----------------------------------------------- Region Update Timer ------------------------------------------------ #


# For timer only
if arg_timer is True:

    # Function to read a .xlsx file
    def xlsxreader(file_name):
        # Open the .xlsx as a zip
        from zipfile import ZipFile

        z = ZipFile(file_name)
        rows = []
        row = []
        value = ""
        # Go trough the sheet
        for e, el in iterparse(z.open("xl/worksheets/sheet1.xml")):
            # Extract value
            if el.tag.endswith("}v") or el.tag.endswith("}t"):
                value = el.text
            # Check value
            if el.tag.endswith("}c"):
                # If it's the region's name, remove the tags added to it by kronos
                if el.attrib["r"][:1] == "A" and int(el.attrib["r"][1:]) > 1:
                    value = value.split(" ")[1:]
                    value = " ".join(value)
                # Unless it's the delegate votes or region link, which we don't need, add it to the row
                if el.attrib["r"][:1] != "F" and el.attrib["r"][:1] != "G":
                    row.append(value)
                value = ""
            # Store the row in rows, and empty the row for a new cycle
            if el.tag.endswith("}row"):
                rows.append(row)
                row = []
        # Return the list of rows
        return rows


    # Get lastupdate from trigger
    def lwget(tn):
        global downloaded
        global _t
        u = "https://www.nationstates.net/cgi-bin/api.cgi?region=" + tn + "&q=lastupdate"
        req = Request(u, None, headers=user_agent)
        try:
            with urlopen(req) as a:
                lu = a.read()
                downloaded += getsizeof(a)
            lu = str(lu.decode("utf-8")).replace("\n", "")
            lu = int(search("<LASTUPDATE>(.*)</LASTUPDATE>", lu).group(1))
        except HTTPError:
            # Try again
            # Maybe this second request is what's blocking the timer sometimes
            sleep(0.6)
            try:
                with urlopen(req) as a:
                    lu = a.read()
                    downloaded += getsizeof(a)
                lu = str(lu.decode("utf-8")).replace("\n", "")
                lu = int(search("<LASTUPDATE>(.*)</LASTUPDATE>", lu).group(1))
            except HTTPError:
                # Region likely doesn't exist
                _t += 1
                lu = -1
        return lu


    def trigger_watchdog():
        # Variables
        global twd_queue
        twd_queue = []
        global thread_error
        thread_error = None
        global _t
        _posix_now = time()
        _t = 0
        _lastupdate = 0
        _last_trigger = _posix_now - (old_seconds_per_nation * 200)
        _delta_cn = 200
        _seconds_per_nation = old_seconds_per_nation
        _spns = []

        # While the main code didn't exit
        while twd_run is True:
            try:
                _posix_now = time()
                # If update has started
                if not _posix_now < update_start:
                    # Check the triggers for when they last updated
                    _trigger_name = triggers[_t][1].lower().replace(" ", "_")
                    _lastupdate = lwget(_trigger_name)
                # If update has not started yet, or it has but the first trigger hasn't updated yet
                if _posix_now < update_start or _lastupdate < update_start and _t == 0:
                    _case = 1
                # First trigger hit of the update, start predicting using triggers
                elif _t == 0:
                    _case = 1
                    _last_trigger = _lastupdate
                    _t += 1
                else:
                    # Get the cumulative nations of the last trigger
                    _trigger_cn = triggers[_t - 1][2]
                    # Get the cumulative nations of the upcoming trigger
                    _upcoming_trigger_cn = triggers[_t][2]
                    # Calculate the nations between the trigger and the target
                    _delta_cn = cumulative_nations - _trigger_cn
                    # During update for triggers between which there aren't more than 500 nations
                    if _lastupdate < update_start and not (_upcoming_trigger_cn - _trigger_cn) > 500:
                        _case = 2
                        # The seconds per nation is the difference between the lastupdate of the last trigger
                        # and the start of update, divided by the amount of nations updated when the last
                        # trigger did
                        _seconds_per_nation = (_last_trigger - update_start) / _trigger_cn
                    # During update for triggers between which there are more than 500 nations, and if we can calculate
                    # an average seconds_per_nations from the previous ones
                    elif _lastupdate < update_start and len(_spns) != 0:
                        _case = 3
                        # In this case we simply take the average of the update speed so far. Pray the user
                        # has the script running for a while, otherwise this is useless
                        _seconds_per_nation = sum(_spns) / len(_spns)
                    # Else the trigger updated or there was no data on it. Either way, we need to switch to the next one
                    else:
                        _case = 2
                        # Add seconds per nation to list so we can calculate the average later
                        try:
                            _spns.append(_seconds_per_nation)
                        except UnboundLocalError:  # Region updated before program caught it, or no data found on region
                            pass                   # Doesn't matter, next region will provide data
                        _last_trigger = _lastupdate
                        _t += 1

                # If update has started, we are consulting the API.
                if not _posix_now < update_start:
                    # Subtract code excecution time from time to wait, unless it took too long.
                    # Minimum time between requests is 0.6s (30s/50r), but we don't need a higher precision than 1s.
                    # No use bothering NS' servers more than absolutely necessary during update.
                    _dT = time() - _posix_now
                    if _dT < 1:
                        _dT = 1 - _dT
                        sleep(_dT)
                # Put list of variables in queue, unless the queue wasn't emptied yet
                if not twd_queue:
                    twd_queue = [_case, _seconds_per_nation, _last_trigger, _delta_cn]
            except (IndexError, KeyboardInterrupt, Exception) as err:
                er = (str(type(err).__name__))
                if er == "IndexError":
                    thread_error = "IndexError"
                    break
                elif er == "KeyboardInterrupt":
                    thread_error = "KeyboardInterrupt"
                    break
                else:
                    thread_error = str(format_exc())
                    break
        # Quit function when twd_run becomes false, or when an error occurs
        return

    # Read the kronos sheet
    file = "Kronos_" + date_today + ".xlsx"
    xlsx = xlsxreader(file)[1:]  # We don't need the headers.

    # Region input loop
    while True:
        # Clear screen
        if sysname == "nt":
            system("cls")
        else:
            system("clear")

        # Print loop controls
        print("Provide no region below to exit.")

        # Get the targeted region
        target_name = input("\nTarget Region: ")
        # Exit if no region name provided
        if len(target_name) == 0:
            break
        # White space between target region and timer
        print("")

        # If the timer would be used before or during minor
        if 2 < hour_now < 14:
            old_seconds_per_nation = float(xlsx[5][6].split(" ")[:1][0])
            update_start = posix_today + 61200
        # Otherwise before or during major
        else:
            old_seconds_per_nation = float(xlsx[2][6].split(" ")[:1][0])
            update_start = posix_today + 18000

        # If DST is in effect for America/New_York, subtract one hour
        if dumb_saving_time is True:
            update_start -= 3600

        # Get the cumulative nations for target_name, and its index in the xlsx list
        target_index = 0
        cumulative_nations = 0
        for region in xlsx:
            if target_name.lower().replace(" ", "_") == region[0].lower().replace(" ", "_"):
                cumulative_nations = int(region[4])
                break
            else:
                target_index += 1

        # If the target region exists
        if target_index < len(xlsx):
            # Get the trigger regions. Each trigger region updates at least 200 nations earlier than the last
            triggers = []
            cn = 99999999999999  # These impossible numbers are to include the target region as a trigger, it
            tcn = 99999999999999  # being the final one. They start the loops at target_index and get the
            n = ""  # needed variables. Then they are overwritten and it functions as normal.
            i = target_index
            # While the cumulative nations for the current trigger region is greater than 200
            while (tcn - 200) > 0:
                # While the cumulative nations for this region is greater than the cumulative nations of the previous
                # trigger region minus 200, keep looking for the next earlier trigger region. In a sense this is the
                # "accuracy" of the timer. But, the higher the accuracy, the higher the inconsistency in the countdown.
                while cn > (tcn - 200):
                    cn = int(xlsx[i][4])
                    n = xlsx[i][0]
                    i -= 1
                # If new trigger found, save its index in the list, name and cumulative nations, and reset cycle
                triggers.append([i, n, cn])
                tcn = cn

            # Print column names
            print("Time     | Variance     | Status")
            print("--------- -------------- -------------------------")

            # If update has already started, check for each trigger (from latest to earliest, as the list currently is)
            # that it hasn't updated yet
            posix_now = time()
            if update_start < posix_now:
                _t = 0
                try:
                    for trigger in triggers:
                        posix_now = time()
                        # Seconds to update is the seconds per nation for each nation up to the target, plus the
                        # (negative) difference in time between now and the start of update
                        seconds_to_update = (old_seconds_per_nation * cumulative_nations) + (update_start - posix_now)
                        # Convert seconds to readable HMS time. Cut off milliseconds.
                        hms_to_update = hms(seconds_to_update)[:-3]
                        # Except for the first trigger (which is the only trigger if the region already updated)
                        if not _t == 0:
                            # Display
                            variance = "n/a"
                            status = "<!> Checking triggers."
                            output = "{0:<5} | {1:<12} | {2:<24}".format(hms_to_update, variance, status)
                            print("\r" + output, end='', flush=False)
                        # Get lastupdate from trigger
                        trigger_name = triggers[_t][1].lower().replace(" ", "_")
                        lastupdate = lwget(trigger_name)
                        # If the trigger has already updated this update, exit the loop
                        if lastupdate > update_start:
                            # Delete one extra region to be safe
                            _t += 1
                            break
                        else:
                            _t += 1
                        # Determine if, after all this code, we still need to wait to honour the NS API ratelimit.
                        # Wait for the full second if the code took less than a second to execute. No higher
                        # precision is needed as it can't be displayed anyway.
                        dT = time() - posix_now
                        if dT < 0.6:
                            # Wait until the full second has passed
                            dT = 0.6 - dT
                            sleep(dT)

                except (KeyboardInterrupt, Exception) as error:
                    r = (str(type(error).__name__))
                    if r == "KeyboardInterrupt":
                        print("\nTimer interrupted.")
                        sleep(1)
                        break
                    else:
                        print("Unexpected error! Please report the below message. Press [Enter] to quit.\n")
                        print(format_exc())
                        quit()

                # Cut all triggers that have already updated off from the list. As the list goes from latest to earliest
                # this means the triggers that have already updated would be at the end of the list.
                triggers = triggers[:_t]

            # Reverse trigger list, because right now it goes from latest to earliest
            triggers = triggers[::-1]

            # Default variables
            case = 1
            seconds_to_update = 1
            twd_run = True
            twd_queue = []
            twd = Thread(target=trigger_watchdog)
            twd.daemon = True
            twd.start()

            # Update timer loop
            while seconds_to_update > 0:
                # Current posix time
                posix_now = time()
                try:
                    # Read information from trigger_watchdog (if it doesn't exist yet use defaults)
                    if twd_queue and not thread_error:
                        # print("Queue: " + str(twd_queue))             # Debug
                        # Get info from trigger_watchdog, empty queue
                        case = twd_queue[0]
                        seconds_per_nation = twd_queue[1]
                        last_trigger = twd_queue[2]
                        delta_cn = twd_queue[3]
                        twd_queue = []
                    # Process cases
                    if case == 1 and not thread_error:
                        # Seconds to update is the seconds per nation for each nation up to the target, plus the
                        # (negative) difference in time between now and the start of update
                        seconds_to_update = (old_seconds_per_nation * cumulative_nations) + (update_start - posix_now)
                        # Convert seconds to readable HMS time. Cut off milliseconds.
                        hms_to_update = hms(seconds_to_update)[:-3]
                        # Display
                        variance = "n/a"
                        status = "<!> Using sheet times."
                        output = "{0:<5} | {1:<12} | {2:<24}".format(hms_to_update, variance, status)
                        print("\r" + output, end='', flush=False)
                    elif case == 2 or case == 3 and not thread_error:
                        # Seconds to update is the seconds per nation for each nation between the cumulative
                        # nations of the last trigger and the cumulative nations of the target, plus the
                        # (negative) difference in time between the time the trigger updated and now
                        seconds_to_update = (seconds_per_nation * delta_cn) + (last_trigger - posix_now)
                        # Variance is the difference in nations between the current trigger and the target times
                        # the seconds per nation, minus the same difference times the seconds per nation
                        # according to the sheet. This returns how much the prediction deviates from the sheet,
                        # and as it changes with each trigger should give the user a good impression of update
                        # variance.
                        variance = (delta_cn * seconds_per_nation) - (delta_cn * old_seconds_per_nation)
                        variance = str(round(variance, 5))[:12]
                        # Convert seconds to readable HMS time. Cut off milliseconds.
                        hms_to_update = hms(seconds_to_update)[:-3]
                        if case == 2:
                            # Display
                            status = ""
                            output = "{0:<5} | {1:<12} | {2:<24}".format(hms_to_update, variance, status)
                            print("\r" + output, end='', flush=False)
                        elif case == 3:
                            # Display
                            status = "<!> Large region ahead!"
                            output = "{0:<5} | {1:<12} | {2:<24}".format(hms_to_update, variance, status)
                            print("\r" + output, end='', flush=False)
                    # Handle thread errors
                    else:
                        if thread_error == "IndexError":
                            print("\nRegion updated! Press [Enter] to go back.")
                            input()
                            break
                        elif thread_error == "KeyboardInterrupt":
                            print("\nTimer interrupted.")
                            sleep(1)
                            break
                        else:
                            print("Unexpected error! Please report the below message.\n")
                            print(format_exc())
                            quit()
                # Handle errors that might occur, especially KeyboardInterrupt (when the user presses CTRL+C) and
                # IndexError, which means the region already updated this update ("this update" lasts from its start
                # until two hours after).
                except (KeyboardInterrupt, Exception) as error:
                    r = (str(type(error).__name__))
                    if r == "KeyboardInterrupt":
                        print("\nTimer interrupted.")
                        sleep(1)
                        break
                    else:
                        print("Unexpected error! Please report the below message.\n")
                        print(format_exc())
                        quit()

            # If the region updated
            if seconds_to_update <= 0:
                print("\nRegion updated! Press [Enter] to go back.")
                input()

            # Exit thread
            twd_run = False
            twd.join()

        # If the target region doesn't exist
        else:
            print("Region not found.")
            sleep(1)

# -------------------------------------------------------------------------------------------------------------------- #
print("\nKronos downloaded " + str(ceil(downloaded / 1024)) + " KiB of data in total.\nGoodbye!\n")
# -------------------------------------------------------------------------------------------------------------------- #
