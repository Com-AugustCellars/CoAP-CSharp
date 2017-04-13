#!/bin/bash
set -ev

nuget restore $SLN
ls packages
ls packages/Com.AugustCellars.COSE.0.1.0
ls packages/Com.AugustCellars.COSE.0.1.0/lib
ls packages/Com.AugustCellars.COSE.0.1.0/lib/portable-net40+s15+win+wpa81+wp8


# xbuild $SLN /p:Configuation=Release
