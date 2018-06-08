:: Create windows executable
python cxfreeze Kronos.py --compress -OO --target-dir "Kronos" --icon "icon.ico"
:: Create windows archive
COPY README.md "./Kronos"
COPY LICENSE "./Kronos"
COPY batch.bat "./Kronos"

:: Create Windows zip file
7z a -tzip "Kronos-Windows.zip" "./Kronos"

:: Clean up
ECHO Y | RMDIR /S "./Kronos"


:: Create Linux archive
MKDIR "./Kronos"
COPY Kronos.py "./Kronos"
COPY README.md "./Kronos"
COPY LICENSE "./Kronos"
COPY bash.sh "./Kronos"

:: Create Linux zip file
7z a -tzip "Kronos-Linux.zip" "./Kronos"

:: Clean up
ECHO Y | RMDIR /S "./Kronos"