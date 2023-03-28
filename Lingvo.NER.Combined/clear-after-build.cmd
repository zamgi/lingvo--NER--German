rd ".vs" /S/Q
del "*.suo" /S/Q/F/A
del "*.csproj.user" /S/Q/F/A

for /f "usebackq" %%f in (`"dir /ad/b/s bin"`) do rd "%%f" /S/Q
for /f "usebackq" %%f in (`"dir /ad/b/s obj"`) do rd "%%f" /S/Q

rem rd "Lingvo.NER.Combined\bin" /S/Q
rem rd "Lingvo.NER.Combined.ConsoleDemo\bin" /S/Q

rem cd .\Lingvo.NER.Combined.WebService
rem call "clear-after-build.cmd"
rem cd ..

