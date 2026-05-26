# Install Podman - Manual Instructions

## 🚨 Critical Prerequisite: WSL 2

Podman on Windows **requires** the Windows Subsystem for Linux (WSL 2). If you see errors about "WSL not found" or "Virtualization not enabled", run this in **Administrator PowerShell**:

```powershell
wsl --install
wsl --update
```
**You must restart your computer after running these commands.**

---

## ⚠️ Installation Was Cancelled

The automated installation attempt was cancelled. Here are your options to install Podman:

---

## **Option 1: Download and Install Manually (Recommended)**

### Step 1: Download Podman Desktop
1. Go to: https://podman-desktop.io/
2. Click **Download** button
3. Download **"Podman Desktop for Windows"**
4. File will be `podman-desktop-x.x.x.exe`

### Step 2: Run the Installer
1. Locate the downloaded `.exe` file (usually in Downloads folder)
2. **Right-click → Run as Administrator**
3. Follow the installation wizard:
   - Accept license agreement
   - Choose installation location (default is fine)
   - Check "Install WSL 2" (if not already installed)
   - Click **Install**
4. **Restart your computer** when prompted

### Step 3: Verify Installation
Open PowerShell and run:
```powershell
podman --version
podman-compose --version
```

---

## **Option 2: Retry Automated Installation**

Try again with Administrator PowerShell:

```powershell
# Run PowerShell as Administrator, then:
winget install RedHat.Podman
```

When prompted:
- ✅ Accept the license
- ✅ Allow any system changes
- ✅ Do NOT cancel the installer

---

## **Option 3: Install via Chocolatey**

If you have Chocolatey installed:

```powershell
# Run PowerShell as Administrator
choco install podman
```

---

## **Option 4: Install Podman Components Separately**

If the GUI installer doesn't work, install command-line only:

### 4a. Install WSL 2 First
```powershell
# Open PowerShell as Administrator
wsl --install
wsl --update
# Restart computer
```

### 4b. Download Podman Directly
1. Go to: https://github.com/containers/podman/releases
2. Download latest: `podman-x.x.x-windows-amd64.msi`
3. Run the `.msi` file as Administrator

### 4c. Install podman-compose
```powershell
# After Podman is installed
pip install podman-compose
```

---

## **What You'll Get**

After installation:
- ✅ Podman CLI tool
- ✅ Podman Machine (virtual machine)
- ✅ podman-compose (for docker-compose compatibility)
- ✅ Podman Desktop (optional GUI)

---

## **Next Steps After Installation**

Once Podman is installed:

```powershell
# 1. Start Podman machine
podman machine start

# 2. Navigate to project
cd c:\Users\Yadavendra.1.Yadav\cs-experiments\snappay-poc

# 3. Build and run containers
podman-compose up --build
```

---

## **WSL 2 Requirement**

Podman on Windows requires WSL 2:

```powershell
# Check if WSL 2 is installed
wsl --list --verbose

# If not installed, install it
wsl --install

# Update WSL
wsl --update

# Restart computer after installation
```

---

## **Troubleshooting Installation**

### "Access Denied" Error
- Run PowerShell **as Administrator**
- Disable antivirus temporarily if blocking
- Check UAC settings

### "WSL 2 not found"
- Install WSL 2 first: `wsl --install`
- Restart computer
- Then install Podman

### "Installation failed"
- Check installation log: `C:\Users\[YourUsername]\AppData\Local\Packages\Microsoft.DesktopAppInstaller_8wekyb3d8bbwe\LocalState\DiagOutputDir\`
- Try alternative installation method
- Update Windows to latest version

### Podman command still not found after installation
- Restart PowerShell/Terminal
- Restart computer
- Verify PATH includes Podman installation directory

---

## **Recommended: Use Podman Desktop**

Podman Desktop provides:
- ✅ GUI for managing containers
- ✅ Automatic WSL 2 setup
- ✅ Machine status monitoring
- ✅ Container logs viewer
- ✅ Image management

Download: https://podman-desktop.io/

---

## **Support Resources**

- **Podman Docs**: https://docs.podman.io/
- **Podman Desktop**: https://podman-desktop.io/
- **Installation Guide**: https://podman-desktop.io/docs/intro
- **GitHub Releases**: https://github.com/containers/podman/releases

---

## **I'm Ready to Help**

Once you've installed Podman:
1. Let me know
2. I'll help verify the installation
3. We'll run `podman-compose up --build`
4. Your services will be running! 🚀
