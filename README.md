sensu-client.net
================

An implementation of the sensu client in .NET for those that don't want to drag around a fully Ruby runtime on Windows. I'm not claiming this to be fully featured, but should support some task execution and simple checks.

Installation
============

The MSI will install a service called 'Sensu Client' and application into `%PROGRAMFILES%`. It provides a sample sensu-compatible json-based config file in the installation directory. Sensu-client.net will then log to `%PROGRAMDATA%\sensu-client.net\logs`.

Download
========

Releases
https://github.com/carllindelof/sensu-client/releases

Start running
=============


1. You need to have Ruby version > 1.9 installed.
2. You need to install sensu-plugin gem.

`gem install sensu-plugin --no-rdoc --no-ri` 
 
In the tools directory there is a powershell script (Import-Certificate.ps1) to install the cert if your are running RabbitMQ with certificates

`
Import-Certificate -CertFile "C:\pathtoyourcert\client_keycert.p12" -StoreNames My -LocalMachine -CurrentUser -CertPassword DemoPass -Verbose`

Development
===========
You need to install WiX Toolset 3.8 is released with official support for Visual Studio 2013 editions. It is available for download from http://wixtoolset.org.
