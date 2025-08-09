# Manual QA Testing Checklist for NullInstaller

## Prerequisites
- [ ] Clean Windows 11 VM prepared
- [ ] VM has internet connectivity
- [ ] Administrator access available
- [ ] Test installers downloaded/prepared
- [ ] Screen recording software ready (optional)

## VM Preparation
1. [ ] Create VM snapshot before testing
2. [ ] Disable Windows Defender temporarily (for testing only)
3. [ ] Clear all previous installations
4. [ ] Document VM specifications (RAM, CPU, Storage)

## Pre-Installation Tests

### 1. Application Launch
- [ ] Double-click NullInstaller.exe
- [ ] Verify application starts within 3 seconds
- [ ] Check no error dialogs appear
- [ ] Confirm dark theme loads correctly
- [ ] Verify window size is 1200x750

### 2. UI Responsiveness
- [ ] All buttons are clickable
- [ ] Tab switching works smoothly
- [ ] Scrollbars appear when needed
- [ ] Window can be resized properly
- [ ] Close button works correctly

## Core Functionality Tests

### 3. Local Files Tab
- [ ] Drag and drop single .exe file
  - [ ] File appears in list
  - [ ] File size displayed correctly
  - [ ] Status shows "Ready"
- [ ] Drag and drop multiple files
  - [ ] All files appear
  - [ ] No duplicates when same file added twice
- [ ] Drag and drop .msi file
  - [ ] MSI recognized correctly
- [ ] Test with invalid file types
  - [ ] .txt files rejected
  - [ ] .pdf files rejected

### 4. Software Catalog Tab
- [ ] Categories display correctly
- [ ] Each category shows appropriate software
- [ ] Checkbox selection works
- [ ] Download URLs are valid (hover to see)
- [ ] Select All button works
- [ ] Deselect All button works

### 5. Download Functionality
- [ ] Select 3 programs from catalog
- [ ] Click "Download Selected"
- [ ] Progress bar updates during download
- [ ] Downloaded files appear in Local Files tab
- [ ] File sizes are reasonable
- [ ] Cancel download mid-process works

## Installation Tests

### 6. Basic Installation
- [ ] Add Chrome installer
- [ ] Check the checkbox
- [ ] Click "Start Installation"
- [ ] Progress bar shows progress
- [ ] Status changes to "Installing..."
- [ ] Status changes to "Completed" when done
- [ ] Chrome actually installed (check Start Menu)

### 7. Multiple Installations
- [ ] Add 5 different installers
- [ ] Select all
- [ ] Start installation
- [ ] All install sequentially
- [ ] Progress bar updates for each
- [ ] Individual statuses update correctly
- [ ] All programs actually installed

### 8. Silent Installation
- [ ] Enable verbose logging
- [ ] Install with "Show Output" unchecked
- [ ] No installer dialogs appear
- [ ] Installation completes silently
- [ ] Check install_log.txt for details

### 9. MSI Installation
- [ ] Add .msi installer (e.g., 7-Zip.msi)
- [ ] Start installation
- [ ] MSI installs correctly
- [ ] No msiexec errors
- [ ] Program appears in Control Panel

## Error Handling Tests

### 10. Failure Scenarios
- [ ] Add corrupted installer
  - [ ] Installation fails gracefully
  - [ ] Error logged
  - [ ] Status shows "Failed"
  - [ ] Other installations continue
- [ ] Add installer requiring admin rights (non-admin mode)
  - [ ] Appropriate error message
  - [ ] Option to retry as admin
- [ ] Remove installer file during installation
  - [ ] Error handled gracefully
  - [ ] Clear error message

### 11. Timeout Handling
- [ ] Add installer that takes >2 minutes
- [ ] Installation times out
- [ ] Timeout message appears
- [ ] Process is killed properly
- [ ] Can retry installation

### 12. Retry Logic
- [ ] Force installation failure
- [ ] Retry button/option available
- [ ] Retry attempts logged
- [ ] Maximum retry limit enforced
- [ ] Final failure logged appropriately

## Post-Installation Tests

### 13. Registry Verification
- [ ] Click "Verify" button
- [ ] Installed programs detected
- [ ] Registry entries created correctly
- [ ] Uninstall entries present in Control Panel
- [ ] Check HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall

### 14. Post-Install Hooks
- [ ] Create post_install.ps1 script
- [ ] Place in NullInstaller directory
- [ ] Run installation
- [ ] Script executes after installation
- [ ] Script output logged
- [ ] Script errors handled

### 15. JetBrains Plugin Installation
- [ ] Install JetBrains IDE first
- [ ] Run JetBrains plugin installer
- [ ] Plugins directory detected
- [ ] Plugins downloaded correctly
- [ ] Plugins appear in IDE after restart
- [ ] Installation logged

## IDE Plugin Verification

### 16. IntelliJ IDEA
- [ ] Open IntelliJ after installation
- [ ] Check File -> Settings -> Plugins
- [ ] Verify installed plugins present
- [ ] Plugins are enabled
- [ ] No compatibility errors

### 17. Visual Studio Code
- [ ] Open VS Code after installation
- [ ] Check Extensions panel
- [ ] Verify extensions installed
- [ ] Extensions are enabled
- [ ] No error notifications

## Logging and Diagnostics

### 18. Log Files
- [ ] install_log.txt created
- [ ] Log contains timestamps
- [ ] All installations logged
- [ ] Errors clearly marked
- [ ] Verbose mode adds detail

### 19. Error Reporting
- [ ] Error messages are clear
- [ ] Stack traces captured (verbose mode)
- [ ] Network errors logged
- [ ] Permission errors identified

## Performance Tests

### 20. Resource Usage
- [ ] Monitor CPU usage during installation
  - [ ] Stays below 50% average
- [ ] Monitor memory usage
  - [ ] Stays below 500MB
- [ ] Monitor disk I/O
  - [ ] No excessive disk thrashing
- [ ] Application remains responsive

### 21. Large Scale Test
- [ ] Add 20+ installers
- [ ] List view performance acceptable
- [ ] Scrolling remains smooth
- [ ] Installation queue processed correctly
- [ ] No memory leaks after completion

## Cleanup Tests

### 22. Temporary Files
- [ ] Check %TEMP% folder
- [ ] Temporary files cleaned up
- [ ] No leftover installer files
- [ ] Downloaded files in correct location

### 23. Uninstallation
- [ ] Programs can be uninstalled normally
- [ ] Registry entries removed on uninstall
- [ ] No leftover files in Program Files

## Edge Cases

### 24. Special Scenarios
- [ ] Install on drive other than C:
- [ ] Install with special characters in path
- [ ] Install with very long path names
- [ ] Install with network drives
- [ ] Install with limited disk space

### 25. Compatibility
- [ ] Test on Windows 11 Home
- [ ] Test on Windows 11 Pro
- [ ] Test with Windows Defender enabled
- [ ] Test with third-party antivirus
- [ ] Test with UAC at different levels

## Final Verification

### 26. Full Stack Validation
- [ ] All selected programs launch correctly
- [ ] No missing dependencies
- [ ] Programs function as expected
- [ ] Settings/preferences preserved
- [ ] Auto-update mechanisms work

### 27. Documentation Check
- [ ] README accurate
- [ ] Help text clear
- [ ] Error messages helpful
- [ ] Version number correct

## Sign-off

| Item | Status | Notes |
|------|--------|-------|
| Functional Testing | ⬜ Pass / ⬜ Fail | |
| Performance Testing | ⬜ Pass / ⬜ Fail | |
| Error Handling | ⬜ Pass / ⬜ Fail | |
| Plugin Installation | ⬜ Pass / ⬜ Fail | |
| Documentation | ⬜ Pass / ⬜ Fail | |

**Tested By:** ___________________  
**Date:** ___________________  
**VM Environment:** ___________________  
**NullInstaller Version:** ___________________  

## Issues Found

1. **Issue:** ___________________
   - **Severity:** High / Medium / Low
   - **Steps to Reproduce:** ___________________
   - **Expected Result:** ___________________
   - **Actual Result:** ___________________

2. **Issue:** ___________________
   - **Severity:** High / Medium / Low
   - **Steps to Reproduce:** ___________________
   - **Expected Result:** ___________________
   - **Actual Result:** ___________________

## Recommendations

- [ ] ___________________
- [ ] ___________________
- [ ] ___________________
