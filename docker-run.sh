#!/bin/bash
# Docker build and run script for Snappay microservices with Dapr

set -e

ACTION="${1:-up}"

check_docker() {
    if ! command -v docker &> /dev/null; then
        echo "❌ Docker is not installed"
        echo "Please follow the installation guide: DOCKER_SETUP.md"
        exit 1
    fi

    if ! command -v docker-compose &> /dev/null; then
        echo "❌ Docker Compose is not installed"
        echo "Please follow the installation guide: DOCKER_SETUP.md"
        exit 1
    fi

    echo "✅ Docker: $(docker --version)"
    echo "✅ Docker Compose: $(docker-compose --version)"
}

start_containers() {
    echo ""
    echo "🚀 Building and starting Snappay containers..."
    echo "This may take several minutes on first run."
    echo ""
    
    docker-compose up --build
}

start_containers_detached() {
    echo ""
    echo "🚀 Building and starting Snappay containers (background mode)..."
    
    docker-compose up -d --build
    
    echo ""
    echo "✅ Containers started in background"
    echo "View logs with: docker-compose logs -f"
}

stop_containers() {
    echo ""
    echo "🛑 Stopping containers..."
    
    docker-compose down
    
    echo ""
    echo "✅ Containers stopped"
}

view_logs() {
    echo ""
    echo "📋 Showing container logs (press Ctrl+C to exit)..."
    
    docker-compose logs -f
}

rebuild_containers() {
    echo ""
    echo "🔨 Rebuilding containers without cache..."
    
    docker-compose up --build --no-cache
}

list_containers() {
    echo ""
    echo "📦 Running containers:"
    
    docker ps
}

# Main execution
echo "╔═══════════════════════════════════════════════════════════════╗"
echo "║         Snappay Microservices Docker Management Script        ║"
echo "╚═══════════════════════════════════════════════════════════════╝"
echo ""

check_docker

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
