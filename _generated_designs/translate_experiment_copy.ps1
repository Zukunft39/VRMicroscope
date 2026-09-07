$ErrorActionPreference = 'Stop'
$path = 'Assets/m_Scripts/Experiment/SNOMDemonstrationController.cs'
$source = [IO.File]::ReadAllText((Join-Path (Get-Location) $path))
$english = @(
'This system combines THz time-domain spectroscopy with an oscillating AFM probe to map local surface response point by point.',
'Ultrashort laser pulses excite a photoconductive emitter. A receiving antenna samples the returning THz electric field.',
'Mirrors guide and focus the THz beam onto the tip. The sharp metal apex confines the interaction to a nanoscale region.',
'The metal tip probes local surface properties. Tip radius strongly influences lateral resolution.',
'A laser reflected from the cantilever reaches a quadrant detector. Feedback controls the tip-sample distance.',
'The oscillating metal tip concentrates the electric field near the surface. The displayed motion is exaggerated for clarity.',
'The detector receives both local tip scattering and far-field background. Signal processing separates the near-field contribution.',
'Demodulation at higher harmonics of the tapping frequency suppresses background and isolates the local response.',
'The system scans successive rows, recording surface height and near-field response at each position.',
'Topography shows surface height. Near-field amplitude and phase reveal local optical properties; spectra show their frequency dependence.',
'Converts ultrashort optical pulses into broadband THz radiation. Assigned as the emitter in this demonstration.',
'Samples the returning THz electric field. Assigned as the receiver in this demonstration.',
'Redirects the THz beam along the optical path.',
'Focuses incident THz radiation or collects scattered radiation near the probe.',
'Concentrates the field at its apex and probes local surface properties during tapping.',
'Illuminates the cantilever to measure deflection for AFM distance feedback.',
'Measures displacement of the reflected laser spot and provides the AFM feedback signal.',
'Controls relative tip-sample position for surface mapping.',
'Amplifies weak electrical signals before further processing.',
'Optional detection module. Its specific role is not assigned in this demonstration.',
'Mechanical or electrical interface for the optional detector module.',
'Small-radius tip for fine surface detail. Requires careful distance control.',
'General-purpose tip for balanced spatial detail and stable scanning.',
'Larger-radius tip for robust scanning, with reduced spatial detail.',
'Choose a probe for the required surface detail. The 3x, 2x and 1x labels indicate relative detail, not optical magnification.',
'The probe is installed. Start the system to begin the automatic measurement sequence.',
'{option.displayName} is installed. Start the system to begin the automatic measurement sequence.'
)
$pattern = '"[^"\r\n]*[\u4e00-\u9fff][^"\r\n]*"'
$lines = $source -split "(?<=`n)"
$index = 0
for ($i=0; $i -lt $lines.Length; $i++) {
    if ($lines[$i] -match $pattern -and $lines[$i] -notmatch 'Resolve\(|ResolveContains\(|FindAllDescendants\(') {
        if ($index -ge $english.Length) { throw 'Unexpected additional Chinese text' }
        $replacement = '"' + $english[$index] + '"'
        $lines[$i] = [regex]::Replace($lines[$i], $pattern, $replacement)
        $index++
    }
}
if ($index -ne $english.Length) { throw "Expected $($english.Length) messages, found $index" }
[IO.File]::WriteAllText((Join-Path (Get-Location) $path), ($lines -join ''), [Text.UTF8Encoding]::new($false))

$path = 'Assets/Scene/Scene_Laboratory/MainScene_Labortory.unity'
$source = [IO.File]::ReadAllText((Join-Path (Get-Location) $path))
$entries = @{
 '52920580' = @('Objective Lens', 'Focuses illumination onto the specimen and collects light from it. Numerical aperture influences resolution and light collection.')
 '52920579' = @('Upper Optical Assembly', 'Houses optical elements that direct light through the microscope.')
 '52920578' = @('Stage Position Control', 'Moves the specimen horizontally to select the area for observation.')
 '52920577' = @('Microscope Support Table', 'Provides a stable platform for the microscope and its optical components.')
 '52920575' = @('Focus Adjustment', 'Changes the axial position of the specimen relative to the objective to bring features into focus.')
}
foreach ($id in $entries.Keys) {
    $pattern = '(?m)(  - partTransform: \{fileID: ' + $id + '\}\r?\n)    displayName: [^\r\n]*\r?\n    description: [^\r\n]*'
    if ([regex]::Matches($source,$pattern).Count -ne 1) { throw "Component binding missing: $id" }
    $source = [regex]::Replace($source,$pattern, ('$1    displayName: "' + $entries[$id][0] + '"' + "`r`n" + '    description: "' + $entries[$id][1] + '"'))
}
[IO.File]::WriteAllText((Join-Path (Get-Location) $path), $source, [Text.UTF8Encoding]::new($false))

$updates = @{
 'Assets/Resources/Prefab/PC.prefab' = @{ '\u8FD4\u56DE'='Back'; '\u7535\u6E90\u952E'='Power' }
 'Assets/Scenes/ModelExploderTest.unity' = @{ '\u8D85\u7EA7\u62FC\u88C5'='Advanced Assembly' }
}
foreach ($path in $updates.Keys) {
    $full = Join-Path (Get-Location) $path
    $source = [IO.File]::ReadAllText($full)
    foreach ($key in $updates[$path].Keys) { $source = $source.Replace('m_text: "'+$key+'"', 'm_text: "'+$updates[$path][$key]+'"') }
    [IO.File]::WriteAllText($full,$source,[Text.UTF8Encoding]::new($false))
}
Write-Output "Translated $index SNOM messages, five Confocal component entries and three UI labels."
