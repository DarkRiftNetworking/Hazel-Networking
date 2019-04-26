# This version of Hazel is no longer supported! For the latest Hazel please see [this fork](https://github.com/willardf/Hazel-Networking)!

-----

#### Hazel Networking is a low-level networking library for C# providing connection orientated, message based communication via TCP, UDP and RUDP.

Its aim is to provide a standardized interface for web communication so that using and switching between protocols is incredibly simple.

Hazel can be downloaded as a NuGet package [here](https://www.nuget.org/packages/DarkRiftNetworking.Hazel/) or you can get the latest build directly from the releases page [here](/../../releases)!

-----

## Features
- TCP, UDP, Reliable UDP and (at some point) Web Sockets
- Completely thread safe
- All protocols are connection oriented (similar to TCP) and message based (similar to UDP)
- Standardised interface so that all protocols can be used interchangeably with each other
- IPv4 and IPv6 support
- Automatic statistics about data passing in and out of connections
- Designed to be as fast and leightweight as possible

-----

HTML documentation, tutorials and quickstarts are available on the DarkRift Website [here](http://www.darkriftnetworking.com/Hazel/Docs); support is available through [email](jamie@darkriftnetworking.com) or alternatively Hazel has a thread on the [Unity forum](http://forum.unity3d.com/threads/hazel-networking-open-source-rudp-tcp-library.409863/) where you'll also find updates and other news.

If you want to make improvements, do so! If you find bugs, raise issues!

-----

## Building Hazel

To build Hazel open [solution file](Hazel.sln) using your favourite C# IDE (I use Visual Studio 2015) and then build as you would any other project.
