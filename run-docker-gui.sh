#!/usr/bin/env bash
#
# تشغيل البرنامج على لينكس بعربي سليم، من جوه كونتينر فيه libgdiplus بتعمل
# تشكيل/ربط صحيح للحروف العربية. النافذة بتظهر على سطح مكتبك مباشرةً.
#
set -e
cd "$(dirname "$0")"

# 1) نتأكد إن البرنامج متبني
if [ ! -f VapeShopPos/bin/Release/VapeShopPos.exe ]; then
  echo ">> البرنامج مش متبني، بشغّل البناء الأول ..."
  nuget restore VapeShopPos.sln
  if command -v msbuild >/dev/null; then BUILD=msbuild; else BUILD=xbuild; fi
  "$BUILD" /p:Configuration=Release /p:Platform=x86 /p:PlatformTarget=AnyCPU \
           /p:Prefer32Bit=false /verbosity:minimal /nologo VapeShopPos.sln
fi

# 2) نتأكد إن الصورة موجودة
if ! docker image inspect vapepos-gui >/dev/null 2>&1; then
  echo ">> ببني صورة الكونتينر (مرة واحدة) ..."
  docker build -f Dockerfile.gui -t vapepos-gui .
fi

# 3) نسمح للكونتينر يستخدم شاشة X بتاعتك
xhost +local: >/dev/null 2>&1 || true

echo ">> تشغيل البرنامج ... (الدخول: admin / admin123)"
docker run --rm \
  --user "$(id -u):$(id -g)" \
  -e DISPLAY="$DISPLAY" \
  -e HOME=/tmp \
  -v /tmp/.X11-unix:/tmp/.X11-unix \
  -v "$PWD":/app \
  vapepos-gui || true

# 4) نرجّع إعدادات الأمان زي ما كانت
xhost -local: >/dev/null 2>&1 || true
echo ">> خلصنا."
