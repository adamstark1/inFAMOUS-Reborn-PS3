#!/bin/bash

cd "$(dirname "$0")/.."

VERSION="1.0.3"
OUT_DIR="inFAMOUS Reborn Linux"

echo "Compiling Linux build for version $VERSION..."

rm -rf "$OUT_DIR" temp_backend temp_launcher
mkdir -p "$OUT_DIR"

dotnet publish inFAMOUSReborn.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:UseAppHost=true -p:Version=$VERSION -o temp_backend
dotnet publish inFAMOUSReborn.Launcher/inFAMOUSReborn.Launcher.csproj -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -p:UseAppHost=true -p:Version=$VERSION -o temp_launcher

rm -rf temp_backend/inFAMOUSReborn.Launcher

cp -rf temp_backend/* "$OUT_DIR/"
cp -rf temp_launcher/* "$OUT_DIR/"

rm -rf temp_backend temp_launcher

mv "$OUT_DIR/inFAMOUSReborn.Launcher" "$OUT_DIR/inFAMOUS Reborn Launcher"

cp infamous.pfx "$OUT_DIR/"

chmod +x "$OUT_DIR/inFAMOUSReborn"
chmod +x "$OUT_DIR/inFAMOUS Reborn Launcher"

if [ -f "Assets/icon.png" ]; then
    cp Assets/icon.png "$OUT_DIR/icon.png"
    ICON_NAME="icon.png"
else
    cp Assets/icon.ico "$OUT_DIR/icon.ico"
    ICON_NAME="icon.ico"
fi

cat > "$OUT_DIR/install.sh" << EOF
#!/bin/bash

DIR="\$(cd "\$(dirname "\${BASH_SOURCE[0]}")" >/dev/null 2>&1 && pwd)"

echo "Configuring network ports for inFAMOUS Reborn..."
echo "Please enter your password to allow the server to use ports 53 and 80:"
sudo setcap 'cap_net_bind_service=+ep' "\$DIR/inFAMOUSReborn"
sudo setcap 'cap_net_bind_service=+ep' "\$DIR/inFAMOUS Reborn Launcher"

mkdir -p ~/.local/share/applications

cat > ~/.local/share/applications/infamous-reborn.desktop << INNEREF
[Desktop Entry]
Type=Application
Version=1.0
Name=inFAMOUS Reborn
Exec="\$DIR/inFAMOUS Reborn Launcher"
Icon=\$DIR/$ICON_NAME
Terminal=false
Categories=Game;Utility;
StartupWMClass=inFAMOUSReborn.Launcher
INNEREF

chmod +x ~/.local/share/applications/infamous-reborn.desktop

echo "Installation complete! You can now launch inFAMOUS Reborn from your application menu."
EOF

chmod +x "$OUT_DIR/install.sh"

echo "Linux build compiled successfully. Run install.sh in the output directory to configure permissions and shortcuts."
