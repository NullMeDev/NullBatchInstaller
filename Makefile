# NullInstaller Makefile for Unix systems (Linux/macOS)

# Default target
all: build

# Variables
BINARY_NAME=NullInstaller
DIST_DIR=dist
LDFLAGS=-s -w
SOURCES=*.go

# Go environment
GO=go
GOOS?=$(shell go env GOOS)
GOARCH?=$(shell go env GOARCH)

# Create dist directory
$(DIST_DIR):
	mkdir -p $(DIST_DIR)

# Build target
build: $(DIST_DIR)
	CGO_ENABLED=1 $(GO) build -ldflags "$(LDFLAGS)" -o $(DIST_DIR)/$(BINARY_NAME) $(SOURCES)
	@echo "Build complete: $(DIST_DIR)/$(BINARY_NAME)"
	@ls -lh $(DIST_DIR)/$(BINARY_NAME)

# Build for specific platforms
build-linux: $(DIST_DIR)
	CGO_ENABLED=1 GOOS=linux GOARCH=amd64 $(GO) build -ldflags "$(LDFLAGS)" -o $(DIST_DIR)/$(BINARY_NAME)-linux $(SOURCES)

build-darwin: $(DIST_DIR)
	CGO_ENABLED=1 GOOS=darwin GOARCH=amd64 $(GO) build -ldflags "$(LDFLAGS)" -o $(DIST_DIR)/$(BINARY_NAME)-darwin $(SOURCES)

build-windows: $(DIST_DIR)
	CGO_ENABLED=1 GOOS=windows GOARCH=amd64 $(GO) build -ldflags "$(LDFLAGS) -H=windowsgui" -o $(DIST_DIR)/$(BINARY_NAME).exe $(SOURCES)

# Development targets
run:
	CGO_ENABLED=1 $(GO) run $(SOURCES)

test:
	$(GO) test ./...

clean:
	rm -rf $(DIST_DIR)
	$(GO) clean

# Install dependencies
deps:
	$(GO) mod tidy
	$(GO) mod download

# Check dependencies and environment
check:
	@echo "Checking build environment..."
	@$(GO) version
	@echo "CGO_ENABLED=$(CGO_ENABLED)"
	@echo "GOOS=$(GOOS)"
	@echo "GOARCH=$(GOARCH)"
	@which gcc >/dev/null || echo "Warning: GCC not found - required for CGO"
	@echo "Dependencies:"
	@$(GO) list -m all

# Help target
help:
	@echo "NullInstaller Build Targets:"
	@echo "  build          Build for current platform"
	@echo "  build-linux    Build for Linux"
	@echo "  build-darwin   Build for macOS"
	@echo "  build-windows  Build for Windows"
	@echo "  run            Run from source"
	@echo "  test           Run tests"
	@echo "  clean          Clean build artifacts"
	@echo "  deps           Download dependencies"
	@echo "  check          Check build environment"
	@echo "  help           Show this help"

.PHONY: all build build-linux build-darwin build-windows run test clean deps check help
