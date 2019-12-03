@echo off
set TMPPATH=%temp%\copy
mkdir "%TMPPATH%"
winscp.com /command ^
    "open sftp://user:password@example.com/" ^
    "get ""/remote/path/*"" -filemask=*.txt ""%TMPPATH%""" ^
    "exit"
for /r "%TMPPATH%" %%f in ("*.*") do move "%%f" "C:\local\path\"
rmdir /s /q "%TMPPATH%"