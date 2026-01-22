FROM ghcr.io/kramins/vintagestory-devcontainer:1.21.6

# Install GitHub Copilot CLI
RUN curl -fsSL https://gh.io/copilot-install | bash -s -- PREFIX="/usr/local" \
    || true && \
    # Ensure copilot is on PATH for all users
    ln -sf /home/vintagestory/.local/bin/copilot /usr/local/bin/copilot || true

