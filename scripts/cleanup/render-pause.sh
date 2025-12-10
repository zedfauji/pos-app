#!/bin/bash
# Render Pause Script - Safely disconnect Render from main branch
# Prevents auto-deploys from main while keeping services running

set -e

RENDER_API_KEY="${RENDER_API_KEY:-}"
RENDER_OWNER_ID="${RENDER_OWNER_ID:-}"

if [ -z "$RENDER_API_KEY" ]; then
    echo "❌ RENDER_API_KEY environment variable not set."
    echo "   Export it: export RENDER_API_KEY='your-api-key'"
    echo "   Get API key from: https://dashboard.render.com/account/api-keys"
    exit 1
fi

BASE_URL="https://api.render.com/v1"
HEADERS=(-H "Authorization: Bearer $RENDER_API_KEY" -H "Accept: application/json")

echo "=== Render Pause & Disconnect from Main ==="
echo ""

# Services in blueprint
SERVICES=(
    "TablesApi"
    "OrderApi"
    "PaymentApi"
    "MenuApi"
    "CustomerApi"
    "DiscountApi"
    "InventoryApi"
    "SettingsApi"
    "UsersApi"
)

echo "📋 Step 1: Fetching Render services..."
SERVICES_JSON=$(curl -s "${HEADERS[@]}" "$BASE_URL/services")
SERVICE_IDS=()

for service in "${SERVICES[@]}"; do
    SERVICE_ID=$(echo "$SERVICES_JSON" | jq -r ".[] | select(.service.name == \"$service\") | .service.id" | head -1)
    if [ -n "$SERVICE_ID" ] && [ "$SERVICE_ID" != "null" ]; then
        SERVICE_IDS+=("$SERVICE_ID")
        echo "   Found: $service ($SERVICE_ID)"
    else
        echo "   ⚠️  Not found: $service"
    fi
done

echo ""
echo "⏸️  Step 2: Pausing auto-deploys..."

for SERVICE_ID in "${SERVICE_IDS[@]}"; do
    SERVICE_NAME=$(echo "$SERVICES_JSON" | jq -r ".[] | select(.service.id == \"$SERVICE_ID\") | .service.name")
    
    echo "   Pausing auto-deploy for: $SERVICE_NAME"
    
    curl -s -X PATCH "${HEADERS[@]}" \
        -H "Content-Type: application/json" \
        -d '{"autoDeploy":"no"}' \
        "$BASE_URL/services/$SERVICE_ID" > /dev/null
    
    echo "   ✓ Paused: $SERVICE_NAME"
done

echo ""
echo "🔌 Step 3: Disconnecting from main branch..."

for SERVICE_ID in "${SERVICE_IDS[@]}"; do
    SERVICE_NAME=$(echo "$SERVICES_JSON" | jq -r ".[] | select(.service.id == \"$SERVICE_ID\") | .service.name")
    
    CURRENT_SERVICE=$(curl -s "${HEADERS[@]}" "$BASE_URL/services/$SERVICE_ID")
    CURRENT_BRANCH=$(echo "$CURRENT_SERVICE" | jq -r '.service.branch')
    
    if [ "$CURRENT_BRANCH" == "main" ]; then
        echo "   Disconnecting $SERVICE_NAME from main branch..."
        
        curl -s -X PATCH "${HEADERS[@]}" \
            -H "Content-Type: application/json" \
            -d '{"repo":null,"branch":null}' \
            "$BASE_URL/services/$SERVICE_ID" > /dev/null
        
        echo "   ✓ Disconnected: $SERVICE_NAME"
    else
        echo "   ℹ️  $SERVICE_NAME already disconnected (branch: $CURRENT_BRANCH)"
    fi
done

echo ""
echo "✅ Render Services Paused & Disconnected"
echo ""
echo "Services are still running but will not auto-deploy from main."
echo "Next: Migrate to DigitalOcean for cost savings ($71/mo saved)."
echo ""
echo "Verification:"
echo "  Visit: https://dashboard.render.com"
echo "  Check each service → Settings → Branch should be empty/null"

