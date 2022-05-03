# COVID-19 Notification App - Backend

## Table of Contents
[Introduction](#Introduction)  
[External Documentation](#External-Documentation)  
[Development and Contribution Process](#Development-and-Contribution-Process)  
[Local Development Setup](#Local-Development-Setup)  
[Certificates](#Certificates)  
[Installation: Windows](#Installation%3A-Windows)  
[Installation: macOS](#Installation%3A-macOS)  
[Installation: Linux](#Installation%3A-Linux)  
[Database](#Database)  
[Windows](#Windows)  
[macOS/Linux](#macOS/Linux)  
[Docker (Windows/Linux/macOS)](#Docker-(Windows/Linux/macOS))  
[Docker (macOS M1)](#Docker-(macOS-M1))  
[Projects](#Projects)  
[DatabaseProvision](#DatabaseProvision)  
[DbBuilder](#DbBuilder)  
[GenTeks](#GenTeks)  
[ForceTekAuth](#ForceTekAuth)  
[PublishContent](#PublishContent)  
[SigtestFileCreator](#SigtestFileCreator)  
[ProtobufScrubber](#ProtobufScrubber)  
[EfgsTestDataGenerator](#EfgsTestDataGenerator)  
[Content.WebApi](#Content.WebApi)  
[DailyCleanup](#DailyCleanup)  
[EksEngine](#EksEngine)  
[ICC.V2.WebApi](#ICC.V2.WebApi)  
[Icc.WebApp](#Icc.WebApp)  
[Iks.Downloader](#Iks.Downloader)  
[Iks.Uploader](#Iks.Uploader)  
[ManifestEngine](#ManifestEngine)  
[MobileAppApi.WebApi](#MobileAppApi.WebApi)  
[License](#License)  
[Attribution](#Attribution)  


## Introduction

This repository contains the backend code for the Dutch exposure notification app.

- The backend is located in the repository you are currently viewing.
- The iOS app can be found here: https://github.com/minvws/nl-covid19-notification-app-ios
- The Android app can be found here: https://github.com/minvws/nl-covid19-notification-app-android
- The designs that are used as a basis to develop the apps can be found here: https://github.com/minvws/nl-covid19-notification-app-design
- The architecture that underpins the development can be found here: https://github.com/minvws/nl-covid19-notification-app-coordination

The backend code runs on .NET Core 3.1. End of support for this version of .NET is December 3rd, 2022.

## External Documentation

The Dutch exposure notification app uses the Google Apple Exposure Notification (GAEN) framework developed by Google and Apple as part of their effort to help combat the SARS-CoV-2 pandemic. Please find their documentation in one of the following 2 locations:

- [Google's Android Exposure Notifications Implementation Guide](https://developers.google.com/android/exposure-notifications/implementation-guide)
- [Apple's iOS Exposure Notification Documentation](https://developer.apple.com/documentation/exposurenotification)

The Dutch exposure notification app is part of the group of EU countries using the European Federation Gateway Service (EFGS) for sharing their national exposure keys on a European level. Please find the EFGS code and documentation on GitHub:

- [efgs-federation-gateway](https://github.com/eu-federation-gateway-service/efgs-federation-gateway)

## Development and Contribution Process

The core team works on the repository in a private fork for reasons of compliance with existing processes, and will share its work as often as possible.

If you plan to make code changes, please feel free to open an issue where we can discuss your changes, before opening a pull request. This avoids possibly doing work that we might not be able to use due to various reasons (specific infrastructure demands, already working on, etc).

If you think the information contained in this README is incomplete or wrong, please feel free to directly open a pull request on the README.

If you have any other questions about the README or the information contained therein, please feel free to open an issue.

## Local Development Setup

Before being able to run the projects contained in the backend solution, you will need to set up a database, and install a test certificate on the machine that will run the code.

### Certificates

CoronaMelder signs its files with an RSA-certificate and an ECDSA-certificate. The latter is a requirement set by Apple and Google.  
Versions of these certificates for local testing can be found in the folder `src/Crypto/Resources`:
- TestRSA.p12  
- TestECDSA.p12  
  
**Please note: these certificates are not production certificates.**  
The file-password for TestRSA.p12 is `Covid19!`; the password for TestECDSA.p12 is `12345678`.

The files `StaatDerNLChain-EV-Expires-2022-12-05.p7b` and `BdCertChain.p7b` can be ignored, as the local certificates are self-signed.  

#### Installation: Windows
Both certificates need to be installed into the local machine certificate store, under 'personal certificates'. Run `certlm.msc` to view this store.  

#### Installation: macOS  
For macOS the project assumes that the RSA certificate is installed in the *System* keychain. Please note that installing the certificate in the *System* keychain makes running the project locally slightly awkward, as it involves either giving the code permission to access this keychain permanently, or otherwise forces the developer to click "Allow" a large amount of times. To get around this, please make the following changes if you are running the backend on macOS:

1. Install the `TestRSA.p12` certificate in the *login* keychain.
2. Change `LocalMachineStoreCertificateProvider.cs` to read from `StoreLocation.CurrentUser` instead of `StoreLocation.LocalMachine`.

#### Installation: Linux  
TBD

### Database

This project assumes the presence of a Microsoft SQL Server database.

#### Windows/Linux

For local development on Windows or Linux, it would suffice to download [SQL Server Developer for Windows](https://www.microsoft.com/nl-nl/sql-server/sql-server-downloads) or follow the installation instructions for [SQL Server on Linux](https://docs.microsoft.com/en-us/sql/linux/sql-server-linux-setup).

After installing SQL Server, you can either create all the necessary databases and tables manually, or run the `DbProvision` project to have everything generated automatically.

#### macOS

For local development on macOS, local installation of SQL Server is not possible, and as such we have created a small Docker setup that contains a database to make developing locally on macOS possible. The Docker setup also contains `DbBuilder`, which serves the same function as the `DbProvision` project mentioned in the paragraph above. When combined together through `docker-compose`, you will end up with a database server populated with the necessary databases and tables running on Docker.

Of course you can also use the Docker setup on Linux or Windows if you do not want to install SQL Server on your machine.

### Docker (Windows/Linux/macOS)

To start the Docker database setup, you can use docker-compose:

```bash
# Solution root
cd docker
docker-compose up --build
```

This will create a Docker-based database server, as well as generate all the necessary databases and tables.

### Docker (macOS M1)

The Docker image used will currently not work out of the box for macOS machines with ARM architecture (macOS M1). To use the Docker setup on macOS M1, please make the following changes:

In `docker-compose.yml`, change

```
image: mcr.microsoft.com/mssql/server:2019-latest
```

to

```
image: mcr.microsoft.com/azure-sql-edge
```

The rest of the setup should work as-is.

### Projects
The codebase consists of the following projects, allowing you to locally set up a backend that contains Temporary Exposure Keys, Exposure Key Sets, a Manifest, and the various configuration JSON-files that are representative of the actual backend.  

#### DatabaseProvision
A console application that removes and rebuilds the required databases; only used for development and debugging.  
The `nonuke`-argument can be supplied to prevent removing any existing databases.  
Additionally, several types of JSON-files can be inserted into the database by means of passing one of the following arguments, followed by a path to the specific JSON-file:
- `-a`, for AppconfigV2.
- `-r`, for RiskCalculationParametersV2.
- `-b`, for ResourcebundleV2.
- `-b2`, for ResourcebundleV3.

#### DbBuilder
Identical to the DatabaseProvisioner, but only supports the `nonuke`-argument.

#### GenTeks
A console-application that generates Temporary Exposure Keys ('TEKs') and inserts them into the database. For development- and testing-purposes only.  
Two arguments can be passed to the application: the amount of workflows (or 'buckets') and the amount of TEKs per workflow.  
By default, 10 workflows with each 14 TEKs are created, equivalent to passing the arguments `10 14`.

#### ForceTekAuth
A console-application that authenticates all workflows in the database, equivalent to users contacting the GGD to publish their workflows.  
For development- and testing-purposes only; no command-line arguments can be provided.

#### PublishContent
A console-application that, equal to DatabaseProvision, allows the insertion of various JSON-files into the database.  
The files can be inserted into the database by means of passing one of the following arguments, followed by a path to a JSON-file that contains said content:
- `-a`, for AppconfigV2.
- `-r`, for RiskCalculationParametersV2.
- `-b`, for ResourcebundleV2.
- `-b2`, for ResourcebundleV3.

#### SigtestFileCreator
A console-application that is used to check if the private keys of the installed certificates can be accessed and used. For development- and testing-purposes only.  
The program has one command-line argument: the path to a file that will be signed with the RSA- and GAEN-key.

#### ProtobufScrubber
A console-application that alters the GAEN-signature of a signed file so that it can be verified by OpenSSL. The requirement of this program stems from a difference in writing the header bytes to the signature files. See `X962PackagingFix.cs` for more info.  
The program has one command-line argument: the path to a zip with an export.sig-file.

#### EfgsTestDataGenerator
A webservice that is used for automated end-to-end tests. It exposes one endpoint to mock the EFGS-service.  

#### Content.WebApi
A webservice for downloading the various files by the clients. It exposes endpoints that allows the user to download the manifests, exposure keysets, appconfig-files, resourcebundles and risk-calculation parameters.

#### DailyCleanup
A command-line program that removes old data from the database, in order to comply with privacy regulations.  
After the cleanup, it generates a set of statistics:  
- The total amount of TEKs.
- The amount of published TEKs.
- The total amount of Workflows.
- The total amount of Workflows with TEKs.
- The total amount of Workflows authorised.  
As the name suggests, the DailyCleanup is run every night at a set time.

#### EksEngine
The meat and potatoes of CoronaMelder. A console-application that takes all authorised workflows and downloaded TEKs from EFGS, bundles them into an EKS, signs it and places it in the databas. It then updates the manifest and prepares the TEKs from the authorised workflows for delivery to EFGS.

#### ICC.V2.WebApi
A webservice that exposes the PubTek-endpoint. This endpoint is used to authorise workflows with the GGD-key.

#### Icc.WebApp
A webservice that is used internally as a pass-through to the PubTek-endpoint.
It exposes the endpoints for the GGD telephone-operators that use the ICCportal-website.

#### Iks.Downloader
A console-application that downloads TEKs from EFGS and stores them to be processed by the EKSEngine.

#### Iks.Uploader
A console-application that takes the TEKs that the EKSEngine has prepared and uploads them to EFGS.

#### ManifestEngine
A console-application that updates the manifest with the most recent content of the databases.

#### MobileAppApi.WebApi
The webservice that the clients use to request the creation of a Workflow (or 'bucket'), upload their TEKs and perform decoy-uploads to preserve the privacy of the client.

## License

This project is licensed under the EUPL license. See [LICENSE](LICENSE/LICENSE.txt) for more information.

## Attribution

Some parts of this application are inspired by the work on [Private Tracer](https://gitlab.com/PrivateTracer/server.azure). You can find their license [here](LICENSE/LICENSE.PrivateTracer.org.txt).
