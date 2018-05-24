#!/usr/bin/env python3

# Note for running automatically at specific times:
# For accurate results to be available as soon as possible before the next update, run right after any update.


# -------------------------------------------------- Imports --------------------------------------------------------- #


import calendar
import gzip
import os
import re
import time
import urllib.request
import xml.etree.ElementTree as ElementTree
from datetime import timedelta, datetime

import pytz
import xlsxwriter


# ---------------------------------------------- User-Agent & Time --------------------------------------------------- #


print('Starting...')


# Set basic user-agent #
user_agent = 'Script: requests regions.xml.gz, happenings, passworded-, founderless- and invader regions once a day. ' \
             'User info: '

# Get User-Agent configured by user #
if os.path.isfile('config.txt'):
    with open('config.txt', 'r') as f:
        user = f.readlines()
    try:
        user_info = user[1]
    # If it has just one line, it's untouched by the user. Ask and add user info
    except IndexError:
        user_info = input('Enter your email address or nation: ')
        with open('config.txt', 'a') as f:
            f.write(user_info)
# If the file doesn't exist, create it
else:
    # Descriptive line for config #
    config = '# Fill in your nation or email address on the line below.' + '\n'
    # Ask user info
    user_info = input('Enter your email address or nation: ')
    config += user_info
    with open('config.txt', 'w') as f:
        f.write(config)
    user_agent += user_info

# Set User-Agent #
header = {'User-Agent': user_agent}


# Date, time and time zone settings #
utc_timezone = pytz.timezone("UTC")
ns_timezone = pytz.timezone("America/New_York")
hour_now = int(datetime.now(ns_timezone).strftime("%H"))
date_today = datetime.now(ns_timezone).strftime("%Y-%m-%d")
posix_today = datetime.now(utc_timezone).strftime("%Y-%m-%d")
posix_today = calendar.timegm(time.strptime(posix_today, "%Y-%m-%d"))
dumb_saving_time = pytz.utc.localize(datetime.utcnow())
dumb_saving_time = (dumb_saving_time.astimezone(ns_timezone).dst() != timedelta(0))


# -------------------------------------------------- Functions ------------------------------------------------------- #


# Get list of changes #
def changes(start, end):
    # Assemble URL #
    u = 'https://www.nationstates.net/cgi-bin/api.cgi?q=happenings;filter=change'
    u += ';sincetime=' + str(start) + ';beforetime=' + str(end) + ';limit=200'
    # Download and save as list #
    r = urllib.request.Request(u, None, headers=header)
    with urllib.request.urlopen(r) as resp:
        h = resp.read()
    c = h.decode('utf-8')
    return c.split(".")


# Find end of update #
def influence(cl):
    for i, s in enumerate(cl):
        if 'influence' in s:
            u = changes_list[i]
            # Return timestamp of influence #
            return int(re.search("<TIMESTAMP>(.*)</TIMESTAMP>", u).group(1))


# Calculate hours, minutes and seconds from seconds #
def hms(s):
    m = int(s / 60)
    s -= m * 60
    h = int(m / 60)
    m -= h * 60
    s = round(float(s), 2)
    if s < 10:
        s = "0" + str(s)
    else:
        s = str(s)
    if m < 10:
        m = "0" + str(m)
    else:
        m = str(m)
    if h < 10:
        h = "0" + str(h)
    else:
        h = str(h)
    return [h, m, s]


# ------------------------------------------------- Downloads -------------------------------------------------------- #


print('Downloading NationStates Data...')


# If .Regions_date_today.xml doesn't exist already, remove previous versions and download current version #
filename = ".Regions_" + date_today + ".xml"
if os.path.isfile(filename) is False:

    # Remove previous versions #
    pattern = '.Regions'
    for f in os.listdir(os.getcwd()):
        if re.search(pattern, f):
            os.remove(os.path.join(os.getcwd(), f))

    # Download regions.xml #
    url = 'https://www.nationstates.net/pages/regions.xml.gz'
    req = urllib.request.Request(url, None, headers=header)
    response = urllib.request.urlopen(req)
    data = response.read()

    # Save downloaded archive as regions.xml.gz #
    with open('regions.xml.gz', "wb") as local_file:
        local_file.write(data)

    # Extract archive to .Regions_date_today.xml #
    gz = gzip.open('regions.xml.gz', 'rb')
    extracted = gz.read()
    filename = ".Regions_" + date_today + ".xml"
    with open(filename, "wb") as local_file:
        local_file.write(extracted)

    # Close gzip, otherwise it remains "in use" #
    gz.close()

    # Remove original archive #
    os.remove('regions.xml.gz')


# API requests should be less than 50/30s. Wait 1s to be safe #
time.sleep(1)


# Download numnations and save as integer #
url = 'https://www.nationstates.net/cgi-bin/api.cgi?q=numnations'
req = urllib.request.Request(url, None, headers=header)
with urllib.request.urlopen(req) as response:
    html = response.read()
nations_total = html.decode('utf-8')
nations_total = int(re.search('<NUMNATIONS>(.*)</NUMNATIONS>', nations_total).group(1))


# API requests should be less than 50/30s. Wait 1s to be safe #
time.sleep(1)


# Download founderless REGIONS and save as list #
url = 'https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=founderless'
req = urllib.request.Request(url, None, headers=header)
with urllib.request.urlopen(req) as response:
    html = response.read()
founderless = html.decode('utf-8')
founderless_list = founderless.split(",")


# API requests should be less than 50/30s. Wait 1s to be safe #
time.sleep(1)


# Download REGIONS protected by a password and save as list #
url = 'https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=password'
req = urllib.request.Request(url, None, headers=header)
with urllib.request.urlopen(req) as response:
    html = response.read()
protected = html.decode('utf-8')
protected_list = protected.split(",")


# API requests should be less than 50/30s. Wait 1s to be safe #
time.sleep(1)


# Download regions marked with the invader tag and save as list
url = 'https://www.nationstates.net/cgi-bin/api.cgi?q=regionsbytag;tags=invader'
request = urllib.request.Request(url, None, headers=header)
with urllib.request.urlopen(request) as response:
    html = response.read()
# Filter out information and save as list
tagged = re.findall('<REGIONS>(.*?)</REGIONS>', str(html))
tagged = str(tagged).replace("['", '').replace("']", '')
tagged_list = tagged.split(',')


# ---------------------------------------------- Calibrate Update ---------------------------------------------------- #


print('Calibrating Update...')


# Time intervals to check for MAJOR (see posix time) #
possible_times = [[posix_today + 18000, posix_today + 25200],
                  [posix_today + 18000, posix_today + 24300],
                  [posix_today + 18000, posix_today + 23400],
                  [posix_today + 18000, posix_today + 22500],
                  [posix_today + 18000, posix_today + 21600],
                  [posix_today + 18000, posix_today + 20700],
                  [posix_today + 18000, posix_today + 19800],
                  [posix_today + 18000, posix_today + 18900]]

# Initial variables #
update_expected_start = None
update_expected_end = None
update_end = None
t = 0

# Loop trough possible time intervals from late to early #
while update_end is None and t <= 7:

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

    # Save as list #
    changes_list = changes(update_expected_start, update_expected_end)

    # Get end of update #
    update_end = influence(changes_list)

    t += 1

# Calculate MAJOR update length #
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
    with open('error-report.txt', "w") as file:
        file.write(error_message)
    print(error_message)
    quit()

# Time intervals to check for MINOR (see posix time) #
possible_times = [[posix_today + 61200, posix_today + 68400],
                  [posix_today + 61200, posix_today + 67500],
                  [posix_today + 61200, posix_today + 66600],
                  [posix_today + 61200, posix_today + 65700],
                  [posix_today + 61200, posix_today + 64800],
                  [posix_today + 61200, posix_today + 63900],
                  [posix_today + 61200, posix_today + 63000],
                  [posix_today + 61200, posix_today + 62100]]

# Initial variables #
update_expected_start = None
update_expected_end = None
update_end = None
t = 0

# Loop trough possible time intervals from late to early #
while update_end is None and t <= 7:

    # When requesting a sheet during or before MINOR we can only get results for MINOR from yesterday
    if 0 <= hour_now <= 14:
        update_expected_start = possible_times[t][0] - 86400
        update_expected_end = possible_times[t][1] - 86400
    else:
        update_expected_start = possible_times[t][0]
        update_expected_end = possible_times[t][1]

    # If DST is in effect for America/New_York, subtract one hour
    if dumb_saving_time is True:
        update_expected_start -= 3600
        update_expected_end -= 3600

    # Save as list #
    changes_list = changes(update_expected_start, update_expected_end)

    # Get end of update #
    update_end = influence(changes_list)

    t += 1

# Calculate MINOR update length #
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
    with open('error-report.txt', "w") as file:
        file.write(error_message)
    print(error_message)
    quit()

# API requests should be less than 50/30s. Wait 1s to be safe #
time.sleep(1)


# Get update length for MINOR from the API #
# Set expected start and end times of the previous MINOR update #


# Calculate update time per nation for MAJOR and MINOR #
major_time = update_length_major / nations_total
minor_time = update_length_minor / nations_total


# ----------------------------------------------- Process Regions ---------------------------------------------------- #


print('Processing Regions...')


regions_names = []
regions_links = []
regions_nation_counts = []
regions_delegate_votes = []
regions_delegate_authority = []

# Getting information from regions.xml #
filename = ".Regions_" + date_today + ".xml"

for event, elem in ElementTree.iterparse(filename):
    if elem.tag == 'REGION':
        name = elem.find('NAME').text
        regions_names += [name]
        regions_links += [str('https://www.nationstates.net/region=' + name).replace(' ', '_')]
        regions_nation_counts += [int(elem.find('NUMNATIONS').text)]
        regions_delegate_votes += [int(elem.find('DELEGATEVOTES').text)]
        authority = elem.find('DELEGATEAUTH').text
        if authority[0] == 'X':
            regions_delegate_authority += [True]
        else:
            regions_delegate_authority += [False]
        elem.clear()


# ------------------------------------------- Calculate Update Times ------------------------------------------------- #


print('Calculating Update Times...')


# Grabbing the cumulative number of nations that've updated by the time a region has.
regions_nations_cumulative = []
for found in regions_nation_counts:
    if len(regions_nations_cumulative) == 0:
        regions_nations_cumulative.extend([int(found)])
    else:
        regions_nations_cumulative.extend([int(found) + regions_nations_cumulative[-1]])


major_list = []
minor_list = []

# Getting the approximate major/minor update times for regions #
for found in regions_nations_cumulative:

    # Calculate MAJOR update time #
    seconds = found * major_time
    # Translate seconds to h:m:s #
    hms_list = hms(seconds)
    string = str(hms_list[0] + ":" + hms_list[1] + ":" + hms_list[2])
    # Store in list #
    major_list.append(string)

    # Calculate MINOR update time #
    seconds = found * minor_time
    # Translate seconds to h:m:s #
    hms_list = hms(seconds)
    string = str(hms_list[0] + ":" + hms_list[1] + ":" + hms_list[2])
    # Store in list #
    minor_list.append(string)


# ----------------------------------------------- Prepare Sheet ------------------------------------------------------ #


print('Preparing Sheet...')


# Opening up virtual sheet #
filename = "Kronos_" + date_today + ".xlsx"
wb = xlsxwriter.Workbook(filename, {'constant_memory': True})
ws = wb.add_worksheet()


# Set formatting #
right = wb.add_format({'align': 'right'})
green = wb.add_format({'bg_color': 'green'})
yellow = wb.add_format({'bg_color': 'yellow'})
orange = wb.add_format({'bg_color': 'orange'})
red = wb.add_format({'bg_color': 'red'})
shrink = wb.add_format({'shrink': True})
header = wb.add_format({'bold': True, 'bg_color': 'gray'})
right_header = wb.add_format({'bold': True, 'align': 'right', 'bg_color': 'gray'})
info_green = wb.add_format({'align': 'right', 'bg_color': 'green'})
info_yellow = wb.add_format({'align': 'right', 'bg_color': 'yellow'})
info_orange = wb.add_format({'align': 'right', 'bg_color': 'orange'})
info_red = wb.add_format({'align': 'right', 'bg_color': 'red'})
region_green = wb.add_format({'shrink': True, 'bg_color': 'green'})
region_yellow = wb.add_format({'shrink': True, 'bg_color': 'yellow'})
region_orange = wb.add_format({'shrink': True, 'bg_color': 'orange'})
region_red = wb.add_format({'shrink': True, 'bg_color': 'red'})


# Set headers #
# ws.write(row, column, cell content, format)
# Note that 'A1' is row 0 and column 0, 'B2' is row 1 and column 1, etc.
ws.write(0, 0, 'Region', header)
ws.write(0, 1, 'Major', header)
ws.write(0, 2, 'Minor', header)
ws.write(0, 3, 'Nations', header)
ws.write(0, 4, 'Cumulative', header)
ws.write(0, 5, "Endo's", header)
ws.write(0, 6, 'Link', header)
ws.write(0, 8, 'World', right_header)
ws.write(0, 9, 'Data', header)


# Building the sheet #
counter = 1

for found in regions_names:
    region = found

    # Add tags and write region name
    _tags = ''
    # Has Founder
    if found not in founderless_list:
        _tags += '+'
        tags = '[' + _tags + '] '
        ws.write(counter, 0, tags + region, region_green)
    # Has password
    if found in protected_list:
        _tags = '#' + _tags
        tags = '[' + _tags + '] '
        ws.write(counter, 0, tags + region, region_green)
    # Has executive Delegacy and password
    if regions_delegate_authority[counter - 1] is True and found in protected_list:
        _tags = '~' + _tags
        tags = '[' + _tags + '] '
        ws.write(counter, 0, tags + region, region_green)
    # Has executive Delegacy, but no password
    if regions_delegate_authority[counter - 1] is True and found not in protected_list:
        _tags = '~' + _tags
        tags = '[' + _tags + '] '
        ws.write(counter, 0, tags + region, region_yellow)
    # Has no Founder, but has a password
    if found in founderless_list and found in protected_list:
        _tags = '!' + _tags
        tags = '[' + _tags + '] '
        ws.write(counter, 0, tags + region, region_green)
    # Has no Founder and no password
    if found in founderless_list and found not in protected_list:
        _tags = '!' + _tags
        tags = '[' + _tags + '] '
        ws.write(counter, 0, tags + region, region_orange)
    # Has been tagged "Invader"
    if found in tagged_list:
        _tags = '!!' + _tags
        tags = '[' + _tags + '] '
        ws.write(counter, 0, tags + region, region_red)
    else:
        tags = '[]'

    # Write region properties #
    ws.write(counter, 1, major_list[counter - 1])
    ws.write(counter, 2, minor_list[counter - 1])
    ws.write(counter, 3, regions_nation_counts[counter - 1])
    ws.write(counter, 4, regions_nations_cumulative[counter - 1])
    ws.write(counter, 5, regions_delegate_votes[counter - 1])
    ws.write(counter, 6, regions_links[counter - 1], shrink)

    # Extra info #
    # Placed here with if statements because of a xlsxwriter memory optimization which works row per row. Due to this
    # you can't edit a cell if you already passed the row, so these "extra" cells are processed in their respective
    # rows. See {'constant_memory': True})
    if counter < 14:
        if counter == 1:
            ws.write(counter, 8, 'Nations: ', right)
            ws.write(counter, 9, str(nations_total))
        if counter == 2:
            ws.write(counter, 8, 'Last Major: ', right)
            ws.write(counter, 9, str(update_length_major) + ' seconds')
        if counter == 3:
            ws.write(counter, 8, 'Secs/Nation: ', right)
            ws.write(counter, 9, str(major_time))
        if counter == 4:
            ws.write(counter, 8, 'Nations/Sec: ', right)
            ws.write(counter, 9, str(1 / major_time))
        if counter == 5:
            ws.write(counter, 8, 'Last Minor: ', right)
            ws.write(counter, 9, str(update_length_minor) + ' seconds')
        if counter == 6:
            ws.write(counter, 8, 'Secs/Nation: ', right)
            ws.write(counter, 9, str(minor_time))
        if counter == 7:
            ws.write(counter, 8, 'Nations/Sec: ', right)
            ws.write(counter, 9, str(1 / minor_time))
        if counter == 9:
            ws.write(counter, 8, 'Legend', right_header)
            ws.write(counter, 9, ':', header)
        if counter == 10:
            ws.write(counter, 8, 'Green:', info_green)
            ws.write(counter, 9, 'Founder [+] or Password [#]', green)
        if counter == 11:
            ws.write(counter, 8, 'Yellow:', info_yellow)
            ws.write(counter, 9, 'Executive Delegacy [~]', yellow)
        if counter == 12:
            ws.write(counter, 8, 'Orange:', info_orange)
            ws.write(counter, 9, 'Founderless [!]', orange)
        if counter == 13:
            ws.write(counter, 8, 'Red:', info_red)
            ws.write(counter, 9, 'Tagged "Invader" [!!]', red)

    counter += 1


# Make columns fit their content #
ws.set_column(0, 0, 50)
ws.set_column(1, 1, 12)
ws.set_column(2, 2, 12)
ws.set_column(3, 3, 10)
ws.set_column(4, 4, 10)
ws.set_column(5, 5, 10)
ws.set_column(6, 6, 60)
ws.set_column(8, 8, 11)
ws.set_column(9, 9, 24)


# ------------------------------------------------- Save Sheet ------------------------------------------------------- #


print('Saving Sheet...')


wb.close()

# -------------------------------------------------------------------------------------------------------------------- #
