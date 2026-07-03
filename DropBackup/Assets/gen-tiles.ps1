param()
Set-Location "C:\Work\Dev\TC\Duplicate\Duplicate\Assets"
$svg    = "icon.svg"
$magick = "magick"

# SVG → 透過PNG（-background transparent で SVG 自体の透過を維持）
function Svg-ToPng($sizePx, $outFile) {
	& $magick -background transparent -density 600 $svg -resize "${sizePx}x${sizePx}" -type TrueColorAlpha PNG32:$outFile
}

# 正方形タイル生成（透過）
function Make-Square($w, $name) {
	$pad     = [int]($w * 0.15)
	$inner   = $w - ($pad * 2)
	Svg-ToPng $inner "tmp_icon.png"
	& $magick -size "${w}x${w}" xc:transparent `
			  "(" "tmp_icon.png" ")" -gravity Center -composite `
			  PNG32:$name
	Remove-Item "tmp_icon.png" -ErrorAction SilentlyContinue
}

# Wide タイル生成（透過）
function Make-Wide($w, $h, $name) {
	$iconSize = [int]($h * 0.7)
	$iconX    = [int]($h * 0.15)
	$ptSize   = [int]($h * 0.26)
	$textOff  = [int]($h * 0.28)
	Svg-ToPng $iconSize "tmp_icon.png"
	& $magick -size "${w}x${h}" xc:transparent `
			  "(" "tmp_icon.png" ")" -gravity West -geometry "+${iconX}+0" -composite `
			  -fill "#1E3A6E" -font "Arial" -pointsize $ptSize `
			  -gravity Center -annotate "+${textOff}+0" "DropBackup" `
			  PNG32:$name
	Remove-Item "tmp_icon.png" -ErrorAction SilentlyContinue
}

Make-Square 50  "StoreLogo.png"
Make-Square 100 "StoreLogo.scale-200.png"
Make-Square 44  "Square44x44Logo.png"
Make-Square 88  "Square44x44Logo.scale-200.png"
Make-Square 150 "Square150x150Logo.png"
Make-Square 300 "Square150x150Logo.scale-200.png"
Make-Square 310 "Square310x310Logo.png"
Make-Square 620 "Square310x310Logo.scale-200.png"
Make-Wide 310 150 "Wide310x150Logo.png"
Make-Wide 620 300 "Wide310x150Logo.scale-200.png"

# ICO（16/32/48/256px 全透過）― 高解像度ベースから縮小してアンチエイリアスを綺麗に
Svg-ToPng 512 "base512.png"
& $magick "base512.png" -resize 256x256 -type TrueColorAlpha PNG32:"tmp256.png"
& $magick "base512.png" -resize  48x48  -type TrueColorAlpha PNG32:"tmp48.png"
& $magick "base512.png" -resize  32x32  -type TrueColorAlpha PNG32:"tmp32.png"
& $magick "base512.png" -resize  16x16  -type TrueColorAlpha PNG32:"tmp16.png"
& $magick "tmp256.png" "tmp48.png" "tmp32.png" "tmp16.png" -define "icon:auto-resize=256,48,32,16" -type TrueColorAlpha "icon.ico"
Remove-Item "base512.png","tmp256.png","tmp48.png","tmp32.png","tmp16.png" -ErrorAction SilentlyContinue

Write-Host "=== 透過確認 ==="
& $magick "icon.ico" -format "ICO %wx%h corner-alpha:%[fx:p{0,0}.a] center-alpha:%[fx:p{w/2,h/2}.a]\n" info:

Get-ChildItem "*.png","*.ico" | Select-Object Name,@{n="KB";e={[int]($_.Length/1KB)}} | Sort-Object Name
