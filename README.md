Exchange CatchAll Agent
=============

CatchAll Agent for Exchange Server.

This code is based on the work of http://catchallagent.codeplex.com/

You can define a domain or subdomain and redirect all E-Mails sent to this domain to forward them to another address.
Using MySQL (not required for basic functionality) you get additional features:
- you can block E-Mails sent to specific addresses of such a catchall domain.
- the number of blocked hits will be logged
- each forwarded E-Mail will be logged

## Supported versions

The .dll is compiled for .NET 3.5

If it's running on other Exchange versions not mentioned here, please notify me, so I can update it here.

### Exchange 2013

See http://technet.microsoft.com/en-us/library/jj591524%28v=exchg.150%29.aspx for instructions on how to use .dll compiled for Exchange 2010

### Exchange 2010

This Receive Agent is fully tested under Exchange 2010 SP3 with Windows Server 2008 R2.

There's are also compiled versions for no Service Pack, SP1 and SP2 installed.

### Exchange 2007

Exchange 2007 SP3 .dll is build and can be found in the release directory. Please check if those are working for you and send me a short notice.

## Installing the Receive Agent

1. Download the .zip and extract it e.g. on the Desktop: [Exchange CatchAll Master.zip](https://github.com/Pro/exchange-catchall/archive/master.zip)
2. If you want to use MySQL, then install MySQL Server and execute the commands from `database.sql` to create the corresponding tables (modify the commands if needed). Don't forget to grant permissions to the user.
3. Open "Exchange Management Shell" from the Startmenu
4. Execute the following command to allow execution of local scripts (will be reset at last step): `Set-ExecutionPolicy Unrestricted`
5. Cd into the folder where the zip has been extracted.
6. Execute the install script `.\install.ps1`
7. Follow the instructions. For the configuration see next section.
8. Reset the execution policy: `Set-ExecutionPolicy Restricted`
9. Check EventLog for errors or warnings.
 Hint: you can create a user defined view in EventLog and then select "Per Source" and as the value "Exchange CatchAll"

Make sure that the priority of the CatchAll Agent is quite high (best is to set it directly after any Antivirus system).
To get a list of all the Export Agents use the Command `Get-TransportAgent`

To change the priority use `Set-TransportAgent -Identity "Exchange CatchAll" -Priority 3`
 
### Configuring the agent
Edit the .config file to fit your needs.

The `domainSection` defines the CatchAll domains. The configuration below forwards all E-Mails to `*@example.com` to the address `you@example.com` and `*@foo.com` to `you@foo.org`.
The destination address must be handled by the local exchange server and cannot be an external E-Mail address. Also make sure that you don't create a circular redirection (using the same domain for `name` and `address`).

The `customSection` defines different application settings:

* `database`: the settings for MySQL logging and blocked check. If you don't need it, set `enabled` to false. Currently only `mysql` is supported but feel free to create a pull request for other databases.
* `general`: Set LogLevel (See Logging section below). Enable/Disable 'X-OrigTo' header by setting `AddOrigToHeader` accordingly. `RejectIfBlocked` defines, if a blocked E-Mail address in MySQL causes a '550 5.1.1 Recipient rejected' response.

```xml
    <domainSection>
      <Domains>
        <Domain name="example.com" address="you@example.org"/>
        <Domain name="foo.com" address="you@foo.org" />
      </Domains>
    </domainSection>
    <customSection>
      <general LogLevel="3" AddOrigToHeader="true" RejectIfBlocked="false" />
      <database enabled="true" type="mysql" host="localhost" port="3306" database="catchall" user="catchall" password="catchall" />
    </customSection>
```


#### Logging
The CatchAll agent logs by default all errors and warnings into EventLog.
You can set the LogLevel in the .config file:

```xml
<setting name="LogLevel" serializeAs="String">
  <value>2</value>
</setting> 
```

Possible values:
* 0 = no logging
* 1 = Error only
* 2 = Warn+Error
* 3 = Info+Warn+Error


## Updating the Transport Agent

If you want to update the Exchange DKIM Transport Agent simply redownload the .zip file and follow the steps in the installation section.

## Uninstalling the Transport Agent

Follow the install instructions but execute `.\uninstall.ps1` instead.

## Known bugs

None

## Notes for developers

### Required DLLs for developing

It isn't allowed to distribute the .dll required for development of this transport agent.
http://blogs.msdn.com/b/webdav_101/archive/2009/04/02/don-t-redistribute-product-dlls-unless-you-know-its-safe-and-legal-to-do-so.aspx

Therefore you have to copy all files from 
<pre>
C:\Program Files\Microsoft\Exchange Server\V14\Public
Microsoft.Exchange.Data.Common.dll
Microsoft.Exchange.Data.Common.xml
Microsoft.Exchange.Data.Transport.dll
Microsoft.Exchange.Data.Transport.xml
</pre>
into the corresponding subdirectory from the Lib directory of this project.

#### Debugging
If you want to debug the .dll on your Exchange Server, you need to install [Visual Studio Remote Debugging](msdn.microsoft.com/en-us/library/vstudio/bt727f1t.aspx) on the Server.

1. After the Remote Debugging Tools are installed on the Server, open Visual Studio
2. Compile the .dll with Debug information
3. Copy the recompiled .dll to the server
4. In Visual Studio select Debug->Attach to Process
5. Under 'Qualifier' input the server IP oder Host Name
6. Select "Show processes from all users"
7. Select the process `EdgeTransport.exe` and then press 'Attach'
8. When reached, the process should stop at the breakpoint

## Changelog

* 25.11.2013:
	- Added custom X-OrigTo header.
	- Added install and uninstall script.
	- Added build for all Exchange 2010 Versions with different SPs.
