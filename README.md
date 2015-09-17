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
==================

It is easy to extend sensu on windows using powershell i have some helper methods
and some examples published on github: https://github.com/carllindelof/sensu-checks

Configuration
=============

In order to configure it, it requires the file `c:\etc\sensu\conf.json`, which looks like:

```
{
  "rabbitmq": {
    "host": "localhost",
    "port": 5672,
    "vhost": "/",
    "user": "guest",
    "password": "guest"
  }
}
```

And the checks are stored in `c:\etc\sensu\conf.d directory`. The most important file is the `client.json`, which should look like:

```
{
  "client": {
    "name": "my_hostname",
    "address": "10.0.0.1",
    "bind": "localhost",
    "subscriptions": ["windows"],
    "safe_mode": false,
    "keepalive": {
    },
    "plugins": "c:\\opt\\sensu-plugins"
  }
}
```

Special Configuration
---------------------

It supports standard Sensu keywords, but has some extra keys:

* `plugins`: The path where plugins are going to be run. It *should exist*.
* `send_metric_with_check`: Sends a metric in addition to checks. This is useful to track metrics and send alerts in one step.

Special commands
----------------

As long as this client can only run on Windows, it has several special features:

- PowerShell is becoming the standard Windows administration shell scripting language, so just setting a `.ps1` file it will be run with a powershell interpreter.
- Same happens with ruby scripts, which filename should end with `.rb`.

In addition, Windows allows a system similar to Sensu called "Performance Counters". These are small programs with a common interface. In order to check them, you can use the special command `!perfcounter> `. Example:

```
{
  "checks": {
    "Windows_CPU": {
      "command": "!perfcounter> processor(_total)\\% processor time; warn=80; critical=90; schema=foo.bar",
      "type": "standard",
      "standalone": true,
      "handlers": [
        "default"
      ],
    }
  }
}
```

You can get the complete list of *Performance Counters* with the command (remember that backslashes require to be duplicated in the JSON file):

```
typeperf -q
```

This feature will return the information in a format Graphite-compatible, and will fail depending on the `warn` and `critical` arguments. The `schema` allows to set exactly the key to be used, so output format is:

```
# errors if any
schema value unix_timestamp
```

Arguments for `!perfcounter>` command are:

- `warn`: the limit for warnings.
- `critical`: the limmit for errors.
- `growth`: can be `asc` (the default), if should alert with values over the limits or `desc` if should alert with values below the limits.
- `schema`: the stats name. Should be Graphite-compliant.

The `schema` admits different variables, such as:
- `{INSTANCE}`, which will be replaced by the Performance Counter instance name.
- `{COUNTER}`, which will be replaced by the Performance Counter name.
- `{CATEGORY}`, which will be replaced by the Performance Counter category.

They will be normalized in order to avoid names with non-letters or numbers.

Finally, you can use the asterisk `*` to configure **any** instance:

```
{
  "checks": {
    "Windows_CPU": {
      "command": "!perfcounter> processor(*)\\% processor time; warn=80; critical=90; schema=processor.{INSTANCE}.time",
      "type": "standard",
      "standalone": true,
      "handlers": [
        "default"
      ],
    }
  }
}
```

In this case, it will fail if **any** processor passes the limits.