param(
    [int]$TimeoutSeconds = 20,
    [string]$AssemblyPath = "src/FrameTheory.Desktop/bin/Release/net10.0/FrameTheory.Desktop.dll"
)

$ErrorActionPreference = "Stop"

$target = (Resolve-Path -LiteralPath $AssemblyPath).Path
$arguments = @()
if ([IO.Path]::GetExtension($target) -eq ".dll") {
    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue
    $dotnet = if ($dotnetCommand) {
        $dotnetCommand.Source
    } else {
        $userDotnet = Join-Path $env:USERPROFILE ".dotnet/dotnet.exe"
        if (-not (Test-Path -LiteralPath $userDotnet -PathType Leaf)) {
            throw "The .NET host executable was not found on PATH or at $userDotnet."
        }

        $userDotnet
    }
    $executable = $dotnet
    $arguments = @($target)
} else {
    $executable = $target
}

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$process = Start-Process -FilePath $executable -ArgumentList $arguments -WorkingDirectory (Split-Path $target) -PassThru
$deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
$root = [System.Windows.Automation.AutomationElement]::RootElement
$windowNameCondition = New-Object System.Windows.Automation.PropertyCondition(
    [System.Windows.Automation.AutomationElement]::NameProperty,
    "Frame Theory Studio AI")
$window = $null

try {
    function Find-NamedControl([string]$name) {
        $candidates = if ($controlAliases.ContainsKey($name)) { $controlAliases[$name] } else { @($name) }
        while ([DateTime]::UtcNow -lt $uiDeadline) {
            foreach ($candidate in $candidates) {
                $condition = New-Object System.Windows.Automation.PropertyCondition(
                    [System.Windows.Automation.AutomationElement]::NameProperty,
                    $candidate)
                $control = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
                if ($null -ne $control) {
                    return $control
                }
            }

            Start-Sleep -Milliseconds 100
        }

        return $null
    }

    function Expand-NamedSection([string]$name) {
        $section = Find-NamedControl $name
        if ($null -eq $section) {
            throw "Required inspector section was not found: $name."
        }

        $expandCollapse = $null
        if ($section.TryGetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern, [ref]$expandCollapse)) {
            if ($expandCollapse.Current.ExpandCollapseState -eq [System.Windows.Automation.ExpandCollapseState]::Collapsed) {
                $expandCollapse.Expand()
            }

            return
        }

        $invoke = $null
        if ($section.TryGetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern, [ref]$invoke)) {
            $invoke.Invoke()
            return
        }

        throw "Inspector section did not expose an expand/collapse or invoke pattern: $name."
    }

    while ([DateTime]::UtcNow -lt $deadline -and $null -eq $window) {
        if ($process.HasExited) {
            throw "Desktop UI process exited before exposing its window (exit code $($process.ExitCode))."
        }

        $window = $root.FindAll([System.Windows.Automation.TreeScope]::Children, $windowNameCondition) |
            Where-Object { $_.Current.ProcessId -eq $process.Id } |
            Select-Object -First 1
        if ($null -eq $window) {
            Start-Sleep -Milliseconds 250
        }
    }

    if ($null -eq $window) {
        throw "Desktop UI window was not found within $TimeoutSeconds seconds."
    }

    $uiDeadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)

    $bounds = $window.Current.BoundingRectangle
    if ($bounds.Width -lt 960 -or $bounds.Height -lt 640) {
        throw "Desktop window launch bounds were below the scaled-safe minimum: $($bounds.Width)x$($bounds.Height)."
    }

    $transformPattern = $null
    if (-not $window.TryGetCurrentPattern([System.Windows.Automation.TransformPattern]::Pattern, [ref]$transformPattern)) {
        throw "Desktop window does not expose the resize pattern needed for adaptive-layout smoke."
    }

    $transformPattern.Resize(960, 640)
    Start-Sleep -Milliseconds 500
    $minimumBounds = $window.Current.BoundingRectangle
    if ($minimumBounds.Width -lt 960 -or $minimumBounds.Height -lt 640) {
        throw "Desktop window could not retain its supported minimum bounds: $($minimumBounds.Width)x$($minimumBounds.Height)."
    }

    $workspacesCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        "Workspaces")
    $workspaces = $window.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $workspacesCondition)
    if ($null -eq $workspaces) {
        throw "Workspace tab strip was not exposed through UI Automation."
    }

    $tabCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::TabItem)
    $tabs = $window.FindAll([System.Windows.Automation.TreeScope]::Descendants, $tabCondition)
    $expectedTabs = @("Video workspace", "Photo workspace", "Color workspace", "AI workspace")
    $actualTabs = @($tabs | ForEach-Object { $_.Current.Name })
    if (@($actualTabs | Where-Object { $_ -notin $expectedTabs }).Count -gt 0 -or
        @($expectedTabs | Where-Object { $_ -notin $actualTabs }).Count -gt 0) {
        throw "Workspace tabs did not match the expected set: $($actualTabs -join ', ')."
    }

    Add-Type -AssemblyName System.Windows.Forms
    for ($shortcut = 1; $shortcut -le $expectedTabs.Count; $shortcut++) {
        $window.SetFocus()
        [System.Windows.Forms.SendKeys]::SendWait("^$shortcut")
        Start-Sleep -Milliseconds 250
        $selectedAfterShortcut = @($tabs | Where-Object {
            $_.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Current.IsSelected
        }) | Select-Object -First 1
        $expectedWorkspace = $expectedTabs[$shortcut - 1]
        if ($null -eq $selectedAfterShortcut -or $selectedAfterShortcut.Current.Name -ne $expectedWorkspace) {
            throw "Ctrl+$shortcut did not select the $expectedWorkspace workspace."
        }
    }

    $controlsByTab = @{
        "Video workspace" = @(
            "Imported media",
            "Search imported media",
            "Clear media search",
            "Timeline tracks",
            "Timeline selection inspector",
            "Timeline operation status",
            "Preview current frame",
            "Timeline preview performance status",
            "Play timeline",
            "Stop playback",
            "Quick save timeline project",
            "Quick export timeline MP4",
            "Timeline action status",
            "Timeline playhead frame",
            "Select at playhead",
            "Import verified transcript JSON",
            "Transcribe with local Whisper",
            "Save timeline project",
            "Open timeline project",
            "Export composite MP4"
        )
        "Photo workspace" = @("Open image", "Open photo project")
        "Color workspace" = @("Before", "After", "Analyze approved visual reference", "Apply reviewed reference grade")
        "AI workspace" = @("Analyze approved samples with Gemini", "Validate AI proposal", "Apply approved AI actions", "Undo last AI apply", "Ask Gemini to revise proposal", "Gemini activity log")
    }
    $controlAliases = @{
        "Preview current frame" = @("Preview current timeline frame", "Preview current frame")
        "Play timeline" = @("Play timeline preview", "Play timeline")
        "Stop playback" = @("Stop timeline preview", "Stop playback")
        "Select at playhead" = @("Select video clip at playhead", "Select at playhead")
        "Export composite MP4" = @("Export timeline MP4", "Export composite MP4")
        "Timeline selection inspector" = @("Timeline selection inspector", "Timeline selection context")
        "Timeline action status" = @("Timeline action status", "Quick save status", "Quick export status")
        "Before" = @("Color grade before preview", "Before")
        "After" = @("Color grade after preview", "After")
    }
    $sectionsByTab = @{
        "Video workspace" = @("Titles and captions section", "Project and export section")
        "Color workspace" = @("Reference match section", "Scopes section")
        "AI workspace" = @("Activity section")
    }
    $observedControls = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($tab in $tabs) {
        $selection = $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        $selection.Select()
        Start-Sleep -Milliseconds 500
        $selected = $tab.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern)
        if (-not $selected.Current.IsSelected) {
            throw "Workspace tab selection did not stick: $($tab.Current.Name)."
        }

        foreach ($sectionName in $sectionsByTab[$tab.Current.Name]) {
            Expand-NamedSection $sectionName
            Start-Sleep -Milliseconds 150
        }

        foreach ($name in $controlsByTab[$tab.Current.Name]) {
            if ($null -eq (Find-NamedControl $name)) {
                throw "Required UI automation control was not found in $($tab.Current.Name): $name."
            }

            $observedControls.Add($name) | Out-Null
        }
    }

    if ($null -eq (Find-NamedControl "Active workspace status")) {
        throw "Required UI automation control was not found: Active workspace status."
    }

    $refresh = Find-NamedControl "Refresh capability check"
    if ($null -eq $refresh) {
        throw "Required UI automation control was not found: Refresh capability check."
    }

    $refresh.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke()
    Write-Output "Desktop UI automation passed: bounds=$($bounds.Width)x$($bounds.Height), minimum=$($minimumBounds.Width)x$($minimumBounds.Height), tabs=$($tabs.Count), controls=$($observedControls.Count)."
} finally {
    if (-not $process.HasExited) {
        Stop-Process -Id $process.Id -Force
        $process.WaitForExit(5000)
    }
}
