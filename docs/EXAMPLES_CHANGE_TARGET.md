# Examples: Change Target Framework with Multiple Selection

## Overview
This document provides practical examples of the enhanced "Change Target Framework" functionality that now works with multiple selected projects of the same type.

---

## Example 1: Upgrading Multiple SDK Projects with Different Current Targets

### Scenario
You have a solution with 5 SDK-style projects targeting different versions of .NET:
- ProjectA.csproj → net6.0
- ProjectB.csproj → net7.0
- ProjectC.csproj → net8.0
- ProjectD.csproj → net6.0-windows
- ProjectE.csproj → net7.0-windows

### Goal
Upgrade all projects to net9.0 or net9.0-windows

### Steps
1. Open CsProjConverter and scan the solution folder
2. Select all 5 SDK-style projects (Ctrl+Click or Shift+Click)
3. **Observe:** "Change target framework" button is **ENABLED** ✅
4. **Observe:** Target framework combo box is **EMPTY** (because current TFMs differ)
5. **Observe:** Tooltip says: "Replace the target framework for all selected projects with different current targets"
6. Enter "net9.0" in the target framework combo box
7. Click "Change target framework"
8. Confirm the operation in the dialog
9. **Result:** All 5 projects are updated to "net9.0" or "net9.0-windows" (for WinForms)

### Before
```
ProjectA.csproj:
  <TargetFramework>net6.0</TargetFramework>

ProjectB.csproj:
  <TargetFramework>net7.0</TargetFramework>

ProjectC.csproj:
  <TargetFramework>net8.0</TargetFramework>
```

### After
```
ProjectA.csproj:
  <TargetFramework>net9.0</TargetFramework>

ProjectB.csproj:
  <TargetFramework>net9.0</TargetFramework>

ProjectC.csproj:
  <TargetFramework>net9.0</TargetFramework>
```

---

## Example 2: Standardizing Old-Style Projects

### Scenario
You have legacy projects targeting different .NET Framework versions:
- LegacyLib1.csproj → v4.7.2
- LegacyLib2.csproj → v4.6.1
- LegacyLib3.csproj → v4.8

### Goal
Standardize all to v4.8

### Steps
1. Scan the folder containing these projects
2. Select all 3 Old-style projects
3. **Observe:** "Change target framework" button is **ENABLED** ✅
4. **Observe:** Combo box is empty (different current targets)
5. Enter "v4.8" in the combo box
6. Click "Change target framework"
7. Confirm the operation
8. **Result:** All 3 projects now target v4.8

---

## Example 3: Mixed Project Types - Operation Blocked

### Scenario
You accidentally select both SDK and Old-style projects:
- ModernApp.csproj → net8.0 (SDK-style)
- LegacyLib.csproj → v4.8 (Old-style)

### What Happens
1. Select both projects
2. **Observe:** "Change target framework" button is **DISABLED** ❌
3. **Observe:** Combo box is also disabled
4. **Observe:** Tooltip says: "Cannot change target: selected projects have different project types (SDK and Old-style)"
5. **No action possible** - Prevents accidental invalid changes

### Why This is Important
- Old-style projects use `TargetFrameworkVersion` (e.g., v4.8)
- SDK-style projects use `TargetFramework` (e.g., net8.0)
- These are incompatible formats
- Safety check prevents errors

---

## Example 4: Multiple Projects with Same Target (Original Behavior)

### Scenario
All selected projects already have the same target:
- ServiceA.csproj → net8.0
- ServiceB.csproj → net8.0
- ServiceC.csproj → net8.0

### Steps
1. Select all 3 projects
2. **Observe:** Button is **ENABLED** ✅
3. **Observe:** Combo box shows "net8.0" (pre-filled with common value)
4. **Observe:** Tooltip says: "Replace the target framework for all selected projects"
5. Change to "net9.0"
6. Click "Change target framework"
7. **Result:** All 3 projects updated to net9.0

**Note:** This behavior is unchanged from the original implementation.

---

## Example 5: Multi-Targeting Scenario

### Scenario
You have projects with multiple target frameworks:
- CrossPlatformLib.csproj → net6.0;net8.0
- UtilityLib.csproj → net7.0;net8.0

### Goal
Standardize all to net8.0;net9.0

### Steps
1. Select both SDK-style projects
2. **Observe:** Button is **ENABLED** ✅
3. **Observe:** Combo box is empty (different TFM sets)
4. Enter "net8.0;net9.0"
5. Click "Change target framework"
6. **Result:** Both projects now target net8.0;net9.0

### Before
```
CrossPlatformLib.csproj:
  <TargetFrameworks>net6.0;net8.0</TargetFrameworks>

UtilityLib.csproj:
  <TargetFrameworks>net7.0;net8.0</TargetFrameworks>
```

### After
```
CrossPlatformLib.csproj:
  <TargetFrameworks>net8.0;net9.0</TargetFrameworks>

UtilityLib.csproj:
  <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
```

---

## Example 6: Large Batch Operation

### Scenario
Solution with 50 SDK-style projects, all targeting different versions (net6.0, net7.0, net8.0 mix)

### Goal
Upgrade entire solution to net9.0

### Steps
1. Scan the solution folder
2. Use filter to find SDK-style projects (if needed)
3. Select all SDK-style projects (Ctrl+A if all visible are SDK)
4. **Observe:** Button is **ENABLED** ✅
5. Enter "net9.0"
6. Click "Change target framework"
7. Confirm for all 50 projects
8. **Result:** Progress dialog shows success/failure for each project
9. All successful projects show ✓ in the "Changed" column
10. Grid cells highlight in green

### Efficiency Gain
- **Before:** Change 50 projects one at a time (50 selections + 50 confirmations)
- **After:** Change all 50 projects in one operation (1 selection + 1 confirmation)
- **Time Saved:** ~95% reduction in effort!

---

## Example 7: Selective Update After Filtering

### Scenario
Solution with 30 projects, mix of SDK and Old-style, various targets

### Goal
Update only the SDK projects in a specific folder

### Steps
1. Scan the solution
2. Use the "Filter by path" textbox to enter folder name (e.g., "Services")
3. **Result:** Grid shows only projects in Services folder
4. Select all visible SDK-style projects (check Style column)
5. Button is **ENABLED** if all selected are SDK
6. Enter new target framework
7. Apply change
8. **Result:** Only the filtered SDK projects in Services folder are updated

---

## Common Patterns

### Pattern 1: Solution-Wide Upgrade
```
Goal: Upgrade entire solution from net8.0 to net9.0

1. Scan solution
2. Filter or select all SDK projects
3. Verify all selected have same project type
4. Enter "net9.0"
5. Apply change
```

### Pattern 2: Incremental Migration
```
Goal: Migrate old-style projects to SDK, then upgrade

Phase 1:
1. Select old-style projects
2. Convert to SDK (using Convert button)

Phase 2:
1. Select newly converted SDK projects
2. Change target to modern version (e.g., net9.0)
```

### Pattern 3: Multi-Framework Alignment
```
Goal: Align all libraries to support net8.0;net9.0

1. Select all SDK library projects
2. Enter "net8.0;net9.0"
3. Apply change
4. Result: All libraries support both frameworks
```

---

## Tips and Best Practices

### Tip 1: Use Filters Effectively
- Filter by path to target specific folders
- Example: "Core" to find all core libraries
- Select filtered results for batch operations

### Tip 2: Check Project Style First
- Always verify the Style column before selecting
- Mix of SDK and Old-style will disable the button
- Group similar projects for batch operations

### Tip 3: Test with Small Batch First
- For large solutions (50+ projects)
- Test with 2-3 projects first
- Verify the change works as expected
- Then apply to full set

### Tip 4: Use Version Control
- Commit before making batch changes
- Easy rollback if needed
- Can review changes with git diff

### Tip 5: Handle Errors Gracefully
- Check the results dialog
- Some projects might fail (read-only, locked)
- Fix issues and retry failed projects
- Success count shows progress

---

## Keyboard Shortcuts for Efficiency

- **Ctrl+Click** - Add individual projects to selection
- **Shift+Click** - Select range of projects
- **Ctrl+A** - Select all visible (filtered) projects
- **Enter** - In combo box, applies change (if button enabled)

---

## Summary

The enhanced "Change Target Framework" feature provides significant efficiency improvements:

✅ Works with multiple projects of same type
✅ Handles different current target frameworks
✅ Smart pre-filling when targets match
✅ Clear user feedback via tooltips
✅ Safety checks for mixed project types
✅ Supports large batch operations
✅ Integrates seamlessly with filtering

This makes solution-wide framework upgrades fast and safe!
