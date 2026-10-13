FROM debian:bullseye-slim
RUN apt-get update && apt-get install -y --no-install-recommends gcc-aarch64-linux-gnu libc6-dev-arm64-cross file && rm -rf /var/lib/apt/lists/*
WORKDIR /workspace
