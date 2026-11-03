#!/usr/bin/env bash
# ARCH-1 P6: builds the Android harness (Release, both ABIs) and, with a device or emulator attached, installs it,
# starts it and prints its logcat lines (tag P6) until "P6 done".
# Usage: build-android.sh <native-out-dir> <b2net-project> <out-dir> [adb-serial]
#   <native-out-dir>  build-native.sh's <work-dir>/out (android-x64-box2d-v3.2.0/, android-arm64-box2d-v3.2.0/)
# bin/obj go to <out-dir>/artifacts, never into the repository.
set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
lib=${1:?native out dir}; b2net=${2:?Box2D.NET project}; out=${3:?output dir}; serial=${4:-}
case "$(uname -s)" in MINGW*|MSYS*|CYGWIN*) w() { cygpath -w "$1"; } ;; *) w() { echo "$1"; } ;; esac
mkdir -p "$out"
dotnet publish "$(w "$here/Android/Android.csproj")" -c Release -nologo -v q --artifacts-path "$(w "$out/artifacts")" \
  -p:Box2DAndroidDir="$(w "$lib")" -p:B2NetProject="$(w "$b2net")" -o "$(w "$out/apk")" > "$out/build.log" 2>&1 \
  || { tail -30 "$out/build.log"; exit 1; }
apk=$(ls "$out/apk/"*-Signed.apk | head -1)
ls -l "$apk"
[ -n "$serial" ] || exit 0
adb -s "$serial" install -r "$(w "$apk")"
adb -s "$serial" logcat -c
adb -s "$serial" shell monkey -p org.microengine.experiments.p6box2d -c android.intent.category.LAUNCHER 1 > /dev/null
for _ in $(seq 1 180); do
  if adb -s "$serial" logcat -d -s P6:I | grep -q "P6 done\|P6 error"; then break; fi
  sleep 2
done
adb -s "$serial" logcat -d -s P6:I | sed 's/^.*P6 *: //'
