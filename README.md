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

There's also a version for Exchange 2010 without any Service Packs installed.

### Exchange 2007

Exchange 2007 SP3 .dll is build and can be found in the release directory. Please check if those are working for you and send me a short notice.

## Installing the Receive Agent

1. Copy all the files from the folder matching your Exchange Server version from the [release directory](CatchAllAgent/bin) into a directory on the server, where Exchange runs.
Eg. into `C:\Program Files\Exchange CatchAll\`. Also copy the `Exchange.CatchAll.dll.config` to the same directory. The final structure should be:
<pre>
C:\Program Files\Exchange CatchAll\Exchange.CatchAll.dll
C:\Program Files\Exchange CatchAll\Exchange.CatchAll.dll.config
</pre>

2. Create the registry key for EventLog by executing the script: [Create Key.reg](Utils/Create key.reg?raw=true)

4. Add `C:\Program Files\Exchange CatchAll\` to your PATH environment variable:

 Normal command prompt: `set "path=%path%;C:\Program Files\Exchange CatchAll"`
 
 or in the Power shell: `setx PATH "$env:path;C:\Program Files\Exchange CatchAll" -m`

 (If you execute the following command in the same shell, you need to first restart the shell load the new environment variable)

5. Then open Exchange Management Shell
<pre>
	Install-TransportAgent -Name "Exchange CatchAll" -TransportAgentFactory "Exchange.CatchAll.CatchAllFactory" -AssemblyPath "C:\Program Files\Exchange CatchAll\Exchange.CatchAll.dll"
	 
	Enable-TransportAgent -Identity "Exchange CatchAll"
	Restart-Service MSExchangeTransport
</pre>
6. Close the Exchange Management Shell Window
7. Check EventLog for errors or warnings.
 Hint: you can create a user defined view in EventLog and then select "Per Source" and as the value "Exchange CatchAll"

### Configuring the agent
Edit the .config file to fit your needs.

The `domainSection` defines the CatchAll domains. The configuration below forwards all E-Mails to `*@example.com` to the address `you@example.com` and `*@foo.com` to `you@foo.org`.
The destination address must be handled by the local exchange server and cannot be an external E-Mail address. Also make sure that you don't create a circular redirection (using the same domain for `name` and `address`).

The `databaseSection` defines the settings for MySQL logging and blocked check. If you don't need it, set `enabled` to false. Currently only `mysql` is supported but feel free to create a pull request for other databases.

```xml
  <domainSection>
    <Domains>
      <Domain name="example.com" address="you@example.org"/>
      <Domain name="foo.com" address="you@foo.org" />
    </Domains>
  </domainSection>
  <databaseSection>
    <database enabled="true" type="mysql" host="localhost" port="3306" database="catchall" user="catchall" password="catchall" />
  </databaseSection>
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
0 = no logging
1 = Error only
2 = Warn+Error
3 = Info+Warn+Error


## Updating the Transport Agent

If you want to update the Exchange DKIM Transport Agent, you need to do the following:

* Open Powershell and stop the services, which block the .dll

        StopService MSExchangeTransport
       
* Then download [ExchangeCatchAll.dll](Src/ExchangeCatchAll/bin) and overwrite the existing .dll
* Start the services again

        StartService MSExchangeTransport

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

## Changelog

* 25.11.2013: Added custom X-OrigTo header.
