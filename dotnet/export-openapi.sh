#!/bin/sh
# Cross-platform shell script to export OpenAPI spec from ASP.NET Core project
# Usage: ./export-openapi.sh

PROJECT_DIR="./MovemasterHttpServer"
PROFILE="http"
SWAGGER_URL="http://localhost:5135/swagger/v1/swagger.json"
OUTPUT_FILE="openapi.json"

echo "Starting ASP.NET Core project..."
dotnet run --launch-profile $PROFILE --project "$PROJECT_DIR" > /dev/null 2>&1 &
SERVER_PID=$!

# Wait for the server to start (max 20 attempts)
MAX_ATTEMPTS=20
ATTEMPT=0
STARTED=0
while [ $ATTEMPT -lt $MAX_ATTEMPTS ]; do
    sleep 1
    HTTP_STATUS=$(curl -s -o /dev/null -w "%{http_code}" "$SWAGGER_URL")
    if [ "$HTTP_STATUS" = "200" ]; then
        STARTED=1
        break
    fi
    ATTEMPT=$((ATTEMPT+1))
done

if [ $STARTED -ne 1 ]; then
    echo "Failed to start server or retrieve OpenAPI spec. Stopping process."
    kill $SERVER_PID
    exit 1
fi

echo "Server started. Downloading OpenAPI spec..."
curl -s "$SWAGGER_URL" -o "$OUTPUT_FILE"
echo "OpenAPI spec saved to $OUTPUT_FILE."

kill $SERVER_PID
echo "Server stopped."
