#!/bin/bash
# Podman build and run script for Snappay microservices with Dapr
# Podman is a daemonless container engine (more secure than Docker)
set -e

ACTION="${1:-up}"
COMPOSE_CMD=""

check_podman() {
    if ! command -v podman &> /dev/null; then
        echo "❌ Podman is not installed"
        echo "Please follow the installation guide: PODMAN_SETUP.md"
        exit 1
    fi

    # Linux/Codespaces specific: Ensure the user-level Podman socket is active
    if [ "$(uname)" == "Linux" ]; then
        # Standardize XDG_RUNTIME_DIR for rootless operation
        export XDG_RUNTIME_DIR="${XDG_RUNTIME_DIR:-/run/user/$(id -u)}"
        if [ ! -d "$XDG_RUNTIME_DIR" ]; then
            export XDG_RUNTIME_DIR="/tmp/podman-$(id -u)"
            mkdir -p "$XDG_RUNTIME_DIR"
            chmod 700 "$XDG_RUNTIME_DIR"
        fi

        # Define the rootless socket path
        USER_SOCKET="$XDG_RUNTIME_DIR/podman/podman.sock"
        mkdir -p "$(dirname "$USER_SOCKET")"

        # Clean up stale service and socket files
        echo "🔄 Resetting Podman API service..."
        pkill -u "$(id -u)" -f "podman system service" || true
        rm -f "$USER_SOCKET"

        # Start Podman API service explicitly bound to the rootless socket
        podman system service --time=0 unix://"$USER_SOCKET" &

        # Wait for the socket to initialize
        for i in {1..10}; do
            [ -S "$USER_SOCKET" ] && break
            sleep 1
        done

        # Export the connection env vars after the service is started
        export CONTAINER_HOST="unix://$USER_SOCKET"
        export DOCKER_HOST="unix://$USER_SOCKET"
    fi

    if podman compose version &> /dev/null; then
        COMPOSE_CMD="podman compose"
    elif command -v podman-compose &> /dev/null; then
        COMPOSE_CMD="podman-compose"
    else
        echo "❌ Podman Compose utility not found."
        echo "Attempting to install podman-compose..."
        pip3 install podman-compose --user && COMPOSE_CMD="podman-compose" || {
            echo "❌ Failed to install podman-compose. Please check PODMAN_SETUP.md"
            exit 1
        }
    fi

    echo "✅ Podman: $(podman --version)"
    echo "✅ Compose Provider: $COMPOSE_CMD"
}

start_containers() {
    echo ""
    echo "🚀 Building and starting Snappay containers with Podman..."
    echo "This may take several minutes on first run."
    echo ""
    
    $COMPOSE_CMD up --build
}

start_containers_detached() {
    echo ""
    echo "🚀 Building and starting Snappay containers in background..."
    
    $COMPOSE_CMD up -d --build
    
    echo ""
    echo "✅ Containers started in background"
    echo "View logs with: $COMPOSE_CMD logs -f"
}

stop_containers() {
    echo ""
    echo "🛑 Stopping containers..."
    
    $COMPOSE_CMD down
    
    echo ""
    echo "✅ Containers stopped"
}

view_logs() {
    echo ""
    echo "📋 Showing container logs (press Ctrl+C to exit)..."
    
    $COMPOSE_CMD logs -f
}

rebuild_containers() {
    echo ""
    echo "🔨 Rebuilding containers without cache..."
    
    $COMPOSE_CMD up --build --no-cache
}

list_containers() {
    echo ""
    echo "📦 Running containers:"
    
    podman ps
}

# Main execution
echo "╔═══════════════════════════════════════════════════════════════╗"
echo "║      Snappay Microservices Podman Management Script           ║"
echo "║                 (Daemonless Container Engine)                 ║"
echo "╚═══════════════════════════════════════════════════════════════╝"
echo ""

check_podman

case "$ACTION" in
    up)
        start_containers
        ;;
    down)
        stop_containers
        ;;
    logs)
        view_logs
        ;;
    rebuild)
        rebuild_containers
        ;;
    ps)
        list_containers
        ;;
    stop)
        stop_containers
        ;;
    *)
        echo "Usage: $0 {up|down|logs|rebuild|ps|stop}"
        exit 1
        ;;
esac
