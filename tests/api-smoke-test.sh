#!/usr/bin/env bash
# ============================================================
# DM Admin API — Smoke test completo
# Uso: bash tests/api-smoke-test.sh [base_url]
# ============================================================
BASE="${1:-http://localhost:5226}"
API="$BASE/api"
PASS=0; FAIL=0; SKIPPED=0
TMPFILE=$(mktemp)
trap "rm -f $TMPFILE" EXIT

GREEN='\033[0;32m'; RED='\033[0;31m'; YELLOW='\033[1;33m'; GRAY='\033[0;90m'; NC='\033[0m'; BOLD='\033[1m'
ok()      { echo -e "  ${GREEN}✓${NC} $1"; PASS=$((PASS+1)); }
fail()    { echo -e "  ${RED}✗${NC} $1 — $2"; FAIL=$((FAIL+1)); }
skip()    { echo -e "  ${GRAY}⊘${NC} $1 (skipped — dependencia fallida)"; SKIPPED=$((SKIPPED+1)); }
section() { echo -e "\n${BOLD}${YELLOW}▶ $1${NC}"; }

# Single-call HTTP request — returns body\n|||HTTP_CODE
req() {
  local method="$1" url="$2"; shift 2
  local code
  code=$(curl -s -X "$method" "$url" \
    -H "Content-Type: application/json" \
    "$@" -o "$TMPFILE" -w "%{http_code}" 2>/dev/null)
  echo "$(cat "$TMPFILE")|||${code}"
}

body() { echo "${1%|||*}"; }
code() { local c="${1##*||||}"; echo "${c// /}"; }
code() { printf '%s' "${1}" | tail -c 3; }

# JSON field extractor
jget() {
  echo "$1" | python -c "
import sys,json
try:
  d=json.loads(sys.stdin.read())
  v=d$2
  print(str(v).lower() if isinstance(v,bool) else v)
except: print('')
" 2>/dev/null || true
}

check() {
  local label="$1" expected="$2" resp="$3"
  local c; c=$(code "$resp")
  if [ "$c" = "$expected" ]; then
    ok "$label (HTTP $c)"
    return 0
  else
    fail "$label" "esperado $expected, got $c — $(body "$resp" | head -c 300)"
    return 1
  fi
}

# ============================================================
section "0. Conectividad"
# ============================================================
C=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/scalar/v1" 2>/dev/null || echo "000")
if [ "$C" != "000" ] && [ "$C" != "" ]; then
  ok "API alcanzable en $BASE (HTTP $C)"
else
  fail "API alcanzable" "No se pudo conectar a $BASE"
  echo -e "\n${RED}Abortando.${NC}"; exit 1
fi

# ============================================================
section "1. Auth"
# ============================================================
TS=$(date +%s)
DM_EMAIL="dm_smoke_${TS}@test.com"
PL_EMAIL="pl_smoke_${TS}@test.com"
PWD="Test@1234!"

R=$(req POST "$API/auth/register" -d "{\"email\":\"$DM_EMAIL\",\"password\":\"$PWD\",\"displayName\":\"DM Smoke\"}")
check "Registro DM" "201" "$R"
DM_TOKEN=$(jget "$(body "$R")" "['accessToken']")

R=$(req POST "$API/auth/register" -d "{\"email\":\"$PL_EMAIL\",\"password\":\"$PWD\",\"displayName\":\"Player Smoke\"}")
check "Registro Player" "201" "$R"
PL_TOKEN=$(jget "$(body "$R")" "['accessToken']")

# Registro duplicado debe fallar
R=$(req POST "$API/auth/register" -d "{\"email\":\"$DM_EMAIL\",\"password\":\"$PWD\",\"displayName\":\"DM Smoke\"}")
C=$(code "$R"); [ "$C" = "409" ] && ok "Email duplicado → 409" || fail "Email duplicado" "esperado 409, got $C"

# Login
R=$(req POST "$API/auth/login" -d "{\"email\":\"$DM_EMAIL\",\"password\":\"$PWD\"}")
check "Login DM" "200" "$R" && DM_TOKEN=$(jget "$(body "$R")" "['accessToken']")

R=$(req POST "$API/auth/login" -d "{\"email\":\"$PL_EMAIL\",\"password\":\"$PWD\"}")
check "Login Player" "200" "$R" && PL_TOKEN=$(jget "$(body "$R")" "['accessToken']")

# Login con contraseña incorrecta
R=$(req POST "$API/auth/login" -d "{\"email\":\"$DM_EMAIL\",\"password\":\"Wrong!\"}")
C=$(code "$R"); [ "$C" = "401" ] && ok "Password incorrecta → 401" || fail "Auth password" "esperado 401, got $C"

if [ -z "$DM_TOKEN" ] || [ -z "$PL_TOKEN" ]; then
  fail "Tokens JWT" "No se obtuvieron — abortando"; exit 1
fi

# ============================================================
section "2. Campaigns"
# ============================================================
R=$(req POST "$API/campaigns" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Smoke Campaign","description":"Test"}')
check "Crear campaña" "201" "$R" || { fail "Campaña requerida — abortando"; exit 1; }
CAMPAIGN_ID=$(jget "$(body "$R")" "['id']")
JOIN_CODE=$(jget "$(body "$R")" "['joinCode']")

R=$(req GET "$API/campaigns" -H "Authorization: Bearer $DM_TOKEN")
check "Listar campañas" "200" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID" -H "Authorization: Bearer $DM_TOKEN")
check "Detalle campaña" "200" "$R"

R=$(req POST "$API/campaigns/join-by-code" \
  -H "Authorization: Bearer $PL_TOKEN" \
  -d "{\"code\":\"$JOIN_CODE\"}")
check "Player se une con código" "200" "$R"

# Unirse dos veces debe fallar
R=$(req POST "$API/campaigns/join-by-code" \
  -H "Authorization: Bearer $PL_TOKEN" \
  -d "{\"code\":\"$JOIN_CODE\"}")
C=$(code "$R"); [ "$C" = "400" ] && ok "Unirse dos veces → 400" || fail "Join duplicado" "esperado 400, got $C"

# ============================================================
section "3. Entity Types & Fields"
# ============================================================
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/entity-types" -H "Authorization: Bearer $DM_TOKEN")
check "Listar entity types (con tipos sistema)" "200" "$R"
PERSONAJE_TYPE_ID=$(body "$R" | python -c "
import sys,json
try:
  data=json.loads(sys.stdin.read())
  [print(t['id']) for t in data if t.get('name')=='Personaje']
except: pass
" 2>/dev/null | head -1 || true)
[ -n "$PERSONAJE_TYPE_ID" ] && ok "Tipo 'Personaje' encontrado" || fail "Tipo Personaje" "no encontrado en la lista"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entity-types" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Dios","icon":"star","color":"#FFD700"}')
check "Crear entity type custom" "201" "$R"
CUSTOM_TYPE_ID=$(jget "$(body "$R")" "['id']")

# Agregar campo isRollFormula=true
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entity-types/$CUSTOM_TYPE_ID/fields" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Poder","fieldType":"number","isRequired":false,"sortOrder":0,"isRollFormula":true}')
check "Agregar campo isRollFormula=true" "201" "$R"
FIELD_ID=$(jget "$(body "$R")" "['id']")
IS_ROLL=$(jget "$(body "$R")" "['isRollFormula']")
[ "$IS_ROLL" = "true" ] && ok "isRollFormula=true en DTO" || fail "isRollFormula" "got '$IS_ROLL'"

# Actualizar campo isRollFormula=false
R=$(req PUT "$API/campaigns/$CAMPAIGN_ID/entity-types/$CUSTOM_TYPE_ID/fields/$FIELD_ID" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Poder","fieldType":"number","isRequired":false,"sortOrder":0,"isRollFormula":false}')
check "Actualizar campo isRollFormula=false" "200" "$R"
IS_ROLL=$(jget "$(body "$R")" "['isRollFormula']")
[ "$IS_ROLL" = "false" ] && ok "isRollFormula=false tras update" || fail "isRollFormula update" "got '$IS_ROLL'"

# Player no puede crear entity types
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entity-types" \
  -H "Authorization: Bearer $PL_TOKEN" \
  -d '{"name":"Intruso","icon":"bug","color":"#000"}')
C=$(code "$R"); [ "$C" = "403" ] && ok "Player bloqueado de crear entity types (403)" || fail "Auth entity type" "esperado 403, got $C"

# ============================================================
section "4. Entities"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entities" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d "{\"name\":\"Gandalf\",\"entityTypeId\":\"$PERSONAJE_TYPE_ID\",\"customFields\":{}}")
check "Crear entidad Personaje" "201" "$R"
ENTITY_ID=$(jget "$(body "$R")" "['id']")

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/entities" -H "Authorization: Bearer $DM_TOKEN")
check "Listar entidades" "200" "$R"
COUNT=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "0")
[ "$COUNT" -ge 1 ] && ok "$COUNT entidad(es) en la campaña" || fail "Entities count" "esperado >=1, got $COUNT"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID" -H "Authorization: Bearer $DM_TOKEN")
check "Detalle entidad" "200" "$R"
TYPE_NAME=$(jget "$(body "$R")" "['entityTypeName']")
[ "$TYPE_NAME" = "Personaje" ] && ok "entityTypeName = Personaje" || fail "entityTypeName" "got '$TYPE_NAME'"

# ============================================================
section "5. CharacterResources (Ficha)"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"HP","current":30,"max":30,"color":"#e53935","sortOrder":0}')
check "Crear recurso HP" "201" "$R"
RESOURCE_ID=$(jget "$(body "$R")" "['id']")

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Mana","current":20,"max":20,"color":"#1565c0","sortOrder":1}')
check "Crear recurso Mana" "201" "$R"
MANA_ID=$(jget "$(body "$R")" "['id']")

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Listar recursos (2)" "200" "$R"
RCOUNT=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "0")
[ "$RCOUNT" = "2" ] && ok "2 recursos creados" || fail "Resources count" "esperado 2, got $RCOUNT"

# PATCH value
R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"current":25}')
check "Actualizar HP=25" "200" "$R"
[ "$(jget "$(body "$R")" "['current']")" = "25" ] && ok "HP = 25 correcto" || fail "HP value" "got $(jget "$(body "$R")" "['current']")"

# Clamp al max
R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"current":999}')
check "Clamp sobre max (999→30)" "200" "$R"
[ "$(jget "$(body "$R")" "['current']")" = "30" ] && ok "Clamp max funciona" || fail "Clamp max" "got $(jget "$(body "$R")" "['current']")"

# Clamp a 0
R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"current":-99}')
check "Clamp bajo cero (-99→0)" "200" "$R"
[ "$(jget "$(body "$R")" "['current']")" = "0" ] && ok "Clamp min funciona" || fail "Clamp min" "got $(jget "$(body "$R")" "['current']")"

# Eliminar recurso Mana
R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$MANA_ID" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar recurso Mana" "204" "$R"

# ============================================================
section "6. Sessions"
# ============================================================

# Terminar sesión activa existente si la hay
EXISTING=$(curl -s "$API/campaigns/$CAMPAIGN_ID/sessions" \
  -H "Authorization: Bearer $DM_TOKEN" 2>/dev/null)
EXISTING_SID=$(python -c "
import sys,json
try:
  data=json.loads('''$EXISTING''')
  active=[s for s in data if s.get('status')=='active']
  if active: print(active[0]['id'])
except: pass
" 2>/dev/null || true)
if [ -n "$EXISTING_SID" ]; then
  curl -s -X POST "$API/campaigns/$CAMPAIGN_ID/sessions/$EXISTING_SID/end" \
    -H "Authorization: Bearer $DM_TOKEN" -o /dev/null 2>/dev/null || true
  echo -e "  ${GRAY}(sesión activa previa terminada)${NC}"
fi

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Sesion de prueba"}')
check "Crear sesión" "201" "$R"
SESSION_ID=$(jget "$(body "$R")" "['id']")

if [ -z "$SESSION_ID" ]; then
  fail "SESSION_ID vacío — saltando secciones 7-11"
  SKIPPED=$((SKIPPED+50)); exit 1
fi

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions" -H "Authorization: Bearer $DM_TOKEN")
check "Listar sesiones" "200" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID" -H "Authorization: Bearer $DM_TOKEN")
check "Detalle sesión" "200" "$R"

# ============================================================
section "7. Escenas y Tokens"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Taberna","gridSize":50,"gridEnabled":true}')
check "Crear escena" "201" "$R"
SCENE_ID=$(jget "$(body "$R")" "['id']")
FOG=$(jget "$(body "$R")" "['fogEnabled']")
[ "$FOG" = "false" ] && ok "fogEnabled=false por defecto" || fail "fogEnabled default" "got '$FOG'"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"name":"Dungeon","gridSize":60,"gridEnabled":false}')
check "Crear segunda escena" "201" "$R"
SCENE2_ID=$(jget "$(body "$R")" "['id']")

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/activate" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Activar escena 1" "200" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes" \
  -H "Authorization: Bearer $PL_TOKEN")
check "Player puede listar escenas" "200" "$R"

# Añadir token
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d "{\"label\":\"Gandalf\",\"color\":\"#3F51B5\",\"x\":2,\"y\":3,\"width\":1,\"height\":1,\"entityId\":\"$ENTITY_ID\"}")
check "Añadir token" "201" "$R"
TOKEN_ID=$(jget "$(body "$R")" "['id']")
HAS_CONDS=$(jget "$(body "$R")" "['conditions']")
[ -n "$HAS_CONDS" ] && ok "Token tiene campo 'conditions'" || fail "conditions en nuevo token" "campo ausente"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Listar tokens" "200" "$R"
TCOUNT=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "0")
[ "$TCOUNT" = "1" ] && ok "1 token en escena" || fail "Token count" "esperado 1, got $TCOUNT"

# Mover token (DM)
R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/move" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"x":5,"y":5}')
check "Mover token (DM)" "200" "$R"
CONDS=$(jget "$(body "$R")" "['conditions']")
[ -n "$CONDS" ] && ok "conditions presente tras mover" || fail "conditions tras move" "campo ausente"

# Player no puede mover token ajeno
R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/move" \
  -H "Authorization: Bearer $PL_TOKEN" -d '{"x":1,"y":1}')
C=$(code "$R"); [ "$C" = "403" ] && ok "Player bloqueado de mover token ajeno (403)" || fail "Auth move" "esperado 403, got $C"

# Actualizar token
R=$(req PUT "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"label":"Gandalf el Gris","color":"#9C27B0","width":2,"height":2,"isVisible":false,"controlledBy":null}')
check "Actualizar token (invisible, 2x2)" "200" "$R"
IS_VIS=$(jget "$(body "$R")" "['isVisible']")
[ "$IS_VIS" = "false" ] && ok "isVisible=false correcto" || fail "isVisible" "got '$IS_VIS'"
CONDS=$(jget "$(body "$R")" "['conditions']")
[ -n "$CONDS" ] && ok "conditions presente tras update" || fail "conditions tras update" "campo ausente"

# ============================================================
section "8. Fog of War"
# ============================================================
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Listar fog zones inicial (vacío)" "200" "$R"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"shape":"rect","x":0,"y":0,"width":200,"height":200}')
check "Añadir fog zone (rect)" "201" "$R"
FOG_RECT_ID=$(jget "$(body "$R")" "['id']")
[ "$(jget "$(body "$R")" "['shape']")" = "rect" ] && ok "shape=rect correcto" || fail "shape fog zone" "got $(jget "$(body "$R")" "['shape']")"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d '{"shape":"circle","x":300,"y":300,"radius":100}')
check "Añadir fog zone (circle)" "201" "$R"
FOG_CIRCLE_ID=$(jget "$(body "$R")" "['id']")

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Listar fog zones (2)" "200" "$R"
FCOUNT=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "0")
[ "$FCOUNT" = "2" ] && ok "2 fog zones correctas" || fail "Fog count" "esperado 2, got $FCOUNT"

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/toggle" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"fogEnabled":true}')
check "Activar fog" "200" "$R"
[ "$(jget "$(body "$R")" "['fogEnabled']")" = "true" ] && ok "fogEnabled=true confirmado" || fail "fog toggle" "got $(jget "$(body "$R")" "['fogEnabled']")"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones/$FOG_CIRCLE_ID" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar fog zone (circle)" "204" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog" \
  -H "Authorization: Bearer $DM_TOKEN")
FCOUNT=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "0")
[ "$FCOUNT" = "1" ] && ok "Queda 1 fog zone tras eliminar" || fail "Fog count tras delete" "esperado 1, got $FCOUNT"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Limpiar todas las fog zones" "204" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog" \
  -H "Authorization: Bearer $DM_TOKEN")
FCOUNT=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "0")
[ "$FCOUNT" = "0" ] && ok "Fog zones limpiadas (0)" || fail "Fog clear" "esperado 0, got $FCOUNT"

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/toggle" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"fogEnabled":false}')
check "Desactivar fog" "200" "$R"

# Player bloqueado
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" \
  -H "Authorization: Bearer $PL_TOKEN" \
  -d '{"shape":"rect","x":0,"y":0,"width":100,"height":100}')
C=$(code "$R"); [ "$C" = "403" ] && ok "Player bloqueado de crear fog (403)" || fail "Auth fog player" "esperado 403, got $C"

# ============================================================
section "9. Initiative Tracker"
# ============================================================
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Listar iniciativa vacía" "200" "$R"
ICOUNT=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "0")
[ "$ICOUNT" = "0" ] && ok "0 entradas al inicio" || fail "Iniciativa inicial" "esperado 0, got $ICOUNT"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"name":"Gandalf","initiative":20}')
check "Añadir Gandalf (20)" "201" "$R"
INIT_GANDALF=$(body "$R" | python -c "
import sys,json
data=json.loads(sys.stdin.read())
[print(e['id']) for e in data if e['name']=='Gandalf']
" 2>/dev/null | head -1 || true)

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"name":"Goblin","initiative":15}')
check "Añadir Goblin (15)" "201" "$R"
INIT_GOBLIN=$(body "$R" | python -c "
import sys,json
data=json.loads(sys.stdin.read())
[print(e['id']) for e in data if e['name']=='Goblin']
" 2>/dev/null | head -1 || true)

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"name":"Balrog","initiative":25}')
check "Añadir Balrog (25)" "201" "$R"
INIT_BALROG=$(body "$R" | python -c "
import sys,json
data=json.loads(sys.stdin.read())
[print(e['id']) for e in data if e['name']=='Balrog']
" 2>/dev/null | head -1 || true)

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative/order" \
  -H "Authorization: Bearer $DM_TOKEN" \
  -d "{\"orderedIds\":[\"$INIT_BALROG\",\"$INIT_GANDALF\",\"$INIT_GOBLIN\"]}")
check "Reordenar: Balrog→Gandalf→Goblin" "200" "$R"
FIRST=$(body "$R" | python -c "
import sys,json
data=sorted(json.loads(sys.stdin.read()), key=lambda x: x['sortOrder'])
print(data[0]['name'] if data else '')
" 2>/dev/null || true)
[ "$FIRST" = "Balrog" ] && ok "Primer turno = Balrog tras reorden" || fail "Orden iniciativa" "esperado Balrog, got '$FIRST'"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative/$INIT_GANDALF/activate" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Activar turno Gandalf" "200" "$R"
ACTIVE=$(body "$R" | python -c "
import sys,json
data=json.loads(sys.stdin.read())
[print(e['name']) for e in data if e.get('isActive')]
" 2>/dev/null | head -1 || true)
[ "$ACTIVE" = "Gandalf" ] && ok "Turno activo = Gandalf" || fail "Turno activo" "esperado Gandalf, got '$ACTIVE'"
# El resto NO deben estar activos
ACTIVE_COUNT=$(body "$R" | python -c "
import sys,json
data=json.loads(sys.stdin.read())
print(len([e for e in data if e.get('isActive')]))
" 2>/dev/null || echo "?")
[ "$ACTIVE_COUNT" = "1" ] && ok "Solo 1 entrada activa a la vez" || fail "Activos múltiples" "got $ACTIVE_COUNT activos"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative/$INIT_GOBLIN" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar Goblin" "200" "$R"
REMAINING=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "?")
[ "$REMAINING" = "2" ] && ok "Quedan 2 entradas tras eliminar Goblin" || fail "Count tras delete" "esperado 2, got $REMAINING"

# Player bloqueado
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -H "Authorization: Bearer $PL_TOKEN" -d '{"name":"Intruso","initiative":5}')
C=$(code "$R"); [ "$C" = "403" ] && ok "Player bloqueado de añadir iniciativa (403)" || fail "Auth iniciativa" "esperado 403, got $C"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Limpiar toda la iniciativa" "204" "$R"

# ============================================================
section "10. Token Conditions"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"condition":"Envenenado"}')
check "Añadir condición Envenenado" "201" "$R"
COND_ID=$(body "$R" | python -c "
import sys,json
data=json.loads(sys.stdin.read())
[print(c['id']) for c in data if c['condition']=='Envenenado']
" 2>/dev/null | head -1 || true)

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"condition":"Aturdido"}')
check "Añadir condición Aturdido" "201" "$R"

# Condición duplicada → 409
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -H "Authorization: Bearer $DM_TOKEN" -d '{"condition":"Envenenado"}')
C=$(code "$R"); [ "$C" = "409" ] && ok "Condición duplicada → 409 Conflict" || fail "Duplicado condición" "esperado 409, got $C"

# Verificar condiciones en listado de tokens
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Listar tokens (con conditions)" "200" "$R"
CCOUNT=$(body "$R" | python -c "
import sys,json
data=json.loads(sys.stdin.read())
for t in data:
    if t['id']=='$TOKEN_ID': print(len(t.get('conditions',[]))); break
" 2>/dev/null || echo "0")
[ "$CCOUNT" = "2" ] && ok "Token tiene 2 condiciones en listado" || fail "Conditions en list" "esperado 2, got $CCOUNT"

# Eliminar condición Envenenado
R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions/$COND_ID" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar condición Envenenado" "200" "$R"
REMAINING=$(body "$R" | python -c "import sys,json; print(len(json.loads(sys.stdin.read())))" 2>/dev/null || echo "?")
[ "$REMAINING" = "1" ] && ok "Queda 1 condición (Aturdido)" || fail "Conditions tras delete" "esperado 1, got $REMAINING"

# Player bloqueado
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -H "Authorization: Bearer $PL_TOKEN" -d '{"condition":"Asustado"}')
C=$(code "$R"); [ "$C" = "403" ] && ok "Player bloqueado de añadir condiciones (403)" || fail "Auth conditions" "esperado 403, got $C"

# ============================================================
section "11. Limpieza"
# ============================================================
R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar token" "204" "$R"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE2_ID" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar escena 2 (Dungeon)" "204" "$R"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar escena 1 (Taberna)" "204" "$R"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" \
  -H "Authorization: Bearer $DM_TOKEN")
check "Eliminar recurso HP" "204" "$R"

# ============================================================
echo -e "\n${BOLD}═══════════════════════════════════════════${NC}"
echo -e "  ${GREEN}${BOLD}$PASS pasaron${NC}  ${RED}${BOLD}$FAIL fallaron${NC}  ${GRAY}$SKIPPED saltados${NC}"
echo -e "${BOLD}═══════════════════════════════════════════${NC}"
[ "$FAIL" -eq 0 ] && exit 0 || exit 1
