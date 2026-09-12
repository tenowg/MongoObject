# Active Context: MongoObject

## Current State

- **Active Branch:** `fix-tracking` (up to date with origin/fix-tracking)
- **Working Tree:** Clean — no uncommitted changes
- **Last Commit:** `1aac4ca` — Add DocFX Markdown output & LLM API index generation

## Recent Work History

| Commit | Description |
|--------|-------------|
| `1aac4ca` | Add DocFX Markdown output & LLM API index generation |
| `d6a4ee5` | Improve BSON serialization safety in GenerateBsonSnapshot |
| `71a0d63` | Enhance TrackingObservableObject change tracking |
| `4762bdf` | Refactor property change tracking with BsonValue snapshots |
| `626547d` | Use QueryName for property tracking and notifications |
| `6bc4e72` | Optimize vector index processing and MongoDB connection |
| `6bc4e72` | Enable change tracking for trackable cached documents |
| `d8fb661` | Add GetById with metadata filter and refactor retrieval |
| `fa36f17` | Refactor MongoDocumentManager: improve updates, cache, search |
| `ae4b0f4` | Add sorting support and improve async/cancellation handling |

## Active Development Focus

### Property Change Tracking (Current Branch)
The `fix-tracking` branch focuses on improving the reliability of property change tracking:
- Refactored to use BsonValue snapshots for accurate change detection
- Enhanced `TrackingObservableObject` with better notification support
- Uses QueryName consistently for property tracking and notifications
- Enabled change tracking for cached documents

### Documentation Updates
- DocFX Markdown output generation
- LLM API index generation for documentation site

## Next Steps (Potential)

Based on recent commit history and active development:
1. Complete the fix-tracking branch testing and merge to master
2. Continue work from `generator-refactor-projections` branch if needed
3. Address any remaining issues related to change tracking reliability

## Important Patterns & Preferences

- **Partial properties (C# 14):** Core mechanism for generating property implementations without runtime proxies
- **BsonValue snapshots:** Used for accurate change detection in property tracking
- **QueryName-based tracking:** Property names tracked via their QueryName for consistency
- **Minimal payloads:** Only changed fields are serialized and sent to MongoDB

## Learnings & Insights

1. **Source generators require careful testing** — Debugging output syntax trees is complex; ensure tests validate generated code
2. **BsonValue handling needs care** — Serialization safety must be verified, especially with edge cases
3. **Change tracking reliability depends on consistent naming** — Using QueryName throughout prevents mismatches
