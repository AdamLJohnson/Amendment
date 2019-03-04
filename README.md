# Amendment.Web

[![Build Status](https://dev.azure.com/columbus0380/amendment/_apis/build/status/AdamLJohnson.Amendment)](https://dev.azure.com/columbus0380/amendment/_build/latest?definitionId=1)

## Build Prerequisites
1. [DotNet Core v2.2 SDK](https://dotnet.microsoft.com/download)
1. [Web Compiler VS Extension](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.WebCompiler)

## Some quick commands

### Compile a release
To build a publish release. Run this command from the project's root folder: `dotnet publish --configuration Release`
 - Once the build is done you will find the files to be copied to the server at `\src\Amendment.Web\bin\Release\netcoreapp2.0\publish\`

### SQL and EF migration commands

Run these commands as needed from `\src\Amendment.Repository`
```
dotnet ef migrations remove
dotnet ef migrations add AddedAmendment
dotnet ef migrations script > d:\temp\sql.sql
```

### Server commands
```
sudo nano /etc/systemd/system/aspnetcore-amendment.service
sudo systemctl status aspnetcore-amendment.service
sudo systemctl enable aspnetcore-amendment.service
sudo systemctl start aspnetcore-amendment.service

sudo journalctl -fu aspnetcore-amendment.service
```

## Setup

### Startup Service Script

Save to `/etc/systemd/system/aspnetcore-amendment.service`
```
[Unit]
Description=Amendment translation web site

[Service]
WorkingDirectory=/var/aspnetcore/amendments
ExecStart=/usr/bin/dotnet /var/aspnetcore/amendments/Amendment.Web.dll
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
SyslogIdentifier=amendment-web
User=adamlj
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=true
Environment=ConnectionStrings__DefaultConnection=Server=<HOST>;Port=8715;Database=<DATABASENAME>;Uid=<USERNAME>;Pwd=<PASSWORD>;

[Install]
WantedBy=multi-user.target
```

### Allow Azure DevOps to restart the service

Add the following to `/etc/sudoers`:
```
%sudo   ALL=NOPASSWD: /bin/systemctl restart aspnetcore-amendment.service
```
