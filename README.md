sensu-client
================

An implementation of the sensu client in .NET for those that don't want to drag around a fully Ruby runtime on Windows. 
It should be fully featured, but the test-case we use for production is regular checks pushed from sensu server.

Sensu is a open source monitoring framework
============
Information om Sensu http://sensuapp.org/
Sensu on github: https://github.com/sensu/sensu

Joe Miller writes som really helpful blogposts
http://www.joemiller.me/2012/01/19/getting-started-with-the-sensu-monitoring-framework/
http://www.joemiller.me/2013/12/07/sensu-and-graphite-part-2/

Installation
============

The MSI will install a service called 'Sensu Client' and application into `%PROGRAMFILES%`. It provides a sample sensu-compatible json-based config file in the installation directory. Sensu-client will then log to `%PROGRAMDATA%\sensu-client\logs`.

Download
========

Current version 1.0.0:
https://github.com/carllindelof/sensu-client/releases/download/v1.0.0/sensu-client.msi

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
You need to install WiX Toolset 3.9 is released with official support for Visual Studio 2013 editions. It is available for download from http://wixtoolset.org.

Working with sensu
==========

It is easy to extend sensu on windows using powershell i have some helper methods
and some examples published on github: https://github.com/carllindelof/sensu-checks

