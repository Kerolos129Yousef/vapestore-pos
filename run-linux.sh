#!/usr/bin/env bash
#
# تشغيل البرنامج على لينكس عن طريق Mono.
# ملاحظة: البرنامج أصلاً مكتوب لويندوز (WinForms/.NET Framework). Mono بيشغّله على
# لينكس بشكل جيد، لكن ممكن تلاقي اختلافات بسيطة في الرسم (ده طبيعي). النسخة الرسمية
# المدعومة بالكامل هي ويندوز + Visual Studio.
#
set -e
cd "$(dirname "$0")"

# 1) التأكد إن Mono متثبّت واختيار أداة البناء (msbuild أو xbuild)
if ! command -v mono >/dev/null; then
  echo "لازم تثبّت Mono الأول:"
  echo "    sudo apt update && sudo apt install -y mono-complete libgdiplus nuget"
  exit 1
fi
if command -v msbuild >/dev/null; then BUILD=msbuild
elif command -v xbuild  >/dev/null; then BUILD=xbuild
else
  echo "مفيش أداة بناء. ثبّت: sudo apt install -y mono-complete"
  exit 1
fi
echo ">> using $BUILD"

# 2) استرجاع حزم NuGet
echo ">> nuget restore ..."
nuget restore VapeShopPos.sln

# 3) البناء كـ AnyCPU (عشان يشتغل 64-بت على لينكس ويطابق مكتبة SQLite للينكس)
echo ">> build ..."
$BUILD /p:Configuration=Release /p:Platform=x86 /p:PlatformTarget=AnyCPU \
       /p:Prefer32Bit=false /verbosity:minimal /nologo VapeShopPos.sln

OUT="VapeShopPos/bin/Release"

# 4) جلب مكتبة SQLite الأصلية (native) الخاصة بلينكس ووضعها جنب البرنامج
echo ">> fetching Linux SQLite interop ..."
TMP="$(mktemp -d)"
cat > "$TMP/packages.config" <<'EOF'
<packages>
  <package id="Stub.System.Data.SQLite.Core.NetStandard" version="1.0.118.0" targetFramework="net48" />
</packages>
EOF
( cd "$TMP" && nuget restore -PackagesDirectory pk >/dev/null )
cp "$TMP"/pk/Stub.System.Data.SQLite.Core.NetStandard.*/runtimes/linux-x64/native/SQLite.Interop.dll \
   "$OUT/SQLite.Interop.dll"
rm -rf "$TMP"

# 5) التشغيل
echo ">> launching ..."
cd "$OUT"
exec mono VapeShopPos.exe
