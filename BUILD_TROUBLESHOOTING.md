# Build Troubleshooting Guide

This document helps resolve common build issues with NullInstaller.

## CGO Compilation Errors

### Error: `cgo: cannot parse $WORK\..._cgo_.o as ELF, Mach-O, PE or XCOFF`

This error indicates a problem with the CGO toolchain configuration.

**Possible Causes:**
1. Incompatible GCC/MinGW version
2. Mixed 32-bit/64-bit toolchains
3. Corrupted CGO cache
4. PATH environment variable conflicts

**Solutions:**

#### Solution 1: Reset CGO Cache
```bash
go clean -cache
go clean -modcache
go env GOCACHE  # Check cache location
# Manually delete the cache directory if needed
```

#### Solution 2: Check Toolchain Compatibility
```bash
# Check Go architecture
go env GOARCH GOOS

# Check GCC architecture
gcc -v
gcc -dumpmachine

# Ensure both are 64-bit (amd64) if building 64-bit
```

#### Solution 3: Reinstall MinGW/TDM-GCC
**Windows:**
1. Uninstall current GCC/MinGW
2. Download and install TDM-GCC 64-bit: https://jmeubank.github.io/tdm-gcc/
3. Add `C:\TDM-GCC-64\bin` to PATH (before any other GCC installations)
4. Restart terminal/IDE
5. Verify: `gcc -v` should show TDM-GCC

#### Solution 4: Alternative MinGW Installation (Windows)
Using MSYS2:
```bash
# Install MSYS2 from https://www.msys2.org/
# In MSYS2 terminal:
pacman -S mingw-w64-x86_64-gcc
pacman -S mingw-w64-x86_64-pkg-config

# Add to PATH: C:\msys64\mingw64\bin
```

#### Solution 5: Use Different Build Tags
Try building with different Fyne build tags:
```bash
# Software rendering (no OpenGL)
go build -tags no_native_menus,software .

# Disable certain features
go build -tags no_native_menus,disable_touchpad_mouse_events .
```

## Windows-Specific Issues

### Error: `gcc: command not found`
**Solution:** Install a C compiler:
- **Recommended:** TDM-GCC (easiest)
- **Alternative:** MinGW-w64 via MSYS2
- **Enterprise:** Visual Studio Build Tools

### Error: `undefined reference to...`
**Possible Causes:**
- Missing system libraries
- Wrong GCC target architecture

**Solution:**
```bash
# Check what libraries are being linked
go build -x . 2>&1 | grep -E "(gcc|ld)"

# For Windows, ensure you're using the right MinGW
where gcc
gcc -print-search-dirs
```

## Linux-Specific Issues

### Error: `pkg-config: command not found`
```bash
# Ubuntu/Debian
sudo apt-get install pkg-config

# Fedora/RHEL
sudo dnf install pkgconf
```

### Error: Missing development libraries
```bash
# Ubuntu/Debian - Install all Fyne dependencies
sudo apt-get install gcc pkg-config libgl1-mesa-dev libxcursor-dev libxrandr-dev libxinerama-dev libxi-dev libglfw3-dev libxxf86vm-dev

# Fedora/RHEL
sudo dnf install gcc pkg-config mesa-libGL-devel libXcursor-devel libXrandr-devel libXinerama-devel libXi-devel glfw-devel libXxf86vm-devel
```

## macOS-Specific Issues

### Error: `xcrun: error: invalid active developer path`
```bash
# Install Xcode Command Line Tools
xcode-select --install

# Reset developer path if needed
sudo xcode-select --reset
```

## General Go Issues

### Error: `go: cannot find main module`
**Solution:** Ensure you're in the project directory with `go.mod`

### Error: Module download failures
```bash
# Set Go proxy (if behind corporate firewall)
go env -w GOPROXY=direct
go env -w GOSUMDB=off

# Or use different proxy
go env -w GOPROXY=https://proxy.golang.org,direct
```

### Error: Permission denied
**Linux/macOS:**
```bash
sudo chown -R $(whoami):$(whoami) $GOPATH
sudo chown -R $(whoami):$(whoami) $(go env GOMODCACHE)
```

**Windows:**
- Run terminal as Administrator
- Check file permissions on project directory

## Alternative Build Methods

If standard builds fail, try these alternatives:

### Method 1: Docker Build (Linux)
```dockerfile
FROM golang:1.22-alpine
RUN apk add --no-cache gcc musl-dev pkgconfig
# ... build steps
```

### Method 2: Cross-compilation from Linux
```bash
# Install cross-compilation tools
sudo apt-get install gcc-mingw-w64

# Build for Windows from Linux
CGO_ENABLED=1 GOOS=windows GOARCH=amd64 CC=x86_64-w64-mingw32-gcc go build .
```

### Method 3: Disable CGO (Limited Functionality)
```bash
# This will disable GUI features but allow basic compilation testing
CGO_ENABLED=0 go build .
```

## Debugging Build Issues

### Verbose Build Output
```bash
# See all build commands
go build -x . 

# Check CGO environment
go env CGO_ENABLED CGO_CFLAGS CGO_CXXFLAGS CGO_LDFLAGS

# Test CGO separately
cat > test_cgo.go << 'EOF'
package main
import "C"
func main() {}
EOF
go build test_cgo.go
```

### Environment Check Script
```bash
#!/bin/bash
echo "=== Build Environment Check ==="
echo "Go Version: $(go version)"
echo "GOOS: $(go env GOOS)"
echo "GOARCH: $(go env GOARCH)"
echo "CGO_ENABLED: $(go env CGO_ENABLED)"
echo "GOROOT: $(go env GOROOT)"
echo "GOPATH: $(go env GOPATH)"
echo ""
echo "GCC Version:"
gcc --version 2>/dev/null || echo "GCC not found"
echo ""
echo "Pkg-config:"
pkg-config --version 2>/dev/null || echo "pkg-config not found"
```

## Getting Help

If none of these solutions work:

1. **Check Go version:** Ensure you're using Go 1.22+
2. **Check Fyne docs:** https://developer.fyne.io/started/
3. **Search issues:** Check NullInstaller and Fyne GitHub issues
4. **Create minimal test:** Try building a simple Fyne "hello world" app first
5. **System info:** Provide OS version, Go version, GCC version when asking for help

## Known Working Configurations

### Windows 10/11
- Go 1.22+ 
- TDM-GCC 9.2.0+ (64-bit)
- OR MinGW-w64 8.0+ via MSYS2

### Ubuntu 20.04+
- Go 1.22+
- GCC 9.0+
- All required -dev packages

### macOS 12+
- Go 1.22+
- Xcode Command Line Tools 13+
- Homebrew GCC (optional)

## Last Resort: Pre-built Binaries

If you cannot build from source, look for pre-built binaries in the releases section or consider using the development build with `go run .` for testing purposes.
