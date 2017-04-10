#!/bin/bash
set -ev

nuget restore $SLN
xbuild $SLN /p:Configuation=Release
