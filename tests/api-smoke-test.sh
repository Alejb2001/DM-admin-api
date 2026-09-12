#!/usr/bin/env bash
# ============================================================
# DM Admin API — Smoke test completo
# Uso: ./api-smoke-test.sh [base_url]
# Ejemplo: ./api-smoke-test.sh http://localhost:5226
# ============================================================
set -euo pipefail

BASE="${1:-http://localhost:5226}"
API="$BASE/api"
PASS=0; FAIL=0

# ── Colores ──────────────────────────────────────────────────
GREEN='\033[0;32m'; RED='\033[0;31m'; YELLOW='\033[1;33m'; NC='\033[0m'; BOLD='\033[1m'

ok()   { echo -e "  ${GREEN}✓${NC} $1"; ((PASS++)); }
fail() { echo -e "  ${RED}✗${NC} $1 — $2"; ((FAIL++)); }
section() { echo -e "\n${BOLD}${YELLOW}▶ $1${NC}"; }

# ── Helper: curl con código HTTP ─────────────────────────────
req() {
  local method="$1" url="$2"; shift 2
  curl -s -X "$method" "$url" \
    -H "Content-Type: application/json" \
    "$@" -w "\n%{http_code}" 2>/dev/null
}

check() {
  local label="$1" expected="$2" response="$3"
  local body code
  body=$(echo "$response" | head -n -1)
  code=$(echo "$response" | tail -n 1)
  if [ "$code" = "$expected" ]; then
    ok "$label (HTTP $code)"
    echo "$body"
  else
    fail "$label" "esperado $expected, got $code — $body"
    echo ""
  fi
}

extract() { echo "$1" | grep -oP "\"$2\":\"[^\"]+\"" | head -1 | grep -oP '(?<=:")[^"]+'; }

# ============================================================
section "0. Conectividad"
# ============================================================
HEALTH=$(curl -s -o /dev/null -w "%{http_code}" "$BASE/scalar/v1" 2>/dev/null || echo "000")
if [ "$HEALTH" != "000" ]; then
  ok "API alcanzable en $BASE"
else
  fail "API alcanzable" "No se pudo conectar a $BASE — ¿está corriendo la API?"
  echo -e "\n${RED}Abortando — API no disponible.${NC}"
  exit 1
fi

# ============================================================
section "1. Auth — Registro y login"
# ============================================================
TIMESTAMP=$(date +%s)
DM_EMAIL="dm_test_${TIMESTAMP}@test.com"
PL_EMAIL="player_test_${TIMESTAMP}@test.com"
PWD="Test@1234!"

R=$(req POST "$API/auth/register" -d "{\"email\":\"$DM_EMAIL\",\"password\":\"$PWD\",\"displayName\":\"DM Test\"}")
check "Registro DM" "201" "$R"

R=$(req POST "$API/auth/register" -d "{\"email\":\"$PL_EMAIL\",\"password\":\"$PWD\",\"displayName\":\"Player Test\"}")
check "Registro Player" "201" "$R"

R=$(req POST "$API/auth/login" -d "{\"email\":\"$DM_EMAIL\",\"password\":\"$PWD\"}")
check "Login DM" "200" "$R"
BODY=$(echo "$R" | head -n -1)
DM_TOKEN=$(echo "$BODY" | grep -oP '"accessToken":"[^"]+"' | grep -oP '(?<=:")[^"]+')
DM_ID=$(echo "$BODY" | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req POST "$API/auth/login" -d "{\"email\":\"$PL_EMAIL\",\"password\":\"$PWD\"}")
check "Login Player" "200" "$R"
PL_TOKEN=$(echo "$R" | head -n -1 | grep -oP '"accessToken":"[^"]+"' | grep -oP '(?<=:")[^"]+')

DM_AUTH="-H \"Authorization: Bearer $DM_TOKEN\""
PL_AUTH="-H \"Authorization: Bearer $PL_TOKEN\""
AUTH_DM=(-H "Authorization: Bearer $DM_TOKEN")
AUTH_PL=(-H "Authorization: Bearer $PL_TOKEN")

# ============================================================
section "2. Campaigns"
# ============================================================
R=$(req POST "$API/campaigns" -d '{"name":"Test Campaign","description":"Prueba"}' "${AUTH_DM[@]}")
check "Crear campaña" "201" "$R"
CAMPAIGN_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')
JOIN_CODE=$(echo "$R" | head -n -1 | grep -oP '"joinCode":"[^"]+"' | grep -oP '(?<=:")[^"]+')

R=$(req GET "$API/campaigns" "${AUTH_DM[@]}")
check "Listar campañas DM" "200" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID" "${AUTH_DM[@]}")
check "Detalle campaña" "200" "$R"

# Player joins campaign
R=$(req POST "$API/campaigns/join" -d "{\"joinCode\":\"$JOIN_CODE\"}" "${AUTH_PL[@]}")
check "Player se une con código" "200" "$R"

# ============================================================
section "3. World — Entity Types & Entities"
# ============================================================
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/entity-types" "${AUTH_DM[@]}")
check "Listar entity types" "200" "$R"
PERSONAJE_TYPE_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+","name":"Personaje"' | grep -oP '"id":"[^"]+"' | grep -oP '(?<=:")[^"]+')

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entity-types" \
  -d '{"name":"Dios","icon":"star","color":"#FFD700"}' "${AUTH_DM[@]}")
check "Crear entity type custom" "201" "$R"
CUSTOM_TYPE_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entity-types/$CUSTOM_TYPE_ID/fields" \
  -d '{"name":"Poder","fieldType":"number","isRequired":false,"sortOrder":0,"isRollFormula":true}' "${AUTH_DM[@]}")
check "Agregar campo isRollFormula=true" "201" "$R"
FIELD_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entities" \
  -d "{\"name\":\"Gandalf\",\"entityTypeId\":\"$PERSONAJE_TYPE_ID\",\"customFields\":{}}" "${AUTH_DM[@]}")
check "Crear entidad Personaje" "201" "$R"
ENTITY_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/entities" "${AUTH_DM[@]}")
check "Listar entidades" "200" "$R"

# ============================================================
section "4. Ficha de Personaje (CharacterResources)"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources" \
  -d '{"name":"HP","current":30,"max":30,"color":"#e53935","sortOrder":0}' "${AUTH_DM[@]}")
check "Crear recurso HP" "201" "$R"
RESOURCE_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources" "${AUTH_DM[@]}")
check "Listar recursos" "200" "$R"

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" \
  -d '{"current":25}' "${AUTH_DM[@]}")
check "Actualizar valor HP (25)" "200" "$R"
CURRENT=$(echo "$R" | head -n -1 | grep -oP '"current":[0-9]+' | grep -oP '[0-9]+')
[ "$CURRENT" = "25" ] && ok "Valor HP = 25 correcto" || fail "Valor HP" "esperado 25, got $CURRENT"

# Over-max clamp
R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" \
  -d '{"current":999}' "${AUTH_DM[@]}")
check "Clamp HP al máximo (999→30)" "200" "$R"
CURRENT=$(echo "$R" | head -n -1 | grep -oP '"current":[0-9]+' | grep -oP '[0-9]+')
[ "$CURRENT" = "30" ] && ok "HP clamp funciona (30)" || fail "HP clamp" "esperado 30, got $CURRENT"

# Under-zero clamp
R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" \
  -d '{"current":-100}' "${AUTH_DM[@]}")
check "Clamp HP al mínimo (-100→0)" "200" "$R"
CURRENT=$(echo "$R" | head -n -1 | grep -oP '"current":[0-9]+' | grep -oP '[0-9]+')
[ "$CURRENT" = "0" ] && ok "HP clamp mínimo (0)" || fail "HP clamp mínimo" "esperado 0, got $CURRENT"

# ============================================================
section "5. Sessions"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions" \
  -d '{"name":"Sesión de prueba"}' "${AUTH_DM[@]}")
check "Crear sesión" "201" "$R"
SESSION_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions" "${AUTH_DM[@]}")
check "Listar sesiones" "200" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID" "${AUTH_DM[@]}")
check "Detalle sesión" "200" "$R"

# ============================================================
section "6. Mapa Virtual — Escenas"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes" \
  -d '{"name":"Taberna","gridSize":50,"gridEnabled":true}' "${AUTH_DM[@]}")
check "Crear escena" "201" "$R"
SCENE_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes" "${AUTH_DM[@]}")
check "Listar escenas" "200" "$R"
FOG_ENABLED=$(echo "$R" | head -n -1 | grep -oP '"fogEnabled":(true|false)' | grep -oP '(true|false)')
[ "$FOG_ENABLED" = "false" ] && ok "fogEnabled=false por defecto" || fail "fogEnabled default" "esperado false, got $FOG_ENABLED"

R=$(req PUT "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID" \
  -d '{"name":"Taberna Actualizada","gridSize":60,"gridEnabled":true}' "${AUTH_DM[@]}")
check "Actualizar escena" "200" "$R"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/activate" \
  "${AUTH_DM[@]}")
check "Activar escena" "200" "$R"

# Player puede ver escenas activas
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes" "${AUTH_PL[@]}")
check "Player puede listar escenas" "200" "$R"

# ============================================================
section "7. Mapa Virtual — Tokens"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens" \
  -d "{\"label\":\"Gandalf\",\"color\":\"#3F51B5\",\"x\":2,\"y\":3,\"width\":1,\"height\":1,\"entityId\":\"$ENTITY_ID\"}" \
  "${AUTH_DM[@]}")
check "Añadir token" "201" "$R"
TOKEN_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens" "${AUTH_DM[@]}")
check "Listar tokens" "200" "$R"
# Verify conditions array present
CONDS=$(echo "$R" | head -n -1 | grep -oP '"conditions":\[\]')
[ -n "$CONDS" ] && ok "Token incluye conditions:[] vacío" || fail "conditions array" "no encontrado en la respuesta"

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/move" \
  -d '{"x":5,"y":5}' "${AUTH_DM[@]}")
check "Mover token" "200" "$R"

R=$(req PUT "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID" \
  -d '{"label":"Gandalf el Gris","color":"#9C27B0","width":1,"height":1,"isVisible":true,"controlledBy":null}' \
  "${AUTH_DM[@]}")
check "Actualizar token (isVisible, label)" "200" "$R"

# ============================================================
section "8. VTT Avanzado — Fog of War"
# ============================================================
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog" "${AUTH_DM[@]}")
check "Listar zonas de niebla (inicialmente vacío)" "200" "$R"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" \
  -d '{"shape":"rect","x":0,"y":0,"width":200,"height":200}' "${AUTH_DM[@]}")
check "Añadir zona fog (rect)" "201" "$R"
FOG_ZONE_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" \
  -d '{"shape":"circle","x":300,"y":300,"radius":100}' "${AUTH_DM[@]}")
check "Añadir zona fog (circle)" "201" "$R"
FOG_ZONE_CIRCLE_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog" "${AUTH_DM[@]}")
check "Listar zonas fog (2 zonas)" "200" "$R"
COUNT=$(echo "$R" | head -n -1 | grep -oP '"id":"' | wc -l)
[ "$COUNT" -ge 2 ] && ok "Hay $COUNT zonas fog" || fail "Zonas fog count" "esperado >=2, got $COUNT"

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/toggle" \
  -d '{"fogEnabled":true}' "${AUTH_DM[@]}")
check "Activar niebla de guerra" "200" "$R"
FOG_RESP=$(echo "$R" | head -n -1 | grep -oP '"fogEnabled":true')
[ -n "$FOG_RESP" ] && ok "fogEnabled=true en respuesta" || fail "fogEnabled" "no true en respuesta"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones/$FOG_ZONE_CIRCLE_ID" \
  "${AUTH_DM[@]}")
check "Eliminar zona fog (circle)" "204" "$R"

R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog" "${AUTH_DM[@]}")
check "Listar zonas fog (1 queda)" "200" "$R"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" "${AUTH_DM[@]}")
check "Limpiar toda la niebla" "204" "$R"

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/toggle" \
  -d '{"fogEnabled":false}' "${AUTH_DM[@]}")
check "Desactivar niebla de guerra" "200" "$R"

# Player NO puede modificar fog
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/fog/zones" \
  -d '{"shape":"rect","x":0,"y":0,"width":100,"height":100}' "${AUTH_PL[@]}")
CODE=$(echo "$R" | tail -n 1)
[ "$CODE" = "403" ] && ok "Player no puede crear fog zones (403)" || fail "Auth fog" "esperado 403, got $CODE"

# ============================================================
section "9. VTT Avanzado — Iniciativa"
# ============================================================
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" "${AUTH_DM[@]}")
check "Listar iniciativa (vacía)" "200" "$R"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -d '{"name":"Gandalf","initiative":20}' "${AUTH_DM[@]}")
check "Añadir entrada iniciativa (Gandalf 20)" "201" "$R"
INIT_GANDALF=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -d '{"name":"Goblin","initiative":15}' "${AUTH_DM[@]}")
check "Añadir entrada iniciativa (Goblin 15)" "201" "$R"
INIT_GOBLIN=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | grep -oP '(?<=:")[^"]+' | tail -1)

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -d '{"name":"Balrog","initiative":25}' "${AUTH_DM[@]}")
check "Añadir entrada iniciativa (Balrog 25)" "201" "$R"
INIT_BALROG=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | grep -oP '(?<=:")[^"]+' | tail -1)

R=$(req PATCH "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative/order" \
  -d "{\"orderedIds\":[\"$INIT_BALROG\",\"$INIT_GANDALF\",\"$INIT_GOBLIN\"]}" "${AUTH_DM[@]}")
check "Reordenar iniciativa" "200" "$R"
FIRST_NAME=$(echo "$R" | head -n -1 | grep -oP '"name":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')
[ "$FIRST_NAME" = "Balrog" ] && ok "Primer turno = Balrog tras reorden" || fail "Orden iniciativa" "esperado Balrog, got $FIRST_NAME"

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative/$INIT_GANDALF/activate" \
  "${AUTH_DM[@]}")
check "Activar turno Gandalf" "200" "$R"
ACTIVE_NAME=$(echo "$R" | head -n -1 | grep -oP '"isActive":true.*?"name":"[^"]+"' -o | grep -oP '"name":"[^"]+"' | grep -oP '(?<=:")[^"]+')
[ "$ACTIVE_NAME" = "Gandalf" ] && ok "Turno activo = Gandalf" || fail "Turno activo" "esperado Gandalf"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative/$INIT_GOBLIN" \
  "${AUTH_DM[@]}")
check "Eliminar entrada Goblin" "200" "$R"

# Player no puede modificar iniciativa
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" \
  -d '{"name":"Intruso","initiative":5}' "${AUTH_PL[@]}")
CODE=$(echo "$R" | tail -n 1)
[ "$CODE" = "403" ] && ok "Player no puede añadir iniciativa (403)" || fail "Auth iniciativa" "esperado 403, got $CODE"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/initiative" "${AUTH_DM[@]}")
check "Limpiar toda la iniciativa" "204" "$R"

# ============================================================
section "10. VTT Avanzado — Condiciones de Token"
# ============================================================
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -d '{"condition":"Envenenado"}' "${AUTH_DM[@]}")
check "Añadir condición Envenenado" "201" "$R"
COND_ID=$(echo "$R" | head -n -1 | grep -oP '"id":"[^"]+"' | head -1 | grep -oP '(?<=:")[^"]+')

R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -d '{"condition":"Aturdido"}' "${AUTH_DM[@]}")
check "Añadir condición Aturdido" "201" "$R"

# Duplicate condition should fail
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -d '{"condition":"Envenenado"}' "${AUTH_DM[@]}")
CODE=$(echo "$R" | tail -n 1)
[ "$CODE" = "409" ] && ok "Condición duplicada devuelve 409" || fail "Duplicado condición" "esperado 409, got $CODE"

# Check conditions in token list
R=$(req GET "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens" "${AUTH_DM[@]}")
check "Token list incluye condiciones" "200" "$R"
COND_COUNT=$(echo "$R" | head -n -1 | grep -oP '"condition":"[^"]+"' | wc -l)
[ "$COND_COUNT" -ge 2 ] && ok "Token tiene $COND_COUNT condiciones" || fail "Condiciones en token" "esperado >=2, got $COND_COUNT"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions/$COND_ID" \
  "${AUTH_DM[@]}")
check "Eliminar condición Envenenado" "200" "$R"
REMAINING=$(echo "$R" | head -n -1 | grep -oP '"condition":"[^"]+"' | wc -l)
[ "$REMAINING" = "1" ] && ok "Queda 1 condición tras eliminar" || fail "Condiciones restantes" "esperado 1, got $REMAINING"

# Player no puede añadir condiciones
R=$(req POST "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID/conditions" \
  -d '{"condition":"Asustado"}' "${AUTH_PL[@]}")
CODE=$(echo "$R" | tail -n 1)
[ "$CODE" = "403" ] && ok "Player no puede añadir condiciones (403)" || fail "Auth condiciones" "esperado 403, got $CODE"

# ============================================================
section "11. Limpieza — Eliminar token y escena"
# ============================================================
R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID/tokens/$TOKEN_ID" \
  "${AUTH_DM[@]}")
check "Eliminar token" "204" "$R"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/sessions/$SESSION_ID/scenes/$SCENE_ID" "${AUTH_DM[@]}")
check "Eliminar escena" "204" "$R"

R=$(req DELETE "$API/campaigns/$CAMPAIGN_ID/entities/$ENTITY_ID/resources/$RESOURCE_ID" "${AUTH_DM[@]}")
check "Eliminar recurso HP" "204" "$R"

# ============================================================
echo -e "\n${BOLD}═══════════════════════════════════════${NC}"
echo -e "${BOLD}  Resultados: ${GREEN}$PASS pasaron${NC}${BOLD}  ${RED}$FAIL fallaron${NC}"
echo -e "${BOLD}═══════════════════════════════════════${NC}"

[ "$FAIL" -eq 0 ] && exit 0 || exit 1
