#!/usr/bin/env bash
set -euo pipefail

# Caminho padrão — já apontado para o que você pediu
DEFAULT_PATH="/home/filipe/GameRpg/GameClient"

DRY_RUN=0
TRASH=0
FORCE=0
TARGET="$DEFAULT_PATH"

usage() {
  cat <<EOF
Uso: $(basename "$0") [--dry-run|-n] [--trash] [--path PATH] [--yes|-y] [-h|--help]

  --dry-run, -n     : apenas lista os arquivos que seriam excluídos
  --trash           : envia para a lixeira (usa 'gio trash') em vez de remover
  --path PATH       : caminho base a buscar (padrão: $DEFAULT_PATH)
  --yes, -y         : sem confirmação interativa (força execução)
  -h, --help        : mostra esta ajuda
EOF
}

# Parse simples de argumentos
while [[ $# -gt 0 ]]; do
  case "$1" in
    --dry-run|-n) DRY_RUN=1; shift ;;
    --trash) TRASH=1; shift ;;
    --path) TARGET="$2"; shift 2 ;;
    --path=*) TARGET="${1#*=}"; shift ;;
    --yes|-y) FORCE=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) echo "Argumento desconhecido: $1"; usage; exit 1 ;;
  esac
done

# Verifica se pasta existe
if [[ ! -d "$TARGET" ]]; then
  echo "Erro: caminho não encontrado: $TARGET" >&2
  exit 1
fi

# Encontra arquivos
# Nota: usamos find com -print0 para lidar com nomes com espaços/newlines
mapfile -d '' files < <(find "$TARGET" -type f -name '*.cs.uid' -print0)

count=${#files[@]}
if [[ $count -eq 0 ]]; then
  echo "Nenhum arquivo com extensão '.cs.uid' encontrado em: $TARGET"
  exit 0
fi

# Dry-run: lista e sai
if [[ $DRY_RUN -eq 1 ]]; then
  echo "DRY-RUN: $count arquivo(s) encontrados (não serão removidos):"
  for f in "${files[@]}"; do printf '%s\n' "$f"; done
  exit 0
fi

# Se --trash foi solicitado, tenta usar gio trash
if [[ $TRASH -eq 1 ]]; then
  if ! command -v gio >/dev/null 2>&1; then
    echo "Comando 'gio' não encontrado — não é possível mover para a lixeira. Instale 'glib2'/'gio' ou rode sem --trash." >&2
    exit 1
  fi

  if [[ $FORCE -eq 0 ]]; then
    printf "Mover para a lixeira %d arquivo(s)? [y/N]: " "$count"
    read -r ans
    [[ "$ans" =~ ^[Yy]$ ]] || { echo "Operação abortada."; exit 0; }
  fi

  for f in "${files[@]}"; do
    gio trash "$f" && printf "-> Lixeira: %s\n" "$f"
  done

  echo "Concluído: $count arquivo(s) movidos para a lixeira."
  exit 0
fi

# Remoção definitiva (rm)
if [[ $FORCE -eq 0 ]]; then
  printf "Remover permanentemente %d arquivo(s)? [y/N]: " "$count"
  read -r ans
  [[ "$ans" =~ ^[Yy]$ ]] || { echo "Operação abortada."; exit 0; }
fi

# Executa remoção de forma segura com find + xargs -0
# (usa -print0 para nomes com caracteres estranhos)
find "$TARGET" -type f -name '*.cs.uid' -print0 | xargs -0 --no-run-if-empty rm -v --

echo "Concluído: arquivos removidos."
