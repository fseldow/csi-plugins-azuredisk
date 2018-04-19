#!/bin/bash

if [ ! -z "$CSI_ENDPOINT" ]; then
    rm $CSI_ENDPOINT
    socat -d UNIX-LISTEN:$CSI_ENDPOINT,fork TCP4:127.0.0.1:10000 2> /tmp/so.log &
fi

dotnet /opt/csi-azuredisk/Csi.Plugins.AzureDisk.dll
