@echo off

@echo Stopping Tridion Services
REM stopping the services with 'net stop' instead of 'sc stop' (this is synchronous)
net stop TCMPublisher
net stop TcmSearchIndexer
net stop TcmServiceHost

@echo Stopping Tridion Com+ application
cscript "Stop ComPlus.vbs"

@echo Adding file to GAC
"C:\Program Files (x86)\Microsoft SDKs\Windows\v7.0A\Bin\NETFX 4.0 Tools\gacutil.exe" /if "D:\Tridion\bin\extensions\Reliant.Tridion.Resolving.dll"

@echo Done
