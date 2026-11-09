#!/bin/bash
# Ejecuta Unity en batchmode sobre este proyecto.
#   Tools/unity.sh compile   -> importa y compila (errores "error CS" al final)
#   Tools/unity.sh scenes    -> RunDashC.EditorTools.SceneBuilder.BuildAll (genera las 4 escenas)
#   Tools/unity.sh test      -> PlayMode tests (resultado en Logs/test-results.xml)
# Solo puede correr un Unity a la vez por proyecto: usa un lock y aborta si el Editor lo tiene abierto.
set -u
cd "$(dirname "$0")/.." || exit 1
PROJECT="$(pwd)"
UNITY="${UNITY:-/Applications/Unity/Hub/Editor/6000.6.4f1/Unity.app/Contents/MacOS/Unity}"
CMD="${1:-}"
case "$CMD" in compile|scenes|test) ;; *) echo "uso: $0 compile|scenes|test"; exit 2 ;; esac
[ -x "$UNITY" ] || { echo "No encuentro Unity en $UNITY (define UNITY=...)"; exit 2; }

LOCK="${TMPDIR:-/tmp}/rundashc-unity.lock"
waited=0
until mkdir "$LOCK" 2>/dev/null; do
  [ $waited -eq 0 ] && echo "Esperando a que termine otro Unity batchmode..."
  sleep 5; waited=$((waited + 5))
  if [ $waited -ge 1200 ]; then echo "Timeout esperando el lock $LOCK (bórralo si quedó colgado)"; exit 4; fi
done
trap 'rmdir "$LOCK" 2>/dev/null' EXIT

if pgrep -fil "Unity.app/Contents/MacOS/Unity" | grep -i -- "$PROJECT" >/dev/null; then
  echo "Unity ya tiene abierto este proyecto. Cierra el Editor y vuelve a intentar."
  exit 3
fi

mkdir -p Logs
LOG="$PROJECT/Logs/batch-$CMD.log"
ARGS=(-batchmode -nographics -projectPath "$PROJECT" -logFile "$LOG")
case "$CMD" in
  compile) "$UNITY" "${ARGS[@]}" -quit ;;
  scenes)  "$UNITY" "${ARGS[@]}" -executeMethod RunDashC.EditorTools.SceneBuilder.BuildAll -quit ;;
  test)    rm -f "$PROJECT/Logs/test-results.xml"
           "$UNITY" "${ARGS[@]}" -runTests -testPlatform PlayMode -testResults "$PROJECT/Logs/test-results.xml" ;;
esac
CODE=$?

echo "---- resumen ($LOG) ----"
grep -E "error CS|[A-Za-z]+Exception|Scripts have compiler errors|\[RunDashC\]|License|licens" "$LOG" | grep -v "^ *at " | sort -u | head -80
if [ "$CMD" = test ] && [ -f "$PROJECT/Logs/test-results.xml" ]; then
  grep -o '<test-run[^>]*' "$PROJECT/Logs/test-results.xml" | grep -oE '(result|total|passed|failed)="[^"]*"' | tr '\n' ' '; echo
  grep -B1 -A6 'result="Failed"' "$PROJECT/Logs/test-results.xml" | grep -E "fullname=|<message>" | head -40
fi
echo "exit=$CODE"
exit $CODE
