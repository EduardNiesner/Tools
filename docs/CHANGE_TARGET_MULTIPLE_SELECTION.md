# Change Target Framework - Multiple Selection Behavior

## Overview
This document describes the behavior of the "Change Target Framework" functionality when multiple projects are selected in the CsprojChecker application.

## Feature Description
The "Change Target Framework" button is now enabled when multiple projects with the same project type (all SDK or all Old-style) are selected, regardless of their current target framework values.

## Button State Logic

### Enabled Conditions
The "Change Target Framework" button is **enabled** when:
1. One or more projects are selected, AND
2. All selected projects have the same project type (all SDK-style OR all Old-style)

### Disabled Conditions
The "Change Target Framework" button is **disabled** when:
1. No projects are selected, OR
2. Selected projects have mixed project types (some SDK-style and some Old-style)

## User Experience

### Case 1: Multiple SDK-style Projects with Same Target
- **Selected:** 2+ SDK-style projects with identical target frameworks (e.g., all have "net6.0")
- **Button State:** Enabled
- **ComboBox Pre-fill:** Shows the common target framework ("net6.0")
- **Tooltip:** "Replace the target framework for all selected projects"
- **Action:** Changing the target updates all selected projects to the new target

### Case 2: Multiple SDK-style Projects with Different Targets
- **Selected:** 2+ SDK-style projects with different target frameworks (e.g., "net6.0" and "net8.0")
- **Button State:** Enabled ✨ **NEW BEHAVIOR**
- **ComboBox Pre-fill:** Empty (user must enter desired target)
- **Tooltip:** "Replace the target framework for all selected projects with different current targets"
- **Action:** Changing the target updates all selected projects to the new target

### Case 3: Multiple Old-style Projects with Different Targets
- **Selected:** 2+ Old-style projects with different target frameworks (e.g., "v4.7.2" and "v4.8")
- **Button State:** Enabled ✨ **NEW BEHAVIOR**
- **ComboBox Pre-fill:** Empty (user must enter desired target)
- **Tooltip:** "Replace the target framework for all selected projects with different current targets"
- **Action:** Changing the target updates all selected projects to the new target

### Case 4: Mixed Project Types
- **Selected:** Mix of SDK-style and Old-style projects
- **Button State:** Disabled
- **ComboBox:** Disabled and empty
- **Tooltip:** "Cannot change target: selected projects have different project types (SDK and Old-style)"
- **Action:** No changes allowed

### Case 5: Single Project
- **Selected:** 1 project (any type)
- **Button State:** Enabled
- **ComboBox Pre-fill:** Shows the current target framework
- **Tooltip:** "Replace the target framework for all selected projects"
- **Action:** Works as before (unchanged behavior)

## Implementation Details

### Modified Method
- **File:** `CsprojChecker/MainForm.cs`
- **Method:** `UpdateFrameworkOperationsState()`
- **Lines:** ~1548-1594

### Key Changes
1. **Button Enablement Logic:** Changed from requiring identical target frameworks to requiring same project type
   ```csharp
   // OLD: Enabled only if allSetsEqual (all TFMs identical)
   // NEW: Enabled if allSameProjectType (all SDK or all Old-style)
   bool allSameProjectType = allSdkStyle || allOldStyle;
   ```

2. **ComboBox Pre-fill:** Now conditional based on whether TFMs are identical
   - If all TFMs are identical: Pre-fill with common value
   - If TFMs differ but project type is same: Leave empty for user input

3. **Dynamic Tooltips:** Provide context-aware feedback about button state

## Testing

### Manual Test Scenarios

#### Test 1: Same Project Type, Different Targets
1. Scan a folder with multiple SDK-style projects that have different target frameworks
2. Select 2+ projects with different targets (e.g., net6.0 and net8.0)
3. **Expected:** Button is enabled, combo box is empty
4. Enter a new target (e.g., net9.0)
5. Click "Change target framework"
6. **Expected:** All selected projects are updated to net9.0

#### Test 2: Mixed Project Types
1. Select both SDK-style and Old-style projects
2. **Expected:** Button is disabled
3. Hover over button
4. **Expected:** Tooltip shows "Cannot change target: selected projects have different project types"

#### Test 3: Single Selection (Regression Test)
1. Select a single project
2. **Expected:** Button is enabled, combo box shows current target
3. Change target
4. **Expected:** Works as before (no behavior change)

### Automated Tests
- Existing tests in `FrameworkOperationsTests.cs` validate the core change target framework logic
- UI state logic is validated through code review and manual testing
- No regressions in existing 82 unit tests

## Benefits
1. **Improved Efficiency:** Users can now update target frameworks for multiple projects with different current targets in a single operation
2. **Better UX:** Clear tooltips explain why the button is disabled
3. **Consistency:** Same project type projects can be batch-updated regardless of their current targets
4. **Safety:** Mixed project types still prevented to avoid accidental incorrect updates

## Backward Compatibility
- Single project selection: No change in behavior
- Multiple projects with identical targets: No change in behavior
- Existing tests: All pass without modification
