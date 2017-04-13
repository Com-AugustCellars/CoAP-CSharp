#!/bin/bash
set -ev

nuget restore $SLN

xbuild /p:Configuation="Release|Mixed Platforms" $SLN
