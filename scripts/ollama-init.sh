#!/bin/sh
set -e

OLLAMA_HOST="${OLLAMA_HOST:-http://ollama:11434}"

echo "Waiting for Ollama to be ready at ${OLLAMA_HOST}..."

# Wait for Ollama to respond
MAX_RETRIES=30
RETRY_COUNT=0
until curl -sf "${OLLAMA_HOST}/" > /dev/null 2>&1; do
    RETRY_COUNT=$((RETRY_COUNT + 1))
    if [ $RETRY_COUNT -ge $MAX_RETRIES ]; then
        echo "ERROR: Ollama did not become ready after ${MAX_RETRIES} attempts"
        exit 1
    fi
    echo "Attempt ${RETRY_COUNT}/${MAX_RETRIES} - Ollama not ready, waiting 5s..."
    sleep 5
done

echo "Ollama is ready. Pulling models..."

# Pull the 3 benchmark models
MODELS="phi3.5 deepseek-r1:1.5b llama3.2:3b"

for MODEL in $MODELS; do
    echo "Pulling ${MODEL}..."
    curl -sf "${OLLAMA_HOST}/api/pull" -d "{\"name\": \"${MODEL}\"}" | while read -r line; do
        STATUS=$(echo "$line" | grep -o '"status":"[^"]*"' | head -1 | cut -d'"' -f4)
        if [ -n "$STATUS" ]; then
            printf "\r  %s: %s                    " "$MODEL" "$STATUS"
        fi
    done
    echo ""
    echo "  ${MODEL} pulled successfully."
done

echo ""
echo "All models pulled. Verifying..."
curl -sf "${OLLAMA_HOST}/api/tags" | grep -o '"name":"[^"]*"' | while read -r line; do
    echo "  Available: $(echo "$line" | cut -d'"' -f4)"
done

echo ""
echo "Ollama initialization complete."
