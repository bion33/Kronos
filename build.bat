:: Create windows executable
python cxfreeze Kronos.py --compress -OO --target-dir "Kronos" --icon "icon.ico"
:: Create windows archive
COPY README.md "./Kronos"
COPY LICENSE "./Kronos"
COPY batch.bat "./Kronos"

:: Create Windows zip file
7z a -tzip "Kronos.zip" "./Kronos"

:: Clean up
ECHO Y | RMDIR /S "./Kronos"


:: Create Linux archive
MKDIR "./Kronos"
COPY Kronos.py "./Kronos"
COPY README.md "./Kronos"
COPY LICENSE "./Kronos"
COPY bash.sh "./Kronos"

:: Create Linux zip file
7z a -ttar "Kronos.tar" "./Kronos" | 7z a -si Kronos.tar.gz

:: Clean up
ECHO Y | DEL "./Kronos.tar"
ECHO Y | RMDIR /S "./Kronos"