#!/bin/bash
set -e

XWIN="$HOME/.local/share/xwin"
NETHOST="$HOME/.nuget/packages/microsoft.netcore.app.host.win-x86/10.0.11/runtimes/win-x86/native"
OUT_DIR="bin/Debug/net10.0"

dotnet publish SADXModLib.net.csproj -r win-x86 --self-contained false -p:PublishDir=bin/Debug/net10.0/dotnet/

clang++ --target=i686-pc-windows-msvc \
  -DNETHOST_USE_AS_STATIC \
  -fms-extensions -fms-compatibility \
  -isystem "$XWIN/crt/include" \
  -isystem "$XWIN/sdk/include/ucrt" \
  -isystem "$XWIN/sdk/include/um" \
  -isystem "$XWIN/sdk/include/shared" \
  -I"$NETHOST" \
  -O1 -fno-stack-protector -mno-stack-arg-probe \
  -c modlib.cpp -o modlib.o

clang++ --target=i686-pc-windows-msvc -fuse-ld=lld -shared \
  -Wl,/machine:x86 \
  -Wl,/libpath:"$XWIN/crt/lib/x86" \
  -Wl,/libpath:"$XWIN/sdk/lib/ucrt/x86" \
  -Wl,/libpath:"$XWIN/sdk/lib/um/x86" \
  -Wl,/libpath:"$NETHOST" \
  modlib.o -llibnethost -lkernel32 -luser32 -ladvapi32 -lshell32 \
  -o "$OUT_DIR/SADXModLib.Native.dll"

rm modlib.o
