param(
    [string]$Suffix = '',
    [string]$PaperDirectory = '',
    [string[]]$Names = @('AIxVR2027_VRMicroscope_Paper_EN_Formal', 'AIxVR2027_VRMicroscope_Draft_Formal')
)
$ErrorActionPreference = 'Stop'
if (-not $PaperDirectory) {
    $PaperDirectory = Join-Path $PSScriptRoot $(if ($Suffix -eq '_Anonymous') { '匿名版' } else { '正式版' })
}
foreach ($name in $Names) {
    $sourcePath = (Resolve-Path (Join-Path $PaperDirectory ($name + $Suffix + '.docx'))).Path
    $pdfPath = [System.IO.Path]::ChangeExtension($sourcePath, '.pdf')
    # These documents contain native Word equations. LibreOffice changes some
    # delimiters into error glyphs and loses the SIM equation's explicit breaks.
    # Export with Word's math renderer. The font finalizer below embeds any
    # system fonts omitted by Word without changing its equation layout.
    $word = New-Object -ComObject Word.Application
    $word.Visible = $false
    $word.DisplayAlerts = 0
    try {
        Write-Output ('Opening: ' + $name + $Suffix)
        $document = $word.Documents.Open($sourcePath, $false, $true)
        try {
            $document.Repaginate()
            $pages = $document.ComputeStatistics(2)
            Write-Output ('Exporting: ' + $name + $Suffix + " ($pages pages)")
            $document.ExportAsFixedFormat($pdfPath, 17)
            Write-Output ($name + $Suffix + ': Word export completed')
        }
        finally { $document.Close(0) }
    }
    finally {
        $word.Quit()
    }
    if (-not (Test-Path -LiteralPath $pdfPath) -or
        (Get-Item -LiteralPath $pdfPath).Length -lt 1024) {
        throw "Word did not produce a complete PDF for $sourcePath."
    }
    & python (Join-Path $PSScriptRoot 'finalize_pdf_fonts.py') $pdfPath
    if ($LASTEXITCODE -ne 0) {
        throw "PDF font validation failed for $sourcePath."
    }
    Write-Output ($name + $Suffix + ": PDF exported with embedded fonts ($pages pages)")
}
