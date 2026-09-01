#!/usr/bin/env bash

PROJECT_PATH="${1:-.}"
pushd "$PROJECT_PATH" >/dev/null || exit 1

# Initialize user-secrets for the project (no-op if already initialized)
dotnet user-secrets init

# Generate 32 random bytes and encode as base64
if command -v head >/dev/null 2>&1 && [ -r /dev/urandom ]; then
  SECRET=$(head -c32 /dev/urandom | base64)
else
  # Fallback to openssl if available
  SECRET=$(openssl rand -base64 32)
fi

# Store into user-secrets under the JWT:Key configuration path
dotnet user-secrets set "JWT:Key" "$SECRET"

echo "JWT:Key set to a 32-byte base64 secret."

popd >/dev/null || exit 1
