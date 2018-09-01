#!/bin/bash
set -ev
mono nuget.exe restore $SLN

msbuild /p:Configuration=$VERSION $SLN

mono ./testrunner/NUnit.ConsoleRunner.3.8.0/tools/nunit3-console.exe ./CoAP.Test/bin/$VERSION/$TARGET/CoAP.Test.dll
