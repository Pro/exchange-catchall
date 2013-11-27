write-host " *** Exchange CatchAll Install Script ***" -f "blue"
write-host "Please select your Exchange Version from the following list:" -f "cyan"
write-host "[1] Exchange 2007 SP3" -f "cyan"
write-host "[2] Exchange 2010 (no Service Pack)" -f "cyan"
write-host "[3] Exchange 2010 SP1" -f "cyan"
write-host "[4] Exchange 2010 SP2" -f "cyan"
write-host "[5] Exchange 2010 SP3" -f "cyan"
write-host "[6] Exchange 2013" -f "cyan"

write-host ""
do { 
	$version = read-host "Your selection"
	if ($version -lt 1 -or $version -gt 6) {
		write-host "Invalid selection. Please input the number in the squares." -f "red"
	} 
} until ($version -ge 1 -and $version -le 6) 

$EXDIR="C:\Program Files\Exchange CatchAll" 
if ($version -eq 1) {
	$SRCDIR="CatchAllAgent\bin\Exchange 2007 SP3"
} elseif ($version -eq 2) {
	$SRCDIR="CatchAllAgent\bin\Exchange 2010"
} elseif ($version -eq 3) {
	$SRCDIR="CatchAllAgent\bin\Exchange 2010 SP1"
} elseif ($version -eq 4) {
	$SRCDIR="CatchAllAgent\bin\Exchange 2010 S23"
} elseif ($version -eq 5) {
	$SRCDIR="CatchAllAgent\bin\Exchange 2010 SP3"
} elseif ($version -eq 6) {
	$SRCDIR="CatchAllAgent\bin\Exchange 2013"
}

write-host "Creating registry key for EventLog" -f "green"
if (Test-Path "HKLM:\SYSTEM\CurrentControlSet\Services\EventLog\Application\Exchange CatchAll") {
	write-host "Registry key for EventLog already exists. Continuing..." -f "yellow"
} else {
	New-Item -Path "HKLM:\SYSTEM\CurrentControlSet\Services\EventLog\Application\Exchange CatchAll"
}


net stop MSExchangeTransport 
 
write-host "Creating install directory: '$EXDIR' and copying data from '$SRCDIR'"  -f "green"
new-item -Type Directory -path $EXDIR -ErrorAction SilentlyContinue 

copy-item "$SRCDIR\ExchangeCatchAll.dll" $EXDIR -force 
$overwrite = read-host "Do you want to copy (and overwrite) the config file: '$SRCDIR\ExchangeCatchAll.dll'? [Y/N]"
if ($overwrite -eq "Y" -or $overwrite -eq "y") {
	copy-item "$SRCDIR\ExchangeCatchAll.dll.config" $EXDIR -force
} else {
	write-host "Not copying config file" -f "yellow"
}

copy-item "$SRCDIR\mysql.data.dll" $EXDIR -force 

read-host "Now open '$EXDIR\ExchangeCatchAll.dll.config' to configure Exchange CatchAll. When done and saved press 'Return'"

write-host "Registering agent" -f "green"
Install-TransportAgent -Name "Exchange CatchAll" -TransportAgentFactory "Exchange.CatchAll.CatchAllFactory" -AssemblyPath "$EXDIR\ExchangeCatchAll.dll"

write-host "Enabling agent" -f "green" 
enable-transportagent -Identity "Exchange CatchAll" 
get-transportagent 
 
write-host "Starting Edge Transport" -f "green" 
net start MSExchangeTransport 
 
write-host "Installation complete. Check previous outputs for any errors!" -f "yellow" 