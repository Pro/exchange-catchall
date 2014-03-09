write-host " *** Exchange CatchAll UNINSTALL Script ***" -f "blue"

$EXDIR="C:\Program Files\Exchange CatchAll" 
 
Net Stop MSExchangeTransport 
 
write-host "Disabling agent..."  -f "green"
Disable-TransportAgent -Identity "Exchange CatchAll" 

write-host "Uninstalling agent..."  -f "green"
Uninstall-TransportAgent -Identity "Exchange CatchAll" 
 
write-host "Deleting directories and files..." -f "green"
Remove-Item $EXDIR\* -Recurse -Force -ErrorAction SilentlyContinue 
Remove-Item $EXDIR -Recurse -Force -ErrorAction SilentlyContinue 

write-host "Removing registry key for EventLog" -f "green"
if (Test-Path "HKLM:\SYSTEM\CurrentControlSet\Services\EventLog\Application\Exchange CatchAll") {
	Remove-Item -Path "HKLM:\SYSTEM\CurrentControlSet\Services\EventLog\Application\Exchange CatchAll"
} else {
	write-host "Key already removed. Continuing..." -f "yellow"
}

write-host "Starting Transport..."  -f "green"
Net Start MSExchangeTransport 
 
write-host "Uninstallation complete. Check previous outputs for any errors!"  -f "yellow"