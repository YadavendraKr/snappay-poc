#!/bin/bash
# Podman build and run script for Snappay microservices with Dapr
# Podman is a daemonless container engine (more secure than Docker)

set -e

ACTION="${1:-up}"

check_podman() {
    if ! command -v podman &> /dev/null; then
        echo "❌ Podman is not installed"
        echo "Please follow the installation guide: PODMAN_SETUP.md"
        exit 1
    fi

    if ! command -v podman-compose &> /dev/null; then
        echo "❌ Podman Compose is not installed"
        echo "Please follow the installation guide: PODMAN_SETUP.md"
        exit 1
    fi

    echo "✅ Podman: $(podman --version)"
    echo "✅ Podman Compose: $(podman-compose --version)"
}

start_containers() {
    echo ""
    echo "🚀 Building and starting Snappay containers with Podman..."
    echo "This may take several minutes on first run."
    echo ""
    
    podman-compose up --build
}

start_containers_detached() {
    echo ""
    echo "🚀 Building and starting Snappay containers in background..."
    
    podman-compose up -d --build
    
    echo ""
    echo "✅ Containers started in background"
    echo "View logs with: podman-compose logs -f"
}

stop_containers() {
    echo ""
    echo "🛑 Stopping containers..."
    
    podman-compose down
    
    echo ""
    echo "✅ Containers stopped"
}

view_logs() {
    echo ""
    echo "📋 Showing container logs (press Ctrl+C to exit)..."
    
    podman-compose logs -f
}

rebuild_containers() {
    echo ""
    echo "🔨 Rebuilding containers without cache..."
    
    podman-compose up --build --no-cache
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
