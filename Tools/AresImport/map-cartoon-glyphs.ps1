# Reads generated alpha bounds into UV metadata. Does not modify raster assets.
Add-Type -AssemblyName System.Drawing
$atlasRoot = Join-Path $PSScriptRoot '../../Assets/ARES/Resources/ARES/UI/CartoonAlphabet'
function Find-Cuts($counts, $parts) {
    $cuts = [System.Collections.Generic.List[int]]::new()
    $cuts.Add(0)
    for ($i = 1; $i -lt $parts; $i++) {
        $ideal = [int]($counts.Length * $i / $parts)
        $radius = [int]($counts.Length / $parts * .36)
        $best = $ideal
        for ($j = $ideal - $radius; $j -le $ideal + $radius; $j++) {
            if ($counts[$j] -lt $counts[$best] -or ($counts[$j] -eq $counts[$best] -and [Math]::Abs($j-$ideal) -lt [Math]::Abs($best-$ideal))) { $best = $j }
        }
        if ($counts[$best] -gt 3) { throw "No clean atlas boundary near $ideal" }
        $cuts.Add($best)
    }
    $cuts.Add($counts.Length)
    return ,$cuts.ToArray()
}
$allGlyphs = [System.Collections.Generic.List[object]]::new()
$sets = @(
    @{ file='letters-numbers'; cols=6; rows=6; chars='ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789' },
    @{ file='accents-symbols'; cols=4; rows=4; chars=([string]::Concat([char[]](192,193,194,195,201,202,205,211,212,213,218,220,199,33,63,37))) }
)
foreach ($set in $sets) {
    $bitmap = [System.Drawing.Bitmap]::new((Join-Path $atlasRoot ($set.file+'.png')))
    try {
        $width=$bitmap.Width; $height=$bitmap.Height
        $mask=[bool[]]::new($width*$height); $rows=[int[]]::new($height)
        for($y=0;$y -lt $height;$y++) { for($x=0;$x -lt $width;$x++) {
            if($bitmap.GetPixel($x,$y).A -ge 128) { $mask[$y*$width+$x]=$true; $rows[$y]++ }
        }}
        $ycuts=Find-Cuts $rows $set.rows
        for($row=0;$row -lt $set.rows;$row++) {
            $cols=[int[]]::new($width)
            for($y=$ycuts[$row];$y -lt $ycuts[$row+1];$y++) { for($x=0;$x -lt $width;$x++) { if($mask[$y*$width+$x]) {$cols[$x]++} } }
            $xcuts=Find-Cuts $cols $set.cols
            for($col=0;$col -lt $set.cols;$col++) {
                $minX=$width; $minY=$height; $maxX=0; $maxY=0
                for($y=$ycuts[$row];$y -lt $ycuts[$row+1];$y++) { for($x=$xcuts[$col];$x -lt $xcuts[$col+1];$x++) {
                    if(!$mask[$y*$width+$x]) {continue}
                    $minX=[Math]::Min($minX,$x); $maxX=[Math]::Max($maxX,$x)
                    $minY=[Math]::Min($minY,$y); $maxY=[Math]::Max($maxY,$y)
                }}
                if($maxX -lt $minX) {throw 'Empty glyph'}
                $minX=[Math]::Max(0,$minX-2); $minY=[Math]::Max(0,$minY-2)
                $maxX=[Math]::Min($width-1,$maxX+2); $maxY=[Math]::Min($height-1,$maxY+2)
                $allGlyphs.Add(@{ character=[string]$set.chars[$row*$set.cols+$col]; atlas=$set.file;
                    x=$minX; y=$height-1-$maxY; width=$maxX-$minX+1; height=$maxY-$minY+1; textureWidth=$width; textureHeight=$height })
            }
        }
    } finally { $bitmap.Dispose() }
}
@{glyphs=$allGlyphs.ToArray()} | ConvertTo-Json -Depth 4 | Set-Content -Encoding UTF8 (Join-Path $atlasRoot 'glyphs.json')
Write-Output ('Mapped '+$allGlyphs.Count+' generated glyphs without modifying their PNGs.')
