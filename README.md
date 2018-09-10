# Amendment.Web

[![Build Status](https://dev.azure.com/columbus0380/amendment/_apis/build/status/AdamLJohnson.Amendment)](https://dev.azure.com/columbus0380/amendment/_build/latest?definitionId=1)

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