#!/bin/bash
set -ev

nuget restore $SLN

xbuild /p:Configuation="Release|Mixed Platforms" $SLN

mono ./testrunner/NUnit.Runners.3.5.0/tools/nunit-console.exe ./CoAP.Test/bin/Release/CoAP.Test.dll
