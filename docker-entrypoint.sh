#!/bin/bash
# Runs the app and the sync process in one container. If one of them stops, the other one is stopped
# too and the container exits, so the restart policy brings both back.

# each process needs its own folder as working directory (appsettings.json, wwwroot)
(cd /app/rest && exec dotnet ProPair.Adapters.Rest.dll) &
(cd /app/sync && exec dotnet ProPair.Adapters.Sync.dll --urls "$SYNC_URLS") &

stop_all() { kill -TERM $(jobs -pr) 2>/dev/null; }

# docker stop
trap 'stop_all; wait; exit 0' TERM INT

wait -n
status=$?
stop_all
wait
exit $status
