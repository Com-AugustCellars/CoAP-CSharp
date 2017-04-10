#!/bin/sh

nuget restore $SLN
xbuild $SLN /p:Configuation=Release
