# Amendment.Web

[![Docker](https://github.com/AdamLJohnson/Amendment/actions/workflows/docker-publish.yml/badge.svg)](https://github.com/AdamLJohnson/Amendment/actions/workflows/docker-publish.yml)

## Build Prerequisites
1. [DotNet 10 SDK](https://dotnet.microsoft.com/download) (10.0.200 or later, pinned in `global.json`)
1. [Web Compiler VS Extension](https://marketplace.visualstudio.com/items?itemName=MadsKristensen.WebCompiler)

## Some quick commands

### Stand up Docker Postgres Database

You can run a temp database in docker. To use docker you need to update the `appsettings.json` file as well.

`docker run -p 5432:5432 --name some-postgres -e POSTGRES_PASSWORD=mysecretpassword -e POSTGRES_USER=amendment -d postgres`

### Stand up a Redis Server

`docker run -p 6379:6379 -d redis`

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

## AWS infrastructure (CDK)

The AWS CDK v2 app lives in `infra/Amendment.Cdk` (C#, `net10.0`). Stage settings (region, hosted zone, hostname,
budget, log retention) are in `infra/Amendment.Cdk/Config/stages.json` and can be overridden with `-c key=value`
(`hostedZoneId`, `hostedZoneName`, `siteDomainName`, `alertEmail`, `monthlyBudgetUsd`).

| Stage | Hostname | Stacks |
|---|---|---|
| `dev` | `https://dev-amendments.adamlj.com` | `Amendment-dev-{Data,Identity,Api,Web,Observability}` |
| `prod` | `https://amendments.adamlj.com` | `Amendment-prod-*` (defined, not deployed without production approval) |

All stacks deploy to `us-east-1` (the CloudFront certificate must be there). The existing `adamlj.com` hosted zone is
imported by ID and never created or deleted. Every resource is tagged `Application=Amendment` and `Environment=<stage>`.

Prerequisites: .NET 10 SDK, Node.js 24, AWS CLI v2, Docker (for Floci). Run CDK commands from `infra/Amendment.Cdk`:

```
npm ci                                    # installs the pinned CDK CLI (package.json)
npx cdk synth -c stage=dev
npx cdk diff --all -c stage=dev
npx cdk deploy --all -c stage=dev --outputs-file cdk-outputs-dev.json
```

Then, from the repository root:

```
infra/scripts/publish-web.ps1 -Stage dev    # publishes the Blazor client, writes amendment-config.json, invalidates CloudFront
infra/scripts/destroy-stage.ps1 -Stage dev  # empties the site bucket, then cdk destroy --all (refuses production stages)
```

**Local validation with [Floci](https://floci.io)** (no AWS account used): `infra/scripts/floci-validate.ps1` starts a
fresh Floci container, deploys all dev stacks into it, checks status, tags and DNS records, publishes the web client,
destroys the stage and checks the shared hosted zone is untouched. Run it before every real deployment.

CDK assertion tests are in `test/Amendment.Cdk.Tests` and run with `dotnet test`. Production deployments need a reviewed
`npx cdk diff --all -c stage=prod` and explicit approval.

## Setup

Be sure that the user that runs the Azure DevOps agent has Read/Write permissions to `/var/aspnetcore/amendments`. You may need to create the directory.

### Startup Service Script

Save to `/etc/systemd/system/aspnetcore-amendment.service`
```
[Unit]
Description=Amendment translation web site

[Service]
WorkingDirectory=/var/aspnetcore/amendments
ExecStart=/usr/bin/dotnet /var/aspnetcore/amendments/Amendment.Server.dll --urls=http://*:5000
Restart=always
# Restart service after 10 seconds if the dotnet service crashes:
RestartSec=10
SyslogIdentifier=amendment-web
User=adamlj
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=true
Environment=ConnectionStrings__DefaultConnection=Host=<HOST>;Database=<DB>;Username=<USERNAME>;Password=<PASSWORD>;

[Install]
WantedBy=multi-user.target
```

### Allow Azure DevOps to restart the service

Add the following to `/etc/sudoers`:
```
%sudo   ALL=NOPASSWD: /bin/systemctl restart aspnetcore-amendment.service
```

### nginx.conf

**NOTE** Replace `DOMAINNAME` with the real domain name. You will also need to remove the let's encrypt/Certbot config until you are ready.

```
user www-data;
worker_processes auto;
pid /run/nginx.pid;
include /etc/nginx/modules-enabled/*.conf;

events {
        worker_connections 768;
        # multi_accept on;
}

http {
    include       mime.types;
    # anything written in /opt/nginx/conf/mime.types is interpreted as if written inside the http { } block

    default_type  application/octet-stream;

    map $http_connection $connection_upgrade {
        "~*Upgrade" $http_connection;
        default keep-alive;
    }

    server {
            server_name amendments.DOMAINNAME.com www.amendments.DOMAINNAME.com;

            # Configure the SignalR Endpoint
            location / {
                      # App server url
                      proxy_pass http://localhost:5000;

                      # Configuration for WebSockets
                      proxy_set_header Upgrade $http_upgrade;
                      proxy_set_header Connection $connection_upgrade;
                      proxy_cache off;
                      # WebSockets were implemented after http/1.0
                      proxy_http_version 1.1;

                      # Configuration for ServerSentEvents
                      proxy_buffering off;

                      # Configuration for LongPolling or if your KeepAliveInterval is longer than 60 seconds
                      proxy_read_timeout 100s;

                      proxy_set_header Host $host;
                      proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                      proxy_set_header X-Forwarded-Proto $scheme;
                     }

    listen 443 ssl; # managed by Certbot
    ssl_certificate /etc/letsencrypt/live/amendments.DOMAINNAME.com/fullchain.pem; # managed by Certbot
    ssl_certificate_key /etc/letsencrypt/live/amendments.DOMAINNAME.com/privkey.pem; # managed by Certbot
    include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot


}


    server {
            server_name dev-amendments.DOMAINNAME.com www.dev-amendments.DOMAINNAME.com;

            # Configure the SignalR Endpoint
            location / {
                      # App server url
                      proxy_pass http://localhost:5050;

                      # Configuration for WebSockets
                      proxy_set_header Upgrade $http_upgrade;
                      proxy_set_header Connection $connection_upgrade;
                      proxy_cache off;
                      # WebSockets were implemented after http/1.0
                      proxy_http_version 1.1;

                      # Configuration for ServerSentEvents
                      proxy_buffering off;

                      # Configuration for LongPolling or if your KeepAliveInterval is longer than 60 seconds
                      proxy_read_timeout 100s;

                      proxy_set_header Host $host;
                      proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
                      proxy_set_header X-Forwarded-Proto $scheme;
                     }

    listen 443 ssl; # managed by Certbot
    ssl_certificate /etc/letsencrypt/live/dev-amendments.DOMAINNAME.com/fullchain.pem; # managed by Certbot
    ssl_certificate_key /etc/letsencrypt/live/dev-amendments.DOMAINNAME.com/privkey.pem; # managed by Certbot
    include /etc/letsencrypt/options-ssl-nginx.conf; # managed by Certbot
    ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem; # managed by Certbot


}




    server {
    if ($host = www.amendments.DOMAINNAME.com) {
        return 301 https://$host$request_uri;
    } # managed by Certbot


    if ($host = amendments.DOMAINNAME.com) {
        return 301 https://$host$request_uri;
    } # managed by Certbot


            listen 80;
            server_name amendments.DOMAINNAME.com www.amendments.DOMAINNAME.com;
    return 404; # managed by Certbot




}


    server {
    if ($host = www.dev-amendments.DOMAINNAME.com) {
        return 301 https://$host$request_uri;
    } # managed by Certbot


    if ($host = dev-amendments.DOMAINNAME.com) {
        return 301 https://$host$request_uri;
    } # managed by Certbot


            listen 80;
            server_name dev-amendments.DOMAINNAME.com www.dev-amendments.DOMAINNAME.com;
    return 404; # managed by Certbot




}}

```
