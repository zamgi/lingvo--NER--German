rd ".vs" /S/Q
del "*.suo" /S/Q/F/A
del "*.csproj.user" /S/Q/F/A

for /f "usebackq" %%f in (`"dir /ad/b/s obj"`) do rd "%%f" /S/Q
for /f "usebackq" %%f in (`"dir /ad/b/s bin"`) do rd "%%f" /S/Q

rem rem del "Lingvo.NER.Combined.WebService\bin\*.pdb" /Q
rem del "Lingvo.NER.Combined.WebService\bin\*.*" /Q
rem rd "Lingvo.NER.Combined.WebService\bin\ref" /S/Q
rem rd "Lingvo.NER.Combined.WebService\bin\runtimes" /S/Q
rem rd "Lingvo.NER.Combined.WebService\bin\win-x64" /S/Q

rem rd "Lingvo.NER.Combined.WebServiceTestApp\.vs" /S/Q
rem rd "Lingvo.NER.Combined.WebServiceTestApp\bin" /S/Q
