#!/bin/bash
setup=$HOME/setup.sh
base_folder=${HOME}/${STEAM_APP_DIR}/game/csgo
addons_folder=$base_folder/addons
gameinfo=$base_folder/gameinfo.gi
gameinfo_insert_line='          Game    csgo/addons/metamod'

install_metamod() {
  local version=${METAMOD_VERSION:-1401}
  local marker="$addons_folder/metamod_installed_$version"
  if [ ! -f "$marker" ]; then
    echo "Installing Metamod $version..."
    wget -q "https://github.com/alliedmodders/metamod-source/releases/download/2.0.0.${version}/mmsource-2.0.0-git${version}-linux.tar.gz" -O /tmp/metamod.tar.gz
    cd "$base_folder"
    tar -xzf /tmp/metamod.tar.gz
    rm /tmp/metamod.tar.gz
    touch "$marker"
    echo "Metamod $version installed."
  else
    echo "Metamod $version already installed."
  fi
}

install_css() {
  local version=${CSSAPI_VERSION:-1.0.367}
  local marker="$addons_folder/counterstrikesharp/css_installed_$version"
  if [ ! -f "$marker" ]; then
    echo "Installing CounterStrikeSharp $version..."
    wget -q "https://github.com/roflmuffin/CounterStrikeSharp/releases/download/v${version}/counterstrikesharp-with-runtime-linux-${version}.zip" -O /tmp/css.zip
    cd "$base_folder"
    unzip -o /tmp/css.zip
    rm /tmp/css.zip
    mkdir -p "$addons_folder/counterstrikesharp"
    touch "$marker"
    echo "CounterStrikeSharp $version installed."
  else
    echo "CounterStrikeSharp $version already installed."
  fi
}

install_pelipaja() {
  echo "Installing Pelipaja plugin..."
  mkdir -p "$addons_folder/counterstrikesharp/plugins/MatchUp"
  cp /tmp/MatchUp.dll "$addons_folder/counterstrikesharp/plugins/MatchUp/MatchUp.dll"
  echo "[Pelipaja] plugin installed."
}

update_gameinfo() {
  if ! grep -qF "csgo/addons/metamod" "$gameinfo"; then
    echo "updating gameinfo"
    sed -i "s:Game_LowViolence\tcsgo_lv // Perfect World content override:Game_LowViolence\tcsgo_lv // Perfect World content override\n$gameinfo_insert_line:" "$gameinfo"
  fi
}

if [ ! -z $1 ]; then
  $1
else
  $setup install_and_update
  install_metamod
  install_css
  install_pelipaja
  update_gameinfo
  exec $setup start
fi
