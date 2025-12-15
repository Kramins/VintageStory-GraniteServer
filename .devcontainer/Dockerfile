FROM docker.io/kramins/vintagestory:latest

RUN useradd -m vscode \
    && apt-get update && apt-get install software-properties-common -y \
    && add-apt-repository ppa:dotnet/backports -y \
    && apt-get update \
    && apt-get install -y sudo dotnet-sdk-8.0 dotnet-sdk-9.0 git \
    && echo "vintagestory ALL=(ALL) NOPASSWD:ALL" >> /etc/sudoers.d/nopasswd \
    && chmod 0440 /etc/sudoers.d/nopasswd

RUN chown -R vintagestory:vintagestory /home/vintagestory

USER vintagestory
SHELL ["/bin/bash", "-c"]
RUN curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.40.3/install.sh | bash && \
    source "$HOME/.nvm/nvm.sh" && \
    nvm install 24 \
    && dotnet tool install csharpier --global
ENV NVM_DIR="/root/.nvm"
ENV PATH="$NVM_DIR/versions/node/v24.0.0/bin/:${PATH}"

# Set permissions for the user (optional, adjust as needed)


# Switch to non-root user
