#!/usr/bin/env python3
"""Documentation-stage repository validation. Does NOT claim to test the application."""
from __future__ import annotations
import json
import hashlib
import re
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
REQUIRED = [
    "README.md", "AGENTS.md", "docs/PRODUCT_REQUIREMENTS.md",
    "docs/ARCHITECTURE.md", "docs/AI_EDIT_PLAN.md", "docs/COLOR_PIPELINE.md",
    "docs/MEDIA_ENGINE.md", "docs/UX_DESIGN.md", "docs/SECURITY_PRIVACY.md",
    "docs/BRAND_ASSETS.md", "docs/ROADMAP.md", "docs/EPICS_AND_TASKS.md",
    "docs/TEST_STRATEGY.md", "docs/CODEX_MASTER_PROMPT.md",
    "docs/IMPLEMENTATION_STATUS.md", "docs/RELEASE_AND_CI.md",
    "docs/DEPENDENCIES_AND_LICENSES.md", "docs/GEMINI_PROVIDER.md",
    "docs/adr/0008-caption-transcription-engine.md",
    "schemas/ai-edit-plan.v1.schema.json"
]

def _p0_tasks(epics: str) -> list[str]:
    tasks = []
    p0_section = False
    for line in epics.splitlines():
        if line.startswith("## "):
            p0_section = re.search(r"\[P0(?:,|\])", line) is not None
        if p0_section:
            tasks.extend(re.findall(r"\*\*(FT-\d+)\*\*", line))
    return sorted(set(tasks))

def _audit_p0_status(epics: str, status: str, audit: str) -> list[str]:
    p0_tasks = _p0_tasks(epics)
    status_entries = {
        task: marker
        for marker, task in re.findall(r"^- \[([ x~])\] (FT-\d+)\b", status, re.MULTILINE)
    }
    missing_p0_status = sorted(set(p0_tasks) - status_entries.keys())
    assert not missing_p0_status, f"P0 tasks missing implementation status: {', '.join(missing_p0_status)}"
    partial_p0 = sorted(task for task in p0_tasks if status_entries[task] == "~")
    blocker_section = audit.split("## P0 blockers before V1", 1)[-1]
    missing_blockers = [task for task in partial_p0 if task not in blocker_section]
    assert not missing_blockers, f"Partial P0 tasks missing V1 blocker evidence: {', '.join(missing_blockers)}"
    assert "V1 remains **not complete**" in audit, "Acceptance audit must not claim V1 completion while blockers remain"
    return partial_p0

def _audit_p0_parser_self_check() -> None:
    epics = "## E00 [P0]\n- **FT-001**\n## E01 [P0, depends E00]\n- **FT-002**\n## E11 [P1]\n- **FT-1101**"
    status = "- [~] FT-001\n- [x] FT-002"
    audit = "## P0 blockers before V1\nFT-001\nV1 remains **not complete**"
    assert _p0_tasks(epics) == ["FT-001", "FT-002"]
    assert _audit_p0_status(epics, status, audit) == ["FT-001"]

def run() -> int:
    _audit_p0_parser_self_check()
    absent = [p for p in REQUIRED if not (ROOT / p).is_file()]
    if absent:
        print("Missing required paths:", *absent, sep="\n- ", file=sys.stderr)
        return 1
    p = ROOT / "schemas/ai-edit-plan.v1.schema.json"
    spec = json.loads(p.read_text(encoding="utf-8"))
    assert spec["properties"]["schemaVersion"]["const"] == "1.0"
    assert "actions" in spec["required"] and spec["$defs"]["action"]["additionalProperties"] is False
    assert len(spec["$defs"]["action"]["properties"]["type"]["enum"]) >= 8
    agent = (ROOT/"AGENTS.md").read_text(encoding="utf-8")
    for term in ["transaction", "Gemini", "undo", "offline"]:
        assert term.lower() in agent.lower(), f"Missing instruction: {term}"
    ci = (ROOT/".github/workflows/ci.yml").read_text(encoding="utf-8")
    assert "detach_dmg()" in ci and "for attempt in 1 2 3 4 5" in ci, "macOS DMG smoke must retry detach cleanup"
    macos_packager = (ROOT / "scripts/package_macos_dmg.sh").read_text(encoding="utf-8")
    assert "dmg_path=" in macos_packager and "for attempt in 1 2 3 4 5" in macos_packager, "macOS DMG creation must retry transient hdiutil failures"
    assert "Avalonia.Headless" in ci and "Avalonia.Headless' -and $_.licenseExpression -eq 'MIT'" in ci, "CI must verify headless test-package license metadata"
    assert "Test headless desktop shell" in ci and "FullyQualifiedName~FoundationTests" in ci, "CI must expose the headless desktop shell test"
    assert "$missingLicenseMetadata" in ci and "metadataFound" in ci, "CI must fail closed on missing NuGet license metadata"
    ui_smoke = (ROOT / "scripts/smoke_desktop_ui.ps1").read_text(encoding="utf-8")
    assert "960" in ui_smoke and "640" in ui_smoke, "Native UI smoke must retain its scaled-safe minimum bounds"
    assert ("SendControlDigit" in ui_smoke or 'SendWait("^$shortcut")' in ui_smoke) and "$expectedTabs.Count" in ui_smoke, "Native UI smoke must exercise every workspace shortcut"
    assert "Expand-NamedSection" in ui_smoke, "Native UI smoke must open collapsed inspector sections before control checks"
    for section_name in ["Titles and captions section", "Reference match section", "Scopes section", "Activity section"]:
        assert section_name in ui_smoke, f"Native UI smoke must cover {section_name}"
    assert "Active workspace status" in ui_smoke, "Native UI smoke must verify the live workspace status region"
    sbom = (ROOT / "docs/SBOM.md").read_text(encoding="utf-8")
    assert "Avalonia.Headless" in sbom and "CI fails closed" in sbom, "SBOM must document the headless package and metadata guard"
    solution = (ROOT/"FrameTheory.sln").read_text(encoding="utf-8")
    assert "FrameTheory.LocalWhisperSmoke" in solution, "Local Whisper acceptance smoke must remain in the solution"
    development = (ROOT/"docs/DEVELOPMENT.md").read_text(encoding="utf-8")
    assert "FrameTheory.LocalWhisperSmoke" in development, "Local Whisper acceptance command must remain documented"
    epics = (ROOT / "docs/EPICS_AND_TASKS.md").read_text(encoding="utf-8")
    status = (ROOT / "docs/IMPLEMENTATION_STATUS.md").read_text(encoding="utf-8")
    audit = (ROOT / "docs/V1_ACCEPTANCE_AUDIT.md").read_text(encoding="utf-8")
    partial_p0 = _audit_p0_status(epics, status, audit)
    print(f"V1 acceptance audit: BLOCKED by {len(partial_p0)} tracked partial P0 task(s); no unsupported completion claim detected.")
    path_security = (ROOT / "src/FrameTheory.Domain/LocalPathSecurity.cs").read_text(encoding="utf-8")
    assert "ContainsReparsePoint" in path_security, "Shared local path security guard must remain present"
    for relative_path in [
        "src/FrameTheory.Media/MediaProbe.cs",
        "src/FrameTheory.Media/FrameDecoder.cs",
        "src/FrameTheory.Media/MediaImport.cs",
        "src/FrameTheory.Media/TimelinePreview.cs",
        "src/FrameTheory.Media/AudioSourcePreparer.cs",
        "src/FrameTheory.Media/PreviewProxy.cs",
        "src/FrameTheory.Media/FfmpegExecutableLocator.cs",
        "src/FrameTheory.AI/GeminiInteractions.cs",
        "src/FrameTheory.Media/MediaExport.cs",
        "src/FrameTheory.Media/TimelineCompositor.cs",
        "src/FrameTheory.Photo/PhotoExport.cs",
        "src/FrameTheory.Captions/Transcription.cs",
        "src/FrameTheory.Captions/LocalWhisperTranscription.cs",
        "src/FrameTheory.Storage/CacheIndex.cs",
        "src/FrameTheory.AI/AiRecipeStore.cs",
        "src/FrameTheory.Color/ColorPreset.cs",
        "src/FrameTheory.Desktop/WorkspaceStateStore.cs",
        "src/FrameTheory.Desktop/PhotoEditorViewModel.cs",
        "src/FrameTheory.Desktop/BrandAssetCatalog.cs",
        "src/FrameTheory.Desktop/MainWindowViewModel.cs",
        "src/FrameTheory.Application/Diagnostics.cs",
        "src/FrameTheory.Platform/SystemCapabilityDetector.cs",
        "src/FrameTheory.Media/PreviewProxy.cs",
    ]:
        source = (ROOT / relative_path).read_text(encoding="utf-8")
        assert "LocalPathSecurity.ContainsReparsePoint" in source, f"{relative_path} must use shared local path security"
    for source_path in (ROOT / "src").rglob("*.cs"):
        if source_path == ROOT / "src/FrameTheory.Domain/LocalPathSecurity.cs":
            continue
        source = source_path.read_text(encoding="utf-8")
        assert "FileAttributes.ReparsePoint" not in source, f"{source_path.relative_to(ROOT)} must use shared local path security"
        assert "&& LocalPathSecurity.ContainsReparsePoint" not in source, f"{source_path.relative_to(ROOT)} must inspect missing and dangling paths"
    asset = ROOT / "assets/brand/frame-theory-original.png"
    assert asset.is_file() and not asset.is_symlink(), "Approved Frame Theory logo asset must be a regular file"
    assert hashlib.sha256(asset.read_bytes()).hexdigest().upper() == "0BD29D37FBCC4C753B8870A65E3A02E23460C1A5C305E53E6CD2D9D2F151F388", "Approved Frame Theory logo checksum changed"
    print("Documentation and edit-plan schema syntax checks passed.")
    print("NOTE: Application code, installers and media exports have not been tested by this script.")
    return 0

if __name__ == "__main__":
    raise SystemExit(run())
