#!/bin/bash
set -ev

nuget restore $SLN

xbuild /p:Configuration=$VERSION $SLN

mono ./testrunner/NUnit.ConsoleRunner.3.5.0/tools/nunit3-console.exe ./CoAP.Test/bin/$VERSION/$TARGET/CoAP.Test.dll
