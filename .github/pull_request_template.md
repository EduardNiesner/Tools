# Pull Request Description

## Changes Made
<!-- Describe the changes in this PR -->

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Documentation update
- [ ] Refactoring
- [ ] Other (please describe):

## Testing
<!-- Describe how you tested these changes -->

---

## For .csproj Conversion Changes

If this PR modifies the conversion logic (Old-style ↔ SDK-style), please verify:

### Conversion Reference Compliance
- [ ] Changes align with [`docs/csproj-conversion-reference.md`](../docs/csproj-conversion-reference.md)
- [ ] All conversions documented in reference are implemented correctly
- [ ] No unsupported conversions are attempted
- [ ] Constraints (PackageReference, multi-targeting, etc.) are properly enforced

### Testing Checklist
Use [`docs/testing-guide.md`](../docs/testing-guide.md) to validate:
- [ ] Framework version conversions work correctly (v4.x ↔ net4x)
- [ ] WinForms detection and `-windows` suffix handling works
- [ ] Variable tokens (e.g., `$(TargetFrameworks)`) are preserved verbatim
- [ ] Root element transformation is correct
- [ ] Properties are properly preserved/added/omitted as documented
- [ ] References are handled correctly (added/removed/preserved)
- [ ] Blocking conditions work (PackageReference, multi-target, non-.NET Framework)
- [ ] Error messages are clear and actionable
- [ ] Encoding preservation works (UTF-8, UTF-8 with BOM)

### Validation Documents
- [ ] Reviewed test cases in [`docs/csproj-conversion-reference.md`](../docs/csproj-conversion-reference.md) Section 10
- [ ] Used [`docs/conversion-checklist.md`](../docs/conversion-checklist.md) for validation
- [ ] All items in checklist pass

### Round-Trip Considerations
- [ ] Understand that conversions are lossy (not round-trip safe)
- [ ] Verified no unexpected data loss
- [ ] Documented any new limitations or caveats

---

## General Checklist
- [ ] Code builds without errors
- [ ] Code follows existing style and conventions
- [ ] Comments added where code is non-obvious
- [ ] README updated if needed
- [ ] Documentation updated if needed

## Additional Notes
<!-- Any additional information, context, or screenshots -->
